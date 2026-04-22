using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using RaporAraclari.Launcher.Models;

namespace RaporAraclari.Launcher.Services;

public sealed class AppInstallationService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly GitHubCliService _gitHubCliService;

    public AppInstallationService()
    {
        _httpClient = new HttpClient();
        _gitHubCliService = new GitHubCliService();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RaporAraclariLauncher", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
    }

    public ResolvedInstall ResolveInstalledExecutable(LauncherAppDefinition definition, AppInstallationRecord? record)
    {
        if (!string.IsNullOrWhiteSpace(record?.InstalledPath) && File.Exists(record.InstalledPath))
        {
            return BuildResolvedInstall(record.InstalledPath, record);
        }

        foreach (var installDirectory in definition.KnownInstallDirectories)
        {
            var expandedDirectory = Environment.ExpandEnvironmentVariables(installDirectory);
            var candidatePath = Path.Combine(expandedDirectory, definition.InstalledExeRelativePath);

            if (File.Exists(candidatePath))
            {
                return BuildResolvedInstall(candidatePath, record);
            }
        }

        var portableRoot = Path.Combine(LauncherPaths.PortableAppsDirectory, definition.PortableInstallRoot);
        if (Directory.Exists(portableRoot))
        {
            var latestVersionDirectory = new DirectoryInfo(portableRoot)
                .GetDirectories()
                .OrderByDescending(directory => directory.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (latestVersionDirectory is not null)
            {
                var portablePath = Path.Combine(latestVersionDirectory.FullName, Path.GetFileName(definition.InstalledExeRelativePath));

                if (File.Exists(portablePath))
                {
                    return BuildResolvedInstall(portablePath, record);
                }
            }
        }

        return new ResolvedInstall
        {
            ExecutablePath = null,
            Version = record?.InstalledVersion,
            AssetKind = record?.LastAssetKind
        };
    }

    public (GitHubReleaseAsset Asset, ReleaseAssetKind AssetKind) SelectPreferredAsset(LauncherAppDefinition definition, GitHubReleaseInfo release)
    {
        var setupAsset = release.Assets.FirstOrDefault(asset => IsMatch(asset.Name, definition.SetupAssetRegex));
        if (setupAsset is not null)
        {
            return (setupAsset, ReleaseAssetKind.Setup);
        }

        if (!string.IsNullOrWhiteSpace(definition.PortableAssetRegex))
        {
            var portableAsset = release.Assets.FirstOrDefault(asset => IsMatch(asset.Name, definition.PortableAssetRegex));
            if (portableAsset is not null)
            {
                return (portableAsset, ReleaseAssetKind.Portable);
            }
        }

        throw new InvalidOperationException(
            $"{definition.DisplayName} release varliklari arasinda desteklenen kurulum dosyasi bulunamadi.");
    }

    public async Task<InstallResult> InstallOrUpdateAsync(
        LauncherAppDefinition definition,
        GitHubReleaseInfo release,
        AppInstallationRecord? existingRecord,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        LauncherPaths.EnsureBaseDirectories();

        var (asset, assetKind) = SelectPreferredAsset(definition, release);
        var downloadDirectory = Path.Combine(LauncherPaths.DownloadsDirectory, definition.Id, SanitizePathSegment(release.TagName));
        Directory.CreateDirectory(downloadDirectory);
        var downloadedFilePath = Path.Combine(downloadDirectory, asset.Name);

        progress?.Report($"{definition.DisplayName} indiriliyor...");
        await DownloadAssetAsync(definition, release, asset, downloadedFilePath, cancellationToken);

        try
        {
            return assetKind switch
            {
                ReleaseAssetKind.Setup => await InstallSetupAsync(definition, release, existingRecord, downloadedFilePath, progress, cancellationToken),
                ReleaseAssetKind.Portable => InstallPortable(definition, release, downloadedFilePath, progress),
                _ => throw new InvalidOperationException("Desteklenmeyen kurulum tipi.")
            };
        }
        finally
        {
            TryDeleteFile(downloadedFilePath);
        }
    }

    public void Launch(ResolvedInstall installation)
    {
        if (!installation.IsInstalled || string.IsNullOrWhiteSpace(installation.ExecutablePath))
        {
            throw new InvalidOperationException("Uygulama acilamadi; calistirilabilir dosya bulunamadi.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = installation.ExecutablePath,
            WorkingDirectory = Path.GetDirectoryName(installation.ExecutablePath) ?? Environment.CurrentDirectory,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    public static bool IsUpdateAvailable(string? installedVersion, string latestVersion)
    {
        if (string.IsNullOrWhiteSpace(installedVersion))
        {
            return true;
        }

        if (TryParseVersion(installedVersion, out var installed) && TryParseVersion(latestVersion, out var latest))
        {
            return latest > installed;
        }

        return !string.Equals(installedVersion, latestVersion, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<InstallResult> InstallSetupAsync(
        LauncherAppDefinition definition,
        GitHubReleaseInfo release,
        AppInstallationRecord? existingRecord,
        string installerPath,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        progress?.Report($"{definition.DisplayName} sessiz kurulumla yukleniyor...");

        var startInfo = new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = string.Join(" ", definition.SilentInstallArguments),
            WorkingDirectory = Path.GetDirectoryName(installerPath) ?? Environment.CurrentDirectory,
            UseShellExecute = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException($"{definition.DisplayName} kurulum islemi baslatilamadi.");
        }

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{definition.DisplayName} kurulum islemi basarisiz oldu. Cikis kodu: {process.ExitCode}");
        }

        var resolvedInstall = ResolveInstalledExecutable(definition, existingRecord);
        if (!resolvedInstall.IsInstalled || string.IsNullOrWhiteSpace(resolvedInstall.ExecutablePath))
        {
            throw new InvalidOperationException(
                $"{definition.DisplayName} kuruldu ancak calistirilabilir dosya bulunamadi.");
        }

        return new InstallResult
        {
            InstalledPath = resolvedInstall.ExecutablePath,
            InstalledVersion = release.TagName,
            AssetKind = ReleaseAssetKind.Setup
        };
    }

    private InstallResult InstallPortable(
        LauncherAppDefinition definition,
        GitHubReleaseInfo release,
        string downloadedFilePath,
        IProgress<string>? progress)
    {
        progress?.Report($"{definition.DisplayName} tasinabilir paket olarak hazirlaniyor...");

        var destinationDirectory = LauncherPaths.GetPortableInstallDirectory(definition.PortableInstallRoot, release.TagName);
        Directory.CreateDirectory(destinationDirectory);

        var destinationPath = Path.Combine(destinationDirectory, Path.GetFileName(downloadedFilePath));
        File.Copy(downloadedFilePath, destinationPath, true);

        return new InstallResult
        {
            InstalledPath = destinationPath,
            InstalledVersion = release.TagName,
            AssetKind = ReleaseAssetKind.Portable
        };
    }

    private async Task DownloadAssetAsync(
        LauncherAppDefinition definition,
        GitHubReleaseInfo release,
        GitHubReleaseAsset asset,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = File.Create(destinationPath);
            await input.CopyToAsync(output, cancellationToken);
        }
        catch (Exception exception) when (TlsFailureDetector.IsSecureChannelFailure(exception))
        {
            if (!_gitHubCliService.IsAvailable())
            {
                throw new InvalidOperationException(
                    $"{definition.DisplayName} indirilemedi. {TlsFailureDetector.BuildUserFacingMessage("HttpClient")}",
                    exception);
            }

            var destinationDirectory = Path.GetDirectoryName(destinationPath) ?? LauncherPaths.DownloadsDirectory;
            await _gitHubCliService.DownloadReleaseAssetAsync(definition, release.TagName, asset.Name, destinationDirectory, cancellationToken);

            if (!File.Exists(destinationPath))
            {
                throw new InvalidOperationException(
                    $"{definition.DisplayName} icin gh fallback indirmesi tamamlandi ancak beklenen dosya bulunamadi: {destinationPath}");
            }
        }
    }

    private static ResolvedInstall BuildResolvedInstall(string executablePath, AppInstallationRecord? record)
    {
        return new ResolvedInstall
        {
            ExecutablePath = executablePath,
            Version = ReadVersionFromExecutable(executablePath) ?? record?.InstalledVersion,
            AssetKind = record?.LastAssetKind
        };
    }

    private static string? ReadVersionFromExecutable(string executablePath)
    {
        try
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
            var version = versionInfo.ProductVersion ?? versionInfo.FileVersion;
            return string.IsNullOrWhiteSpace(version) ? null : version;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsMatch(string value, string pattern)
    {
        return Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant);
    }

    private static bool TryParseVersion(string value, out Version version)
    {
        value = value.Trim();
        if (value.StartsWith("v", true, CultureInfo.InvariantCulture))
        {
            value = value[1..];
        }

        return Version.TryParse(value, out version!);
    }

    private static string SanitizePathSegment(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(character => invalidChars.Contains(character) ? '_' : character));
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Leave the file if Windows still holds a handle.
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
