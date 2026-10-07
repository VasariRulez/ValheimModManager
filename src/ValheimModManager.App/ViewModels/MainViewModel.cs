namespace ValheimModManager.App.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ValheimModManager.Core.Abstractions;
using ValheimModManager.Core.Import;
using ValheimModManager.Core.Install;
using ValheimModManager.Core.Models;
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
    private readonly R2ModmanImporter _r2Importer;
    private readonly GameLauncher _gameLauncher;
    private readonly ISteamLocator _steamLocator;
    private readonly IProcessMonitor _processMonitor;

    private CancellationTokenSource? _catalogSearchCts;
    private CancellationTokenSource? _installedSearchCts;

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
    private string _bepInExStatusText = "BepInEx: Verifica in corso...";

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

    public IReadOnlyList<string> SourceFilterOptions { get; } =
        ["Tutte le fonti", "Thunderstore", "Hexium"];

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
        _r2Importer = new R2ModmanImporter(_httpClient, _profileService, _catalogService);

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

        // Initial setup
        DetectGame();
        LoadCustomArgs();
        LoadProfilesList();
        _ = InitializeCatalogAsync();
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
            StatusMessage = "La cartella specificata non esiste.";
            return;
        }

        var exe = Path.Combine(folderPath, "valheim.exe");
        if (!File.Exists(exe))
        {
            StatusMessage = "Attenzione: 'valheim.exe' non trovato nella cartella selezionata.";
            return;
        }

        var state = _profileService.LoadState();
        _profileService.SaveState(state with { CustomGamePath = folderPath });

        CurrentInstall = new GameInstall(GameTarget.Client, folderPath, exe);
        GamePath = folderPath;
        CustomGamePathInput = folderPath;
        IsGameFound = true;
        UpdateBepInExStatus();
        StatusMessage = "Cartella di Valheim configurata con successo!";
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
            ? "Opzioni di avvio personalizzate rimosse."
            : "Opzioni di avvio personalizzate impostate con successo!";
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

    public void UpdateBepInExStatus()
    {
        var profileDir = _profileService.GetProfileDirectory(SelectedProfile);
        var status = _bepInExService.GetStatus(CurrentInstall, profileDir);

        IsBepInExInstalled = status.IsInstalledInProfile && status.IsGameConfigured;

        if (IsBepInExInstalled)
        {
            BepInExStatusText = $"BepInEx: Configurato & Attivo ({status.Version ?? "5.4.x"})";
        }
        else if (status.IsInstalledInProfile)
        {
            BepInExStatusText = "BepInEx: Installato nel profilo, ganci di gioco da applicare";
        }
        else
        {
            BepInExStatusText = "BepInEx: Non installato (necessario per caricare le mod)";
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
        StatusMessage = "Caricamento catalogo locale...";
        await _catalogService.LoadFromLocalCacheAsync();

        if (_catalogService.TotalPackageCount == 0)
        {
            StatusMessage = "Primo avvio: aggiornamento catalogo da Thunderstore & Hexium...";
            await RefreshOnlineCatalogAsync();
        }
        else
        {
            ApplyCatalogSearch();
            StatusMessage = $"Catalogo pronto ({_catalogService.TotalPackageCount} pacchetti disponibili).";
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RefreshOnlineCatalogAsync()
    {
        IsBusy = true;
        StatusMessage = "Aggiornamento catalogo online da Thunderstore e Hexium in corso...";
        ProgressValue = 0.1;

        try
        {
            await _catalogService.RefreshAllAsync();
            ApplyCatalogSearch();
            StatusMessage = $"Catalogo aggiornato ({_catalogService.TotalPackageCount} pacchetti disponibili).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore aggiornamento catalogo: {ex.Message}";
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

        StatusMessage = $"Download e installazione di {item.Name} v{chosenVersion} da {chosenSource.DisplayName}...";

        try
        {
            var profile = _profileService.GetProfile(SelectedProfile);
            var profileBepDir = _profileService.GetProfileBepInExDirectory(SelectedProfile);

            // 1. Resolve and install dependencies first
            var dependencies = _dependencyResolver.ResolveDependencies(chosenSummary, chosenVersion, profile);
            foreach (var dep in dependencies)
            {
                if (dep.AlreadyInstalled) continue;

                StatusMessage = $"Installazione dipendenza: {dep.Summary.Name} v{dep.RequiredVersion}...";
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
            StatusMessage = $"{item.Name} v{chosenVersion} installata da {chosenSource.DisplayName} nel profilo [{SelectedProfile}]!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore durante l'installazione di {item.Name}: {ex.Message}";
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

            StatusMessage = $"{item.Name} {(enabled ? "attivata" : "disattivata")}.";
        }
        catch (Exception ex)
        {
            item.SetIsEnabledSilently(!enabled);
            StatusMessage = $"Errore durante la modifica di {item.Name}: {ex.Message}";
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
        StatusMessage = $"{item.Name} disinstallata.";
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
            StatusMessage = "Tutte le mod sono già aggiornate all'ultima versione!";
            return;
        }

        foreach (var mod in outdated)
        {
            await UpdateModAsync(mod);
        }

        StatusMessage = "Tutte le mod sono state aggiornate con successo!";
    }

    [RelayCommand]
    public async Task InstallBepInExAsync()
    {
        if (CurrentInstall == null)
        {
            StatusMessage = "Specificare prima la cartella di Valheim nelle impostazioni.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Download del pacchetto ufficiale BepInExPack_Valheim...";
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
            StatusMessage = "BepInEx installato e configurato con successo!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore installazione BepInEx: {ex.Message}";
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
        StatusMessage = "Gioco ripristinato allo stato Vanilla originale (ganci rimossi).";
    }

    [RelayCommand]
    public void LaunchGame()
    {
        if (CurrentInstall == null)
        {
            StatusMessage = "Percorso di Valheim non trovato.";
            return;
        }

        var profileDir = _profileService.GetProfileDirectory(SelectedProfile);
        var profileArgs = string.IsNullOrWhiteSpace(CustomLaunchArgsInput) ? null : CustomLaunchArgsInput.Trim();
        try
        {
            _gameLauncher.LaunchGame(CurrentInstall, profileDir, profileArgs);
            StatusMessage = $"Valheim avviato con il profilo [{SelectedProfile}]!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore avvio gioco: {ex.Message}";
        }
    }

    [RelayCommand]
    public void LaunchServer()
    {
        var installs = _steamLocator.FindInstalls();
        var serverInstall = installs.FirstOrDefault(i => i.Target == GameTarget.DedicatedServer);

        if (serverInstall == null)
        {
            StatusMessage = "Valheim Dedicated Server non rilevato nella libreria Steam (App ID 896660).";
            return;
        }

        var profileDir = _profileService.GetProfileDirectory(SelectedProfile);
        try
        {
            _gameLauncher.LaunchGame(serverInstall, profileDir, "-nographics -batchmode");
            StatusMessage = $"Valheim Dedicated Server avviato con il profilo [{SelectedProfile}]!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore avvio server: {ex.Message}";
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
            StatusMessage = $"Nuovo profilo [{name}] creato con successo.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore creazione profilo: {ex.Message}";
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
            StatusMessage = $"Profilo [{newName}] clonato con successo.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore clonazione profilo: {ex.Message}";
        }
    }

    [RelayCommand]
    public void DeleteCurrentProfile()
    {
        if (SelectedProfile.Equals("Default", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Il profilo predefinito 'Default' non può essere cancellato.";
            return;
        }

        try
        {
            var old = SelectedProfile;
            _profileService.DeleteProfile(old);
            LoadProfilesList();
            StatusMessage = $"Profilo [{old}] eliminato.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore eliminazione profilo: {ex.Message}";
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
        IsBusy = true;
        StatusMessage = "Download e importazione profilo da codice r2modman in corso...";
        try
        {
            var res = await _r2Importer.ImportFromCodeAsync(R2CodeInput.Trim());
            LoadProfilesList();
            SelectedProfile = res.CreatedProfile.Name;
            IsImportR2CodeDialogVisible = false;
            StatusMessage = $"Profilo [{res.CreatedProfile.Name}] importato con successo ({res.ResolvedCatalogMods.Count} mod risolte)!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore importazione codice: {ex.Message}";
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

    public async Task ImportR2zFileAsync(string filePath)
    {
        IsBusy = true;
        StatusMessage = $"Importazione profilo r2modman da file {Path.GetFileName(filePath)}...";
        try
        {
            var res = await _r2Importer.ImportFromR2zAsync(filePath);
            LoadProfilesList();
            SelectedProfile = res.CreatedProfile.Name;
            StatusMessage = $"Profilo [{res.CreatedProfile.Name}] importato con successo!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore importazione .r2z: {ex.Message}";
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
            StatusMessage = $"Profilo [{imported.Name}] importato con successo!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore importazione: {ex.Message}";
        }
    }

    public void ExportActiveProfile(string targetZip)
    {
        try
        {
            _profileService.ExportProfile(SelectedProfile, targetZip);
            StatusMessage = $"Profilo esportato in {targetZip}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore esportazione: {ex.Message}";
        }
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
