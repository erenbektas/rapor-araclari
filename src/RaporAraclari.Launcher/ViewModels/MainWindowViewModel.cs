using System.Collections.ObjectModel;
using System.Reflection;
using RaporAraclari.Launcher.Infrastructure;
using RaporAraclari.Launcher.Models;
using RaporAraclari.Launcher.Services;

namespace RaporAraclari.Launcher.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly AppManifestProvider _manifestProvider;
    private readonly LauncherStateStore _stateStore;
    private readonly GitHubReleaseService _releaseService;
    private readonly AppInstallationService _installationService;
    private readonly DialogService _dialogService;
    private readonly List<LauncherAppDefinition> _definitions = [];
    private LauncherState _state = new();
    private string _statusMessage = "Hazirlaniyor...";
    private string _statusDetail = "Launcher servisleri baslatiliyor.";
    private bool _isBusy;
    private bool _isInitialized;

    public MainWindowViewModel(
        AppManifestProvider manifestProvider,
        LauncherStateStore stateStore,
        GitHubReleaseService releaseService,
        AppInstallationService installationService,
        DialogService dialogService)
    {
        _manifestProvider = manifestProvider;
        _stateStore = stateStore;
        _releaseService = releaseService;
        _installationService = installationService;
        _dialogService = dialogService;

        OpenAppCommand = new AsyncRelayCommand<AppCardViewModel>(OpenAppAsync, CanRunCardAction);
        CheckUpdatesCommand = new AsyncRelayCommand(() => CheckForUpdatesAsync(true), () => !IsBusy);
        LauncherVersion = $"Launcher v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"}";
    }

    public ObservableCollection<AppCardViewModel> Apps { get; } = [];

    public AsyncRelayCommand<AppCardViewModel> OpenAppCommand { get; }

    public AsyncRelayCommand CheckUpdatesCommand { get; }

    public string LauncherVersion { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string StatusDetail
    {
        get => _statusDetail;
        private set => SetProperty(ref _statusDetail, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OpenAppCommand.RaiseCanExecuteChanged();
                CheckUpdatesCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await RunBusyOperationAsync(
            "Calisma alani hazirlaniyor",
            "Yuklu araclar kontrol ediliyor ve gerekiyorsa ilk kurulum baslatiliyor.",
            async () =>
            {
                _definitions.Clear();
                _definitions.AddRange(await _manifestProvider.LoadAsync());
                _state = await _stateStore.LoadAsync();

                Apps.Clear();
                foreach (var definition in _definitions)
                {
                    var record = GetOrCreateRecord(definition.Id);
                    var card = new AppCardViewModel(definition);
                    card.ApplyState(record, _installationService.ResolveInstalledExecutable(definition, record));
                    Apps.Add(card);
                }

                await EnsureAppsInstalledAsync();
                await CheckForUpdatesCoreAsync(false);
                _isInitialized = true;
            });
    }

    public async Task CheckForUpdatesAsync(bool showUpToDateMessage)
    {
        await RunBusyOperationAsync(
            "Guncellemeler denetleniyor",
            "GitHub release bilgileri kontrol ediliyor.",
            () => CheckForUpdatesCoreAsync(showUpToDateMessage));
    }

    private async Task EnsureAppsInstalledAsync()
    {
        foreach (var card in Apps)
        {
            var record = GetOrCreateRecord(card.Definition.Id);
            var resolvedInstall = _installationService.ResolveInstalledExecutable(card.Definition, record);

            if (resolvedInstall.IsInstalled)
            {
                SyncRecordWithResolution(card.Definition, resolvedInstall);
                card.ApplyState(record, resolvedInstall);
                continue;
            }

            try
            {
                await InstallLatestAsync(card, $"{card.Definition.DisplayName} ilk kez kuruluyor...");
            }
            catch
            {
                card.ApplyState(record);
            }
        }

        await _stateStore.SaveAsync(_state);
    }

    private async Task OpenAppAsync(AppCardViewModel? card)
    {
        if (card is null)
        {
            return;
        }

        await RunBusyOperationAsync(
            $"{card.Definition.DisplayName} hazirlaniyor",
            "Uygulama baslatilmadan once kurulum durumu dogrulaniyor.",
            async () =>
            {
                var record = GetRecord(card.Definition.Id);
                var resolvedInstall = _installationService.ResolveInstalledExecutable(card.Definition, record);

                if (!resolvedInstall.IsInstalled)
                {
                    await InstallLatestAsync(card, $"{card.Definition.DisplayName} indiriliyor ve acilisa hazirlaniyor...");
                    record = GetRecord(card.Definition.Id);
                    resolvedInstall = _installationService.ResolveInstalledExecutable(card.Definition, record);
                }

                _installationService.Launch(resolvedInstall);
                card.ApplyState(record, resolvedInstall);
                StatusMessage = $"{card.Definition.DisplayName} acildi";
                StatusDetail = "Launcher arka planda acik kalmaya devam ediyor.";
            },
            card);
    }

    private async Task InstallLatestAsync(AppCardViewModel card, string operationText)
    {
        var release = await _releaseService.GetLatestReleaseAsync(card.Definition);
        await InstallReleaseAsync(card, release, operationText);
    }

    private async Task CheckForUpdatesCoreAsync(bool showUpToDateMessage)
    {
        var updateCandidates = new List<UpdateCandidate>();

        foreach (var card in Apps)
        {
            var record = GetOrCreateRecord(card.Definition.Id);

            try
            {
                var release = await _releaseService.GetLatestReleaseAsync(card.Definition);
                var (_, assetKind) = _installationService.SelectPreferredAsset(card.Definition, release);

                record.LastKnownReleaseVersion = release.TagName;
                record.LastCheckedUtc = DateTimeOffset.UtcNow;
                record.LastError = null;

                var resolvedInstall = _installationService.ResolveInstalledExecutable(card.Definition, record);
                SyncRecordWithResolution(card.Definition, resolvedInstall);
                card.ApplyState(record, resolvedInstall);
                card.HasUpdateAvailable = AppInstallationService.IsUpdateAvailable(record.InstalledVersion, release.TagName);
                card.StatusText = card.HasUpdateAvailable ? "Guncelleme hazir" : card.StatusText;

                if (card.HasUpdateAvailable)
                {
                    updateCandidates.Add(new UpdateCandidate
                    {
                        Definition = card.Definition,
                        Release = release,
                        CurrentVersion = record.InstalledVersion ?? "Bilinmiyor",
                        AssetKind = assetKind
                    });
                }
            }
            catch (Exception exception)
            {
                record.LastError = exception.Message;
                card.ApplyState(record);
            }
        }

        await _stateStore.SaveAsync(_state);

        foreach (var candidate in updateCandidates)
        {
            if (_dialogService.ShowUpdatePrompt(candidate))
            {
                var card = Apps.Single(app => app.Definition.Id == candidate.Definition.Id);
                await InstallReleaseAsync(card, candidate.Release, "Guncelleme kuruluyor...");
                card.HasUpdateAvailable = false;
            }
        }

        if (showUpToDateMessage && updateCandidates.Count == 0)
        {
            _dialogService.ShowInfo("Guncelleme Durumu", "Tum araclar guncel gorunuyor.");
        }

        StatusMessage = updateCandidates.Count == 0 ? "Tum araclar hazir" : "Guncelleme denetimi tamamlandi";
        StatusDetail = updateCandidates.Count == 0
            ? "Yeni bir surum bulunmadi."
            : "Uygun guncellemeler icin onay pencereleri gosterildi.";
    }

    private async Task InstallReleaseAsync(AppCardViewModel card, GitHubReleaseInfo release, string operationText)
    {
        var progress = new Progress<string>(message =>
        {
            StatusMessage = operationText;
            StatusDetail = message;
        });

        card.IsBusy = true;
        card.LatestVersion = release.TagName;

        try
        {
            var record = GetOrCreateRecord(card.Definition.Id);
            var result = await _installationService.InstallOrUpdateAsync(card.Definition, release, record, progress);

            record.InstalledVersion = result.InstalledVersion;
            record.InstalledPath = result.InstalledPath;
            record.LastAssetKind = result.AssetKind.ToString();
            record.LastKnownReleaseVersion = release.TagName;
            record.LastCheckedUtc = DateTimeOffset.UtcNow;
            record.LastUpdatedUtc = DateTimeOffset.UtcNow;
            record.LastError = null;

            var resolvedInstall = _installationService.ResolveInstalledExecutable(card.Definition, record);
            card.ApplyState(record, resolvedInstall);
            card.HasUpdateAvailable = false;

            await _stateStore.SaveAsync(_state);
        }
        catch (Exception exception)
        {
            var record = GetOrCreateRecord(card.Definition.Id);
            record.LastError = exception.Message;
            record.LastCheckedUtc = DateTimeOffset.UtcNow;
            record.LastKnownReleaseVersion = release.TagName;
            card.ApplyState(record);
            await _stateStore.SaveAsync(_state);
            throw;
        }
        finally
        {
            card.IsBusy = false;
        }
    }

    private AppInstallationRecord GetRecord(string appId)
    {
        return _state.Apps.TryGetValue(appId, out var record) ? record : new AppInstallationRecord();
    }

    private AppInstallationRecord GetOrCreateRecord(string appId)
    {
        if (!_state.Apps.TryGetValue(appId, out var record))
        {
            record = new AppInstallationRecord();
            _state.Apps[appId] = record;
        }

        return record;
    }

    private void SyncRecordWithResolution(LauncherAppDefinition definition, ResolvedInstall resolvedInstall)
    {
        if (!resolvedInstall.IsInstalled)
        {
            return;
        }

        var record = GetOrCreateRecord(definition.Id);
        record.InstalledPath = resolvedInstall.ExecutablePath;
        record.InstalledVersion ??= resolvedInstall.Version;
        record.LastAssetKind ??= resolvedInstall.AssetKind;
    }

    private async Task RunBusyOperationAsync(string status, string detail, Func<Task> action, AppCardViewModel? card = null)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = status;
            StatusDetail = detail;

            if (card is not null)
            {
                card.IsBusy = true;
            }

            await action();
        }
        catch (Exception exception)
        {
            StatusMessage = "Islem tamamlanamadi";
            StatusDetail = exception.Message;
            _dialogService.ShowError("Launcher Hatasi", exception.Message);
        }
        finally
        {
            if (card is not null)
            {
                card.IsBusy = false;
            }

            IsBusy = false;
        }
    }

    private bool CanRunCardAction(AppCardViewModel? card)
    {
        return card is not null && !IsBusy && !card.IsBusy;
    }
}
