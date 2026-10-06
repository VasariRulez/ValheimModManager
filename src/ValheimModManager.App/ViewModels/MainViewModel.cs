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
    private readonly GameLauncher _gameLauncher;
    private readonly ISteamLocator _steamLocator;
    private readonly IProcessMonitor _processMonitor;

    [ObservableProperty]
    private string _gamePath = "";

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
    private int _selectedTab = 0; // 0=Installed, 1=Browse, 2=Settings

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
            IsGameFound = true;
        }
        else
        {
            CurrentInstall = null;
            GamePath = "Valheim non rilevato automaticamente.";
            IsGameFound = false;
        }

        UpdateBepInExStatus();
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
            BepInExStatusText = "BepInEx: Installato nel profilo, ma ganci di gioco da configurare";
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

    partial void OnSelectedProfileChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        _profileService.SetActiveProfile(value);
        UpdateBepInExStatus();
        LoadInstalledMods();
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

            InstalledMods.Add(new InstalledModItemViewModel(mod, latest, hasUpd));
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

        var results = _catalogService.Search(query);
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
        StatusMessage = $"Download e installazione di {item.Name} v{item.SelectedVersion}...";

        try
        {
            var profile = _profileService.GetProfile(SelectedProfile);
            var profileBepDir = _profileService.GetProfileBepInExDirectory(SelectedProfile);

            // 1. Resolve and install dependencies first
            var dependencies = _dependencyResolver.ResolveDependencies(item.Summary, item.SelectedVersion, profile);
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

            // 2. Install root mod
            var versionObj = item.Summary.Versions.FirstOrDefault(v => v.VersionNumber == item.SelectedVersion)
                             ?? item.Summary.Versions.First();

            var ticket = new DownloadTicket(new Uri(versionObj.DownloadUrl), $"{item.Name}-{item.SelectedVersion}.zip");
            var zipPath = await _installService.DownloadPackageAsync(ticket, item.ProviderId, item.Name, item.SelectedVersion);
            var files = _installService.InstallZipToProfile(zipPath, profileBepDir, item.Summary.CanonicalId);

            profile.Mods.RemoveAll(m => m.CanonicalId == item.Summary.CanonicalId);
            profile.Mods.Add(new InstalledMod(
                Key: item.Summary.Key,
                CanonicalId: item.Summary.CanonicalId,
                InstalledVersion: item.SelectedVersion,
                IsEnabled: true,
                InstalledAt: DateTime.UtcNow,
                InstalledFiles: files,
                Dependencies: versionObj.Dependencies.Select(d => d.RawIdentifier).ToList()
            ));

            _profileService.SaveProfile(profile);
            LoadInstalledMods();
            StatusMessage = $"{item.Name} v{item.SelectedVersion} installata con successo nel profilo {SelectedProfile}!";
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

    [RelayCommand]
    public void ToggleMod(InstalledModItemViewModel item)
    {
        if (item == null) return;
        var profile = _profileService.GetProfile(SelectedProfile);
        var targetMod = profile.Mods.FirstOrDefault(m => m.Key == item.Model.Key);
        if (targetMod == null) return;

        var newState = !item.IsEnabled;
        _installService.ToggleMod(targetMod.InstalledFiles, newState);

        var idx = profile.Mods.IndexOf(targetMod);
        profile.Mods[idx] = targetMod with { IsEnabled = newState };
        _profileService.SaveProfile(profile);

        item.IsEnabled = newState;
        StatusMessage = $"{item.Name} {(newState ? "attivata" : "disattivata")}.";
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
                // Direct fallback
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
        try
        {
            _gameLauncher.LaunchGame(CurrentInstall, profileDir);
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

    [RelayCommand]
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
    public void ExportCurrentProfile(string targetZip)
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

    [RelayCommand]
    public void ImportProfile(string sourceZip)
    {
        try
        {
            var imported = _profileService.ImportProfile(sourceZip);
            LoadProfilesList();
            SelectedProfile = imported.Name;
            StatusMessage = $"Profilo [{imported.Name}] importato con successo!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore importazione: {ex.Message}";
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
