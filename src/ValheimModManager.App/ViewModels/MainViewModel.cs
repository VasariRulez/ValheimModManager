namespace ValheimModManager.App.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimModManager.Core.Abstractions;
using ValheimModManager.Core.Import;
using ValheimModManager.Core.Install;
using ValheimModManager.Core.Models;
using ValheimModManager.Core.Localization;
using ValheimModManager.Core.Providers.Thunderstore;
using ValheimModManager.Core.Services;
using ValheimModManager.Platform.Windows;
using ValheimModManager.Platform.Linux;

public partial class MainViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient;
    private readonly ProfileService _profileService;
    private readonly BepInExService _bepInExService;
    private readonly CatalogService _catalogService;
    private readonly InstallService _installService;
    private readonly DependencyResolver _dependencyResolver;
    private readonly UpdateService _updateService;
    private readonly AppUpdateService _appUpdateService;
    private readonly R2ModmanImporter _r2Importer;
    private readonly ProfileShareService _profileShareService;
    private readonly ManualModInstaller _manualInstaller;
    private readonly GameLauncher _gameLauncher;
    private readonly ISteamLocator _steamLocator;
    private readonly IProcessMonitor _processMonitor;

    public Action<string>? OnCopyToClipboardRequested { get; set; }

    private CancellationTokenSource? _catalogSearchCts;
    private CancellationTokenSource? _installedSearchCts;

    [ObservableProperty]
    private string _currentAppVersion = "1.0.4";

    [ObservableProperty]
    private bool _isAppUpdateAvailable;

    [ObservableProperty]
    private bool _isAppUpdateBannerDismissed;

    public bool IsAppUpdateBannerVisible => IsAppUpdateAvailable && !IsAppUpdateBannerDismissed;

    partial void OnIsAppUpdateBannerDismissedChanged(bool value) => OnPropertyChanged(nameof(IsAppUpdateBannerVisible));
    partial void OnIsAppUpdateAvailableChanged(bool value) => OnPropertyChanged(nameof(IsAppUpdateBannerVisible));

    [ObservableProperty]
    private bool _isAppUpdateChecking;

    [ObservableProperty]
    private string _latestAppVersion = "";

    [ObservableProperty]
    private string _latestAppReleaseTitle = "";

    [ObservableProperty]
    private string _latestAppReleaseNotes = "";

    [ObservableProperty]
    private string _latestAppReleaseUrl = "";

    [ObservableProperty]
    private string _appUpdateStatusText = "Valheim Mod Manager è aggiornato";

    [ObservableProperty]
    private bool _isAppUpdateDialogVisible;

    [ObservableProperty]
    private bool _isAppUpdateDownloading;

    [ObservableProperty]
    private double _appUpdateDownloadProgress;

    [ObservableProperty]
    private string? _downloadedAppPackagePath;

    [ObservableProperty]
    private AppReleaseInfo? _latestAppRelease;

    [ObservableProperty]
    private string _gamePath = "";

    [ObservableProperty]
    private string _customGamePathInput = "";

    [ObservableProperty]
    private string _customLaunchArgsInput = "";

    [ObservableProperty]
    private bool _isGameFound;

    [ObservableProperty]
    private GameInstall? _currentInstall;

    [ObservableProperty]
    private bool _isBepInExInstalled;

    [ObservableProperty]
    private string _bepInExStatusText = "BepInEx: ...";

    [ObservableProperty]
    private string _bepInExStatusColor = "#ef4444";

    [ObservableProperty]
    private string _bepInExStatusTooltip = "";

    [ObservableProperty]
    private ObservableCollection<string> _profiles = [];

    [ObservableProperty]
    private string _selectedProfile = "Default";

    [ObservableProperty]
    private ObservableCollection<InstalledModItemViewModel> _installedMods = [];

    [ObservableProperty]
    private ObservableCollection<CatalogModItemViewModel> _catalogMods = [];

    [ObservableProperty]
    private string _catalogSearchText = "";

    [ObservableProperty]
    private string _installedSearchText = "";

    [ObservableProperty]
    private string _selectedSourceFilter = "Tutte le fonti";

    [ObservableProperty]
    private string _statusMessage = "Pronto";

    [ObservableProperty]
    private double _progressValue = 0.0;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private int _selectedTab = 0;

    // Modals / Overlays
    [ObservableProperty]
    private bool _isNewProfileDialogVisible;

    [ObservableProperty]
    private string _newProfileNameInput = "";

    [ObservableProperty]
    private bool _isImportR2CodeDialogVisible;

    [ObservableProperty]
    private string _r2CodeInput = "";

    [ObservableProperty]
    private bool _isShareProfileDialogVisible;

    [ObservableProperty]
    private string _generatedShareCode = "";

    [ObservableProperty]
    private bool _isShareCodeCopied;

    [ObservableProperty]
    private AppStrings _strings = ItalianStrings.Instance;

    [ObservableProperty]
    private string _selectedLanguageCode = LocalizationService.LanguageItalian;

    public IReadOnlyList<LanguageOption> AvailableLanguages => LocalizationService.Instance.AvailableLanguages;

    public LanguageOption? SelectedLanguage
    {
        get => AvailableLanguages.FirstOrDefault(l => l.Code == SelectedLanguageCode) ?? AvailableLanguages[0];
        set
        {
            if (value != null && value.Code != SelectedLanguageCode)
            {
                SetLanguage(value.Code);
            }
        }
    }

    [ObservableProperty]
    private IReadOnlyList<string> _sourceFilterOptions = [];

    public string FormattedUpdateBannerTitle => string.Format(Strings.FooterBannerUpdateAvailable, LatestAppVersion);
    public string FormattedInstallUpdateBanner => string.Format(Strings.HeaderBannerInstallUpdate, LatestAppVersion);
    public string FormattedCurrentAppVersion => string.Format(Strings.AppVersionCurrentPrefix, CurrentAppVersion);
    public string FormattedDownloadAppUpdateButton => string.Format(Strings.DownloadAppUpdateButton, LatestAppVersion);

    public MainViewModel()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _profileService = new ProfileService();
        _bepInExService = new BepInExService();

        var tsProvider = new ThunderstoreCompatibleProvider(ThunderstoreSourceOptions.Thunderstore, _httpClient);
        var hexProvider = new ThunderstoreCompatibleProvider(ThunderstoreSourceOptions.Hexium, _httpClient);

        _catalogService = new CatalogService([tsProvider, hexProvider]);
        _installService = new InstallService(_httpClient);
        _dependencyResolver = new DependencyResolver(_catalogService);
        _updateService = new UpdateService(_catalogService);
        _appUpdateService = new AppUpdateService(_httpClient);
        _r2Importer = new R2ModmanImporter(_httpClient, _profileService, _catalogService);
        _profileShareService = new ProfileShareService();
        _manualInstaller = new ManualModInstaller();

        if (OperatingSystem.IsWindows())
        {
            _steamLocator = new WindowsSteamLocator();
            _processMonitor = new WindowsProcessMonitor();
        }
        else if (OperatingSystem.IsLinux())
        {
            _steamLocator = new LinuxSteamLocator();
            _processMonitor = new LinuxProcessMonitor();
        }
        else
        {
            _steamLocator = new DummySteamLocator();
            _processMonitor = new DummyProcessMonitor();
        }

        _gameLauncher = new GameLauncher(_bepInExService, _processMonitor);

        // Determine current app version dynamically
        CurrentAppVersion = ResolveCurrentAppVersion();

        // Language setup
        var state = _profileService.LoadState();
        var initialLang = LocalizationService.Instance.NormalizeLanguageCode(state.Language);
        SetLanguageInternal(initialLang, saveState: false);

        // Initial setup
        DetectGame();
        LoadCustomArgs();
        LoadProfilesList();
        _ = InitializeCatalogAsync();
        _ = CheckAppUpdateAsync(silent: true);
    }

    public void DetectGame()
    {
        var state = _profileService.LoadState();
        if (!string.IsNullOrWhiteSpace(state.CustomGamePath) && Directory.Exists(state.CustomGamePath))
        {
            var exe = Path.Combine(state.CustomGamePath, "valheim.exe");
            if (File.Exists(exe))
            {
                CurrentInstall = new GameInstall(GameTarget.Client, state.CustomGamePath, exe);
                GamePath = state.CustomGamePath;
                CustomGamePathInput = state.CustomGamePath;
                IsGameFound = true;
                UpdateBepInExStatus();
                return;
            }
        }

        var installs = _steamLocator.FindInstalls();
        var clientInstall = installs.FirstOrDefault(i => i.Target == GameTarget.Client);

        if (clientInstall != null)
        {
            CurrentInstall = clientInstall;
            GamePath = clientInstall.GameDirectory;
            CustomGamePathInput = clientInstall.GameDirectory;
            IsGameFound = true;
        }
        else
        {
            CurrentInstall = null;
            GamePath = "Valheim non rilevato automaticamente.";
            CustomGamePathInput = "";
            IsGameFound = false;
        }

        UpdateBepInExStatus();
    }

    [RelayCommand]
    public void ApplyCustomGamePath()
    {
        if (string.IsNullOrWhiteSpace(CustomGamePathInput)) return;
        SetCustomGamePath(CustomGamePathInput.Trim());
    }
    
    public void SetCustomGamePath(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            StatusMessage = Strings.StatusFolderNotExists;
            return;
        }

        var exe = Path.Combine(folderPath, "valheim.exe");
        if (!File.Exists(exe))
        {
            StatusMessage = Strings.StatusExeNotFound;
            return;
        }

        var state = _profileService.LoadState();
        _profileService.SaveState(state with { CustomGamePath = folderPath });

        CurrentInstall = new GameInstall(GameTarget.Client, folderPath, exe);
        GamePath = folderPath;
        CustomGamePathInput = folderPath;
        IsGameFound = true;
        UpdateBepInExStatus();
        StatusMessage = Strings.StatusGamePathConfigured;
    }

    [RelayCommand]
    public void ApplyCustomLaunchArgs()
    {
        SetCustomLaunchArgs(CustomLaunchArgsInput);
    }

    public void SetCustomLaunchArgs(string? launchArgs)
    {
        var trimmed = string.IsNullOrWhiteSpace(launchArgs) ? null : launchArgs.Trim();

        var state = _profileService.LoadState();
        _profileService.SaveState(state with { CustomLaunchArgs = trimmed });

        CustomLaunchArgsInput = trimmed ?? "";
        StatusMessage = trimmed == null
            ? Strings.StatusLaunchArgsEmpty
            : Strings.StatusLaunchArgsSaved;
    }

    [RelayCommand]
    public void AppendLaunchArg(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg)) return;
        var current = CustomLaunchArgsInput?.Trim() ?? "";
        if (!current.Contains(arg, StringComparison.OrdinalIgnoreCase))
        {
            CustomLaunchArgsInput = string.IsNullOrEmpty(current) ? arg : $"{current} {arg}";
            ApplyCustomLaunchArgs();
        }
    }

    [RelayCommand]
    public void ClearLaunchArgs()
    {
        CustomLaunchArgsInput = "";
        ApplyCustomLaunchArgs();
    }

    [RelayCommand]
    public void SetLanguage(string languageCode)
    {
        SetLanguageInternal(languageCode, saveState: true);
    }

    private void SetLanguageInternal(string languageCode, bool saveState)
    {
        var norm = LocalizationService.Instance.NormalizeLanguageCode(languageCode);
        SelectedLanguageCode = norm;
        Strings = LocalizationService.Instance.GetStrings(norm);
        LocalizationService.Instance.CurrentStrings = Strings;
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(FormattedUpdateBannerTitle));
        OnPropertyChanged(nameof(FormattedInstallUpdateBanner));
        OnPropertyChanged(nameof(FormattedCurrentAppVersion));
        OnPropertyChanged(nameof(FormattedDownloadAppUpdateButton));

        UpdateSourceFilterOptions();
        UpdateBepInExStatus();

        if (InstalledMods.Count > 0)
        {
            LoadInstalledMods();
        }
        if (CatalogMods.Count > 0)
        {
            ApplyCatalogSearch();
        }

        if (StatusMessage == "Pronto" || StatusMessage == "Ready" || string.IsNullOrWhiteSpace(StatusMessage))
        {
            StatusMessage = Strings.CommonReady;
        }

        if (saveState)
        {
            var state = _profileService.LoadState();
            _profileService.SaveState(state with { Language = norm });
        }
    }

    private void UpdateSourceFilterOptions()
    {
        var prevFilter = SelectedSourceFilter;
        SourceFilterOptions = [Strings.SourceFilterAll, "Thunderstore", "Hexium"];

        if (prevFilter == "Thunderstore")
        {
            SelectedSourceFilter = "Thunderstore";
        }
        else if (prevFilter == "Hexium")
        {
            SelectedSourceFilter = "Hexium";
        }
        else
        {
            SelectedSourceFilter = SourceFilterOptions[0];
        }
    }

    public void UpdateBepInExStatus()
    {
        if (Strings == null) return;
        var profileDir = _profileService.GetProfileDirectory(SelectedProfile);
        var status = _bepInExService.GetStatus(CurrentInstall, profileDir);

        IsBepInExInstalled = status.IsInstalledInProfile && status.IsGameConfigured;

        if (IsBepInExInstalled)
        {
            var ver = status.Version ?? "5.4.x";
            BepInExStatusText = string.Format(Strings.BepInExConfiguredShort, ver);
            BepInExStatusColor = "#22c55e";
            BepInExStatusTooltip = string.Format(Strings.BepInExTooltipConfigured, ver);
        }
        else if (status.IsInstalledInProfile)
        {
            BepInExStatusText = Strings.BepInExHooksPendingShort;
            BepInExStatusColor = "#f59e0b";
            BepInExStatusTooltip = Strings.BepInExTooltipHooksPending;
        }
        else
        {
            BepInExStatusText = Strings.BepInExNotInstalledShort;
            BepInExStatusColor = "#ef4444";
            BepInExStatusTooltip = Strings.BepInExTooltipNotInstalled;
        }
    }

    private void LoadProfilesList()
    {
        var list = _profileService.ListProfileNames();
        Profiles.Clear();
        foreach (var p in list) Profiles.Add(p);

        var active = _profileService.GetActiveProfileName();
        if (Profiles.Contains(active))
        {
            SelectedProfile = active;
        }
        else if (Profiles.Count > 0)
        {
            SelectedProfile = Profiles[0];
        }

        LoadInstalledMods();
    }

    private void LoadCustomArgs()
    {
        var state = _profileService.LoadState();
        CustomLaunchArgsInput = state.CustomLaunchArgs ?? "";
    }

    partial void OnSelectedProfileChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        _profileService.SetActiveProfile(value);
        UpdateBepInExStatus();
        LoadInstalledMods();
    }

    // DEBOUNCE RICERCA MOD CATALOGO
    partial void OnCatalogSearchTextChanged(string value)
    {
        _catalogSearchCts?.Cancel();
        _catalogSearchCts = new CancellationTokenSource();
        var token = _catalogSearchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                Avalonia.Threading.Dispatcher.UIThread.Post(ApplyCatalogSearch);
            }
            catch (OperationCanceledException) { }
        });
    }

    // DEBOUNCE RICERCA MOD INSTALLATE
    partial void OnInstalledSearchTextChanged(string value)
    {
        _installedSearchCts?.Cancel();
        _installedSearchCts = new CancellationTokenSource();
        var token = _installedSearchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(200, token);
                Avalonia.Threading.Dispatcher.UIThread.Post(LoadInstalledMods);
            }
            catch (OperationCanceledException) { }
        });
    }

    // FIX TRIGGER FILTRO SORGENTE
    partial void OnSelectedSourceFilterChanged(string value)
    {
        ApplyCatalogSearch();
    }

    [RelayCommand]
    public void CheckUpdates() => LoadInstalledMods();

    public void LoadInstalledMods()
    {
        var profile = _profileService.GetProfile(SelectedProfile);
        var updateResults = _updateService.CheckUpdates(profile);
        var updateMap = updateResults.ToDictionary(r => r.InstalledMod.Key, r => r);

        InstalledMods.Clear();
        foreach (var mod in profile.Mods)
        {
            if (!string.IsNullOrWhiteSpace(InstalledSearchText) &&
                !mod.CanonicalId.Name.Contains(InstalledSearchText, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var hasUpd = updateMap.TryGetValue(mod.Key, out var upd) && upd.HasUpdate;
            var latest = upd?.LatestVersion ?? mod.InstalledVersion;

            InstalledMods.Add(new InstalledModItemViewModel(mod, latest, hasUpd, (vm, isEnabled) => OnModToggleChanged(vm, isEnabled)));
        }
    }

    private async Task InitializeCatalogAsync()
    {
        IsBusy = true;
        StatusMessage = Strings.StatusCatalogLoading;
        await _catalogService.LoadFromLocalCacheAsync();

        if (_catalogService.TotalPackageCount == 0)
        {
            StatusMessage = Strings.StatusCatalogFirstRun;
            await RefreshOnlineCatalogAsync();
        }
        else
        {
            ApplyCatalogSearch();
            StatusMessage = string.Format(Strings.StatusCatalogReady, _catalogService.TotalPackageCount);
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RefreshOnlineCatalogAsync()
    {
        IsBusy = true;
        StatusMessage = Strings.StatusCatalogUpdating;
        ProgressValue = 0.1;

        try
        {
            await _catalogService.RefreshAllAsync();
            ApplyCatalogSearch();
            StatusMessage = string.Format(Strings.StatusCatalogReady, _catalogService.TotalPackageCount);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusCatalogError, ex.Message);
        }
        finally
        {
            IsBusy = false;
            ProgressValue = 0.0;
        }
    }

    [RelayCommand]
    public void ApplyCatalogSearch()
    {
        string? providerId = SelectedSourceFilter switch
        {
            "Thunderstore" => "thunderstore",
            "Hexium" => "hexium",
            _ => null
        };

        var query = new ModQuery(
            SearchText: CatalogSearchText,
            ProviderId: providerId,
            PageSize: 100
        );

        var results = _catalogService.SearchGrouped(query);
        CatalogMods.Clear();
        foreach (var mod in results)
        {
            CatalogMods.Add(new CatalogModItemViewModel(mod));
        }
    }

    [RelayCommand]
    public async Task InstallModAsync(CatalogModItemViewModel item)
    {
        if (item == null || item.IsInstalling) return;

        item.IsInstalling = true;
        IsBusy = true;
        var chosenSource = item.SelectedSource;
        var chosenSummary = chosenSource.Summary;
        var chosenVersion = item.SelectedVersion;

        StatusMessage = string.Format(Strings.StatusDownloadingMod, item.Name, chosenVersion, chosenSource.DisplayName);

        try
        {
            var profile = _profileService.GetProfile(SelectedProfile);
            var profileBepDir = _profileService.GetProfileBepInExDirectory(SelectedProfile);

            // 1. Resolve and install dependencies first
            var dependencies = _dependencyResolver.ResolveDependencies(chosenSummary, chosenVersion, profile);
            foreach (var dep in dependencies)
            {
                if (dep.AlreadyInstalled) continue;

                StatusMessage = string.Format(Strings.StatusInstallingDependency, dep.Summary.Name, dep.RequiredVersion);
                var depVersion = dep.Summary.Versions.FirstOrDefault(v => v.VersionNumber == dep.RequiredVersion)
                                 ?? dep.Summary.Versions.First();

                var depTicket = new DownloadTicket(new Uri(depVersion.DownloadUrl), $"{dep.Summary.Name}-{dep.RequiredVersion}.zip");
                var depZip = await _installService.DownloadPackageAsync(depTicket, dep.Summary.Key.ProviderId, dep.Summary.Name, dep.RequiredVersion);
                var depFiles = _installService.InstallZipToProfile(depZip, profileBepDir, dep.CanonicalId);

                profile.Mods.RemoveAll(m => m.CanonicalId == dep.CanonicalId);
                profile.Mods.Add(new InstalledMod(
                    Key: dep.Summary.Key,
                    CanonicalId: dep.CanonicalId,
                    InstalledVersion: dep.RequiredVersion,
                    IsEnabled: true,
                    InstalledAt: DateTime.UtcNow,
                    InstalledFiles: depFiles,
                    Dependencies: depVersion.Dependencies.Select(d => d.RawIdentifier).ToList()
                ));
            }

            // 2. Install root mod from selected source & version
            var versionObj = chosenSummary.Versions.FirstOrDefault(v => v.VersionNumber == chosenVersion)
                             ?? chosenSummary.Versions.First();

            var ticket = new DownloadTicket(new Uri(versionObj.DownloadUrl), $"{item.Name}-{chosenVersion}.zip");
            var zipPath = await _installService.DownloadPackageAsync(ticket, chosenSource.ProviderId, item.Name, chosenVersion);
            var files = _installService.InstallZipToProfile(zipPath, profileBepDir, chosenSummary.CanonicalId);

            profile.Mods.RemoveAll(m => m.CanonicalId == chosenSummary.CanonicalId);
            profile.Mods.Add(new InstalledMod(
                Key: chosenSummary.Key,
                CanonicalId: chosenSummary.CanonicalId,
                InstalledVersion: chosenVersion,
                IsEnabled: true,
                InstalledAt: DateTime.UtcNow,
                InstalledFiles: files,
                Dependencies: versionObj.Dependencies.Select(d => d.RawIdentifier).ToList()
            ));

            _profileService.SaveProfile(profile);
            LoadInstalledMods();
            StatusMessage = string.Format(Strings.StatusModInstalledInProfile, item.Name, chosenVersion, chosenSource.DisplayName, SelectedProfile);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusModInstallError, item.Name, ex.Message);
        }
        finally
        {
            item.IsInstalling = false;
            IsBusy = false;
        }
    }

    private void OnModToggleChanged(InstalledModItemViewModel item, bool enabled)
    {
        var profile = _profileService.GetProfile(SelectedProfile);
        var targetMod = profile.Mods.FirstOrDefault(m => m.Key == item.Model.Key);
        if (targetMod == null) return;

        try
        {
            var updatedFiles = _installService.ToggleMod(targetMod.InstalledFiles, enabled);

            var idx = profile.Mods.IndexOf(targetMod);
            profile.Mods[idx] = targetMod with 
            { 
                IsEnabled = enabled,
                InstalledFiles = updatedFiles
            };
            _profileService.SaveProfile(profile);

            StatusMessage = $"{item.Name} {(enabled ? Strings.StatusModEnabled : Strings.StatusModDisabled)}.";
        }
        catch (Exception ex)
        {
            item.SetIsEnabledSilently(!enabled);
            StatusMessage = string.Format(Strings.StatusModToggleError, item.Name, ex.Message);
        }
    }

    [RelayCommand]
    public void ToggleMod(InstalledModItemViewModel item)
    {
        if (item == null) return;
        item.IsEnabled = !item.IsEnabled;
    }

    [RelayCommand]
    public void UninstallMod(InstalledModItemViewModel item)
    {
        if (item == null) return;
        var profile = _profileService.GetProfile(SelectedProfile);
        var targetMod = profile.Mods.FirstOrDefault(m => m.Key == item.Model.Key);
        if (targetMod == null) return;

        _installService.UninstallMod(targetMod.InstalledFiles);
        profile.Mods.Remove(targetMod);
        _profileService.SaveProfile(profile);

        InstalledMods.Remove(item);
        StatusMessage = string.Format(Strings.StatusModUninstalled, item.Name);
    }

    public async Task InstallManualModFilesAsync(IReadOnlyList<string> filePaths)
    {
        if (filePaths == null || filePaths.Count == 0) return;

        IsBusy = true;
        try
        {
            var profileBepDir = _profileService.GetProfileBepInExDirectory(SelectedProfile);
            var profile = _profileService.GetProfile(SelectedProfile);
            int installedCount = 0;

            foreach (var filePath in filePaths)
            {
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext != ".zip" && ext != ".dll")
                {
                    StatusMessage = string.Format(Strings.StatusDropNotSupported, Path.GetFileName(filePath));
                    continue;
                }

                StatusMessage = string.Format(Strings.StatusManualModInstalling, Path.GetFileName(filePath));

                try
                {
                    var installedMod = await Task.Run(() => _manualInstaller.InstallFromFile(filePath, profileBepDir));
                    profile.Mods.RemoveAll(m => m.CanonicalId == installedMod.CanonicalId);
                    profile.Mods.Add(installedMod);
                    installedCount++;
                }
                catch (Exception ex)
                {
                    StatusMessage = string.Format(Strings.StatusManualModError, ex.Message);
                }
            }

            if (installedCount > 0)
            {
                _profileService.SaveProfile(profile);
                LoadInstalledMods();
                StatusMessage = string.Format(Strings.StatusManualModSuccess, Path.GetFileName(filePaths[0]));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task UpdateModAsync(InstalledModItemViewModel item)
    {
        if (item == null) return;
        var catMod = _catalogService.FindByCanonicalId(item.Model.CanonicalId, item.Model.Key.ProviderId);
        if (catMod != null)
        {
            var vm = new CatalogModItemViewModel(catMod) { SelectedVersion = catMod.LatestVersionNumber };
            await InstallModAsync(vm);
        }
    }

    [RelayCommand]
    public async Task UpdateAllModsAsync()
    {
        var outdated = InstalledMods.Where(m => m.HasUpdate).ToList();
        if (outdated.Count == 0)
        {
            StatusMessage = Strings.StatusAllModsAlreadyUpToDate;
            return;
        }

        foreach (var mod in outdated)
        {
            await UpdateModAsync(mod);
        }

        StatusMessage = Strings.StatusAllModsUpdatedSuccess;
    }

    [RelayCommand]
    public async Task InstallBepInExAsync()
    {
        if (CurrentInstall == null)
        {
            StatusMessage = Strings.StatusSpecifyGameFolderFirst;
            return;
        }

        IsBusy = true;
        StatusMessage = Strings.StatusDownloadingBepInEx;
        try
        {
            var bepMod = _catalogService.FindByCanonicalId(new CanonicalModId("denikson", "BepInExPack_Valheim"));
            string downloadUrl;
            string version;

            if (bepMod != null && bepMod.Versions.Count > 0)
            {
                downloadUrl = bepMod.Versions[0].DownloadUrl;
                version = bepMod.LatestVersionNumber;
            }
            else
            {
                version = "5.4.2351";
                downloadUrl = $"https://thunderstore.io/package/download/denikson/BepInExPack_Valheim/{version}/";
            }

            var ticket = new DownloadTicket(new Uri(downloadUrl), $"BepInExPack_Valheim-{version}.zip");
            var zipPath = await _installService.DownloadPackageAsync(ticket, "thunderstore", "BepInExPack_Valheim", version);

            var profileDir = _profileService.GetProfileDirectory(SelectedProfile);
            _bepInExService.InstallToProfile(zipPath, profileDir);
            _bepInExService.DeployToGame(zipPath, CurrentInstall, profileDir);

            UpdateBepInExStatus();
            StatusMessage = Strings.StatusBepInExInstalledSuccess;
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusBepInExInstallError, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void RestoreVanilla()
    {
        if (CurrentInstall == null) return;
        _bepInExService.RestoreVanilla(CurrentInstall);
        UpdateBepInExStatus();
        StatusMessage = Strings.StatusVanillaRestored;
    }

    [RelayCommand]
    public void LaunchGame()
    {
        if (CurrentInstall == null)
        {
            StatusMessage = Strings.StatusGamePathNotFound;
            return;
        }

        var profileDir = _profileService.GetProfileDirectory(SelectedProfile);
        var profileArgs = string.IsNullOrWhiteSpace(CustomLaunchArgsInput) ? null : CustomLaunchArgsInput.Trim();
        try
        {
            _gameLauncher.LaunchGame(CurrentInstall, profileDir, profileArgs);
            StatusMessage = string.Format(Strings.StatusGameLaunched, SelectedProfile);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusGameLaunchError, ex.Message);
        }
    }

    [RelayCommand]
    public void LaunchServer()
    {
        var installs = _steamLocator.FindInstalls();
        var serverInstall = installs.FirstOrDefault(i => i.Target == GameTarget.DedicatedServer);

        if (serverInstall == null)
        {
            StatusMessage = Strings.StatusServerNotDetected;
            return;
        }

        var profileDir = _profileService.GetProfileDirectory(SelectedProfile);
        try
        {
            _gameLauncher.LaunchGame(serverInstall, profileDir, "-nographics -batchmode");
            StatusMessage = string.Format(Strings.StatusServerLaunched, SelectedProfile);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusServerLaunchError, ex.Message);
        }
    }

    // GESTIONE PROFILI: MODALI E COMANDI
    [RelayCommand]
    public void ShowNewProfileDialog()
    {
        NewProfileNameInput = "";
        IsNewProfileDialogVisible = true;
    }

    [RelayCommand]
    public void ConfirmNewProfile()
    {
        if (string.IsNullOrWhiteSpace(NewProfileNameInput)) return;
        CreateNewProfile(NewProfileNameInput.Trim());
        IsNewProfileDialogVisible = false;
    }

    [RelayCommand]
    public void CancelNewProfileDialog()
    {
        IsNewProfileDialogVisible = false;
    }

    public void CreateNewProfile(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        try
        {
            _profileService.CreateProfile(name);
            LoadProfilesList();
            SelectedProfile = name;
            StatusMessage = string.Format(Strings.StatusProfileCreated, name);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusProfileCreateError, ex.Message);
        }
    }

    [RelayCommand]
    public void CloneActiveProfile()
    {
        var baseName = SelectedProfile;
        var newName = $"{baseName} (Copia)";
        int counter = 2;
        var existing = _profileService.ListProfileNames();
        while (existing.Contains(newName, StringComparer.OrdinalIgnoreCase))
        {
            newName = $"{baseName} (Copia {counter++})";
        }

        try
        {
            _profileService.CloneProfile(baseName, newName);
            LoadProfilesList();
            SelectedProfile = newName;
            StatusMessage = string.Format(Strings.StatusProfileCloned, newName);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusProfileCloneError, ex.Message);
        }
    }

    [RelayCommand]
    public void DeleteCurrentProfile()
    {
        if (SelectedProfile.Equals("Default", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = Strings.StatusDefaultProfileCannotDelete;
            return;
        }

        try
        {
            var old = SelectedProfile;
            _profileService.DeleteProfile(old);
            LoadProfilesList();
            StatusMessage = string.Format(Strings.StatusProfileDeleted, old);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusProfileDeleteError, ex.Message);
        }
    }

    [RelayCommand]
    public void ShowImportR2CodeDialog()
    {
        R2CodeInput = "";
        IsImportR2CodeDialogVisible = true;
    }

    [RelayCommand]
    public async Task ConfirmImportR2CodeAsync()
    {
        if (string.IsNullOrWhiteSpace(R2CodeInput)) return;
        var input = R2CodeInput.Trim();
        IsBusy = true;

        try
        {
            if (_profileShareService.IsVmmShareCode(input))
            {
                StatusMessage = "Importazione profilo da codice VMM...";
                var manifest = _profileShareService.ParseShareCode(input);

                var existingNames = _profileService.ListProfileNames();
                var uniqueName = manifest.ProfileName;
                int counter = 2;
                while (existingNames.Contains(uniqueName, StringComparer.OrdinalIgnoreCase))
                {
                    uniqueName = $"{manifest.ProfileName} ({counter++})";
                }

                var profile = _profileService.CreateProfile(uniqueName, manifest.Target);
                var profileDir = _profileService.GetProfileDirectory(profile.Name);
                var profileBepDir = _profileService.GetProfileBepInExDirectory(profile.Name);

                // Extract config files
                var configDir = Path.Combine(profileDir, "BepInEx", "config");
                Directory.CreateDirectory(configDir);
                foreach (var (relPath, content) in manifest.ConfigFiles)
                {
                    try
                    {
                        var destPath = Path.Combine(configDir, relPath.Replace('/', Path.DirectorySeparatorChar));
                        var parentDir = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(parentDir)) Directory.CreateDirectory(parentDir);
                        File.WriteAllText(destPath, content, System.Text.Encoding.UTF8);
                    }
                    catch { }
                }

                // Download & install catalog mods
                int installedCount = 0;
                foreach (var modEntry in manifest.Mods)
                {
                    try
                    {
                        var canonical = CanonicalModId.Parse(modEntry.CanonicalId);
                        var catalogMatch = _catalogService.FindByCanonicalId(canonical);
                        if (catalogMatch != null)
                        {
                            var targetVersion = catalogMatch.Versions.FirstOrDefault(v => v.VersionNumber == modEntry.Version)
                                                ?? catalogMatch.Versions.First();

                            StatusMessage = string.Format(Strings.StatusDownloadingMod, catalogMatch.Name, targetVersion.VersionNumber, catalogMatch.Key.ProviderId);

                            var ticket = new DownloadTicket(new Uri(targetVersion.DownloadUrl), $"{catalogMatch.Name}-{targetVersion.VersionNumber}.zip");
                            var zip = await _installService.DownloadPackageAsync(ticket, catalogMatch.Key.ProviderId, catalogMatch.Name, targetVersion.VersionNumber);
                            var files = _installService.InstallZipToProfile(zip, profileBepDir, catalogMatch.CanonicalId);

                            if (!modEntry.IsEnabled)
                            {
                                files = _installService.ToggleMod(files, false);
                            }

                            profile.Mods.Add(new InstalledMod(
                                Key: catalogMatch.Key,
                                CanonicalId: catalogMatch.CanonicalId,
                                InstalledVersion: targetVersion.VersionNumber,
                                IsEnabled: modEntry.IsEnabled,
                                InstalledAt: DateTime.UtcNow,
                                InstalledFiles: files,
                                Dependencies: targetVersion.Dependencies.Select(d => d.RawIdentifier).ToList()
                            ));

                            installedCount++;
                        }
                    }
                    catch { }
                }

                _profileService.SaveProfile(profile);
                LoadProfilesList();
                SelectedProfile = profile.Name;
                IsImportR2CodeDialogVisible = false;
                StatusMessage = string.Format(Strings.StatusShareImportSuccess, profile.Name, installedCount);
            }
            else
            {
                StatusMessage = Strings.StatusR2Importing;
                var res = await _r2Importer.ImportFromCodeAsync(input);
                LoadProfilesList();
                SelectedProfile = res.CreatedProfile.Name;
                IsImportR2CodeDialogVisible = false;
                StatusMessage = string.Format(Strings.StatusR2ImportSuccess, res.CreatedProfile.Name, res.ResolvedCatalogMods.Count);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusR2ImportError, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void CancelImportR2CodeDialog()
    {
        IsImportR2CodeDialogVisible = false;
    }

    [RelayCommand]
    public void ShowShareProfileDialog()
    {
        try
        {
            var profile = _profileService.GetProfile(SelectedProfile);
            var profileDir = _profileService.GetProfileDirectory(SelectedProfile);
            GeneratedShareCode = _profileShareService.GenerateShareCode(profile, profileDir);
            IsShareCodeCopied = false;
            IsShareProfileDialogVisible = true;
            OnCopyToClipboardRequested?.Invoke(GeneratedShareCode);
            IsShareCodeCopied = true;
            StatusMessage = Strings.StatusShareCodeCopied;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    public void CopyGeneratedShareCode()
    {
        if (string.IsNullOrWhiteSpace(GeneratedShareCode)) return;
        OnCopyToClipboardRequested?.Invoke(GeneratedShareCode);
        IsShareCodeCopied = true;
        StatusMessage = Strings.StatusShareCodeCopied;
    }

    [RelayCommand]
    public void DismissShareProfileDialog()
    {
        IsShareProfileDialogVisible = false;
    }

    public void ExportServerPackage(string destinationZip)
    {
        try
        {
            var profileDir = _profileService.GetProfileDirectory(SelectedProfile);
            _profileShareService.ExportServerPackage(profileDir, destinationZip);
            StatusMessage = string.Format(Strings.StatusServerExportSuccess, destinationZip);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusServerExportError, ex.Message);
        }
    }

    public async Task ImportR2zFileAsync(string filePath)
    {
        IsBusy = true;
        StatusMessage = string.Format(Strings.StatusR2FileImporting, Path.GetFileName(filePath));
        try
        {
            var res = await _r2Importer.ImportFromR2zAsync(filePath);
            LoadProfilesList();
            SelectedProfile = res.CreatedProfile.Name;
            StatusMessage = string.Format(Strings.StatusVmmImportSuccess, res.CreatedProfile.Name);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusR2ImportError, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ImportVmmProfileFile(string filePath)
    {
        try
        {
            var imported = _profileService.ImportProfile(filePath);
            LoadProfilesList();
            SelectedProfile = imported.Name;
            StatusMessage = string.Format(Strings.StatusVmmImportSuccess, imported.Name);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusR2ImportError, ex.Message);
        }
    }

    public void ExportActiveProfile(string targetZip)
    {
        try
        {
            _profileService.ExportProfile(SelectedProfile, targetZip);
            StatusMessage = string.Format(Strings.StatusVmmExportSuccess, targetZip);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusVmmExportError, ex.Message);
        }
    }

    [RelayCommand]
    public async Task CheckAppUpdateManualAsync()
    {
        await CheckAppUpdateAsync(silent: false);
    }

    public async Task CheckAppUpdateAsync(bool silent = false)
    {
        if (IsAppUpdateChecking) return;

        IsAppUpdateChecking = true;
        if (!silent)
        {
            StatusMessage = Strings.StatusAppUpdateChecking;
        }

        try
        {
            var result = await _appUpdateService.CheckForUpdateAsync(CurrentAppVersion);
            if (result.HasUpdate && result.LatestRelease != null)
            {
                IsAppUpdateAvailable = true;
                IsAppUpdateBannerDismissed = false;
                LatestAppRelease = result.LatestRelease;
                LatestAppVersion = result.LatestRelease.Version;
                LatestAppReleaseTitle = result.LatestRelease.Title;
                LatestAppReleaseNotes = result.LatestRelease.ReleaseNotes;
                LatestAppReleaseUrl = result.LatestRelease.HtmlUrl;
                OnPropertyChanged(nameof(FormattedUpdateBannerTitle));
                OnPropertyChanged(nameof(FormattedInstallUpdateBanner));
                OnPropertyChanged(nameof(FormattedDownloadAppUpdateButton));
                AppUpdateStatusText = string.Format(Strings.StatusAppUpdateAvailable, result.LatestRelease.Version);
                if (!silent)
                {
                    StatusMessage = AppUpdateStatusText;
                    IsAppUpdateDialogVisible = true;
                }
            }
            else
            {
                IsAppUpdateAvailable = false;
                AppUpdateStatusText = Strings.StatusAppUpdateLatest;
                if (!silent)
                {
                    StatusMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? AppUpdateStatusText
                        : string.Format(Strings.StatusAppUpdateCheckFailed, result.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            if (!silent)
            {
                StatusMessage = string.Format(Strings.StatusAppUpdateCheckFailed, ex.Message);
            }
        }
        finally
        {
            IsAppUpdateChecking = false;
        }
    }

    [RelayCommand]
    public void ShowAppUpdateDialog()
    {
        if (LatestAppRelease != null)
        {
            IsAppUpdateDialogVisible = true;
        }
        else
        {
            _ = CheckAppUpdateManualAsync();
        }
    }

    [RelayCommand]
    public void DismissAppUpdateDialog()
    {
        IsAppUpdateDialogVisible = false;
    }

    [RelayCommand]
    public void DismissUpdateBanner()
    {
        IsAppUpdateBannerDismissed = true;
    }

    [RelayCommand]
    public async Task DownloadAppUpdateAsync()
    {
        if (LatestAppRelease == null || IsAppUpdateDownloading) return;

        var asset = _appUpdateService.SelectAssetForCurrentPlatform(LatestAppRelease);
        if (asset == null)
        {
            StatusMessage = Strings.StatusNoPackageForPlatform;
            return;
        }

        IsAppUpdateDownloading = true;
        AppUpdateDownloadProgress = 0;
        StatusMessage = string.Format(Strings.StatusAppUpdateDownloading, LatestAppRelease.Version);

        try
        {
            var downloadsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
            if (!Directory.Exists(downloadsDir))
            {
                downloadsDir = Path.GetTempPath();
            }

            var destFile = Path.Combine(downloadsDir, asset.Name);
            var progress = new Progress<double>(p => AppUpdateDownloadProgress = p * 100);

            await _appUpdateService.DownloadAssetAsync(asset, destFile, progress);
            DownloadedAppPackagePath = destFile;
            StatusMessage = string.Format(Strings.StatusAppUpdateDownloaded, destFile);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusAppUpdateDownloadError, ex.Message);
        }
        finally
        {
            IsAppUpdateDownloading = false;
        }
    }

    [RelayCommand]
    public void OpenDownloadedUpdate()
    {
        if (string.IsNullOrEmpty(DownloadedAppPackagePath) || !File.Exists(DownloadedAppPackagePath))
        {
            return;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{DownloadedAppPackagePath}\"",
                    UseShellExecute = true
                });
            }
            else
            {
                var dir = Path.GetDirectoryName(DownloadedAppPackagePath) ?? DownloadedAppPackagePath;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{dir}\"",
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusCannotOpenFolder, ex.Message);
        }
    }

    [RelayCommand]
    public void OpenGitHubReleasePage()
    {
        var targetUrl = !string.IsNullOrEmpty(LatestAppReleaseUrl)
            ? LatestAppReleaseUrl
            : "https://github.com/VasariRulez/ValheimModManager/releases";

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = targetUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Strings.StatusCannotOpenBrowser, ex.Message);
        }
    }

    private static string ResolveCurrentAppVersion()
    {
        try
        {
            var asm = typeof(MainViewModel).Assembly;

            // 1. Priorità: AssemblyInformationalVersionAttribute (versione semantica iniettata dal tag Git / -p:Version)
            var infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(infoVer))
            {
                var plusIdx = infoVer.IndexOf('+');
                var clean = plusIdx >= 0 ? infoVer[..plusIdx].Trim() : infoVer.Trim();
                if (!string.IsNullOrWhiteSpace(clean))
                {
                    return clean;
                }
            }

            // 2. Seconda priorità: AssemblyFileVersionAttribute
            var fileVer = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            if (!string.IsNullOrWhiteSpace(fileVer))
            {
                return fileVer.Trim();
            }

            // 3. Terza priorità: AssemblyName.Version (gestendo sia 3 che 4 componenti numerici)
            var ver = asm.GetName().Version;
            if (ver != null)
            {
                return ver.Revision > 0
                    ? $"{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}"
                    : $"{ver.Major}.{ver.Minor}.{ver.Build}";
            }
        }
        catch
        {
            // fallback
        }

        return "1.0.0";
    }

    private class DummySteamLocator : ISteamLocator
    {
        public IReadOnlyList<GameInstall> FindInstalls() => [];
    }

    private class DummyProcessMonitor : IProcessMonitor
    {
        public bool IsRunning(GameTarget target) => false;
    }
}
