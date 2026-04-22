using System.Diagnostics;
using System.Text.Json;
using RaporAraclari.Launcher.Models;

namespace RaporAraclari.Launcher.Services;

public sealed class GitHubCliService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool IsAvailable()
    {
        try
        {
            var startInfo = CreateStartInfo("--version");
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<GitHubReleaseInfo> GetLatestReleaseAsync(LauncherAppDefinition definition, CancellationToken cancellationToken = default)
    {
        var output = await RunGhAsync(
            $"api repos/{definition.RepoOwner}/{definition.RepoName}/releases/latest",
            cancellationToken);

        var release = JsonSerializer.Deserialize<GitHubReleaseInfo>(output, SerializerOptions);
        if (release is null)
        {
            throw new InvalidOperationException($"{definition.DisplayName} icin gh release verisi cozumlenemedi.");
        }

        return release;
    }

    public async Task DownloadReleaseAssetAsync(
        LauncherAppDefinition definition,
        string tagName,
        string assetName,
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(destinationDirectory);

        await RunGhAsync(
            $"release download {Quote(tagName)} --repo {definition.RepoOwner}/{definition.RepoName} --pattern {Quote(assetName)} --dir {Quote(destinationDirectory)} --clobber",
            cancellationToken);
    }

    private async Task<string> RunGhAsync(string arguments, CancellationToken cancellationToken)
    {
        var startInfo = CreateStartInfo(arguments);
        using var process = Process.Start(startInfo);

        if (process is null)
        {
            throw new InvalidOperationException("gh islemi baslatilamadi.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"gh komutu basarisiz oldu (cikis kodu {process.ExitCode}). {error}".Trim());
        }

        return output;
    }

    private static ProcessStartInfo CreateStartInfo(string arguments)
    {
        return new ProcessStartInfo
        {
            FileName = "gh",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
    }

    private static string Quote(string value)
    {
        return $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }
}

