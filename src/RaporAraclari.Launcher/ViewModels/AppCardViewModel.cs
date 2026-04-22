using System.Windows.Media;
using RaporAraclari.Launcher.Infrastructure;
using RaporAraclari.Launcher.Models;

namespace RaporAraclari.Launcher.ViewModels;

public sealed class AppCardViewModel : ObservableObject
{
    private static readonly Brush AccentStatusBrush = CreateBrush("#4AADE0");
    private static readonly Brush SecondaryStatusBrush = CreateBrush("#999999");
    private static readonly Brush DangerStatusBrush = CreateBrush("#CC0000");

    private string _installedVersion = "Kurulu değil";
    private string _latestVersion = "Kontrol edilmedi";
    private string _statusText = "Kurulum bekleniyor";
    private string _lastCheckedText = "Henüz kontrol edilmedi";
    private string? _installedPath;
    private string? _lastError;
    private bool _isInstalled;
    private bool _isBusy;
    private bool _hasUpdateAvailable;

    public AppCardViewModel(LauncherAppDefinition definition)
    {
        Definition = definition;
    }

    public LauncherAppDefinition Definition { get; }

    public string InstalledVersion
    {
        get => _installedVersion;
        set
        {
            if (SetProperty(ref _installedVersion, value))
            {
                OnPropertyChanged(nameof(InstalledSummary));
            }
        }
    }

    public string LatestVersion
    {
        get => _latestVersion;
        set
        {
            if (SetProperty(ref _latestVersion, value))
            {
                OnPropertyChanged(nameof(UpdateSummary));
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            if (SetProperty(ref _statusText, value))
            {
                OnPropertyChanged(nameof(UpdateSummary));
            }
        }
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
                OnPropertyChanged(nameof(UpdateSummary));
                OnPropertyChanged(nameof(StatusBrush));
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
                OnPropertyChanged(nameof(InstalledSummary));
                OnPropertyChanged(nameof(UpdateSummary));
                RefreshActionProperties();
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
                RefreshActionProperties();
            }
        }
    }

    public bool HasUpdateAvailable
    {
        get => _hasUpdateAvailable;
        set
        {
            if (SetProperty(ref _hasUpdateAvailable, value))
            {
                OnPropertyChanged(nameof(UpdateSummary));
                OnPropertyChanged(nameof(StatusBrush));
                RefreshActionProperties();
            }
        }
    }

    public string InstalledSummary => $"Kurulu sürüm: {InstalledVersion}";

    public string UpdateSummary
    {
        get
        {
            if (HasError)
            {
                return "Durum: Son işlem dikkat gerektiriyor";
            }

            if (HasUpdateAvailable)
            {
                return $"Durum: Yeni sürüm hazır ({LatestVersion})";
            }

            return $"Durum: {StatusText}";
        }
    }

    public Brush StatusBrush =>
        HasError ? DangerStatusBrush :
        HasUpdateAvailable ? AccentStatusBrush :
        SecondaryStatusBrush;

    public string PrimaryActionText => IsBusy ? "İşleniyor..." : IsInstalled ? "Aç" : "Kur ve Aç";

    public bool ShowUpdateAction => HasUpdateAvailable && IsInstalled && !IsBusy;

    public bool ShowAccentPrimaryAction => !ShowUpdateAction;

    public bool ShowNeutralPrimaryAction => ShowUpdateAction;

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
            ? record?.InstalledVersion ?? resolvedInstall.Version ?? "Sürüm okunamadı"
            : "Kurulu değil";

        LatestVersion = string.IsNullOrWhiteSpace(record?.LastKnownReleaseVersion)
            ? "Kontrol edilmedi"
            : record.LastKnownReleaseVersion;

        LastCheckedText = record?.LastCheckedUtc is null
            ? "Henüz kontrol edilmedi"
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
                nameof(ReleaseAssetKind.Portable) => "Hazır (taşınabilir paket)",
                nameof(ReleaseAssetKind.Setup) => "Hazır",
                _ => "Hazır"
            };
        }

        return "Kurulum bekleniyor";
    }

    private void RefreshActionProperties()
    {
        OnPropertyChanged(nameof(PrimaryActionText));
        OnPropertyChanged(nameof(ShowUpdateAction));
        OnPropertyChanged(nameof(ShowAccentPrimaryAction));
        OnPropertyChanged(nameof(ShowNeutralPrimaryAction));
    }

    private static Brush CreateBrush(string value)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(value)!;
        brush.Freeze();
        return brush;
    }
}
