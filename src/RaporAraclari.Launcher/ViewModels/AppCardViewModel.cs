using System.Windows.Media;
using RaporAraclari.Launcher.Infrastructure;
using RaporAraclari.Launcher.Models;

namespace RaporAraclari.Launcher.ViewModels;

public sealed class AppCardViewModel : ObservableObject
{
    private string _installedVersion = "Kurulu degil";
    private string _latestVersion = "Kontrol edilmedi";
    private string _statusText = "Kurulum bekleniyor";
    private string _lastCheckedText = "Henuz kontrol edilmedi";
    private string? _installedPath;
    private string? _lastError;
    private bool _isInstalled;
    private bool _isBusy;
    private bool _hasUpdateAvailable;

    public AppCardViewModel(LauncherAppDefinition definition)
    {
        Definition = definition;
        AccentBrush = CreateBrush(definition.AccentColor);
        AccentSoftBrush = CreateBrush(definition.AccentLightColor);
    }

    public LauncherAppDefinition Definition { get; }

    public Brush AccentBrush { get; }

    public Brush AccentSoftBrush { get; }

    public string InstalledVersion
    {
        get => _installedVersion;
        set => SetProperty(ref _installedVersion, value);
    }

    public string LatestVersion
    {
        get => _latestVersion;
        set => SetProperty(ref _latestVersion, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string LastCheckedText
    {
        get => _lastCheckedText;
        set => SetProperty(ref _lastCheckedText, value);
    }

    public string? InstalledPath
    {
        get => _installedPath;
        set => SetProperty(ref _installedPath, value);
    }

    public string? LastError
    {
        get => _lastError;
        set
        {
            if (SetProperty(ref _lastError, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(LastError);

    public bool IsInstalled
    {
        get => _isInstalled;
        set
        {
            if (SetProperty(ref _isInstalled, value))
            {
                OnPropertyChanged(nameof(ActionText));
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(ActionText));
            }
        }
    }

    public bool HasUpdateAvailable
    {
        get => _hasUpdateAvailable;
        set => SetProperty(ref _hasUpdateAvailable, value);
    }

    public string ActionText => IsBusy ? "Isleniyor..." : IsInstalled ? "Uygulamayi Ac" : "Kur ve Ac";

    public void ApplyState(AppInstallationRecord? record, ResolvedInstall? resolvedInstall = null)
    {
        resolvedInstall ??= new ResolvedInstall
        {
            ExecutablePath = record?.InstalledPath,
            Version = record?.InstalledVersion,
            AssetKind = record?.LastAssetKind
        };

        IsInstalled = resolvedInstall.IsInstalled;
        InstalledPath = resolvedInstall.ExecutablePath;
        InstalledVersion = resolvedInstall.IsInstalled
            ? record?.InstalledVersion ?? resolvedInstall.Version ?? "Surum okunamadi"
            : "Kurulu degil";

        LatestVersion = string.IsNullOrWhiteSpace(record?.LastKnownReleaseVersion)
            ? "Kontrol edilmedi"
            : record.LastKnownReleaseVersion;

        LastCheckedText = record?.LastCheckedUtc is null
            ? "Henuz kontrol edilmedi"
            : $"{record.LastCheckedUtc.Value.LocalDateTime:G}";

        LastError = record?.LastError;
        StatusText = BuildStatusText(record, resolvedInstall);
    }

    private static string BuildStatusText(AppInstallationRecord? record, ResolvedInstall resolvedInstall)
    {
        if (!string.IsNullOrWhiteSpace(record?.LastError))
        {
            return "Dikkat gerekiyor";
        }

        if (resolvedInstall.IsInstalled)
        {
            return record?.LastAssetKind switch
            {
                nameof(ReleaseAssetKind.Portable) => "Hazir (tasinabilir paket)",
                nameof(ReleaseAssetKind.Setup) => "Hazir (kurulu uygulama)",
                _ => "Hazir"
            };
        }

        return "Kurulum bekleniyor";
    }

    private static Brush CreateBrush(string value)
    {
        return (Brush)new BrushConverter().ConvertFromString(value)!;
    }
}

