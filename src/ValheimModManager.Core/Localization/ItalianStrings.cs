namespace ValheimModManager.Core.Localization;

public sealed record ItalianStrings : AppStrings
{
    public static ItalianStrings Instance { get; } = new();

    private ItalianStrings()
    {
        // General / Common
        AppTitle = "VALHEIM MOD MANAGER";
        CommonSave = "Salva";
        CommonCancel = "Annulla";
        CommonClose = "Chiudi";
        CommonUpdate = "Aggiorna";
        CommonInstall = "Installa";
        CommonInstalled = "Installata";
        CommonUninstall = "Disinstalla";
        CommonReady = "Pronto";
        CommonLoading = "Caricamento in corso...";

        // Header Toolbar
        HeaderProfileLabel = "Profilo:";
        HeaderTooltipNewProfile = "Crea nuovo profilo";
        HeaderTooltipCloneProfile = "Clona profilo attivo";
        HeaderTooltipDeleteProfile = "Elimina profilo attivo";
        HeaderTooltipExportProfile = "Esporta profilo (.vmmprofile)";
        HeaderTooltipImportProfile = "Opzioni di importazione profilo";
        HeaderTooltipProfileOptions = "Altre opzioni profilo (clona, elimina, esporta, importa)";
        HeaderMenuCloneProfile = "📋  Clona profilo attivo";
        HeaderMenuDeleteProfile = "🗑️  Elimina profilo attivo";
        HeaderMenuShareProfileCode = "🔗  Condividi Profilo (Copia Codice)";
        HeaderMenuExportProfile = "📤  Esporta Profilo (.vmmprofile)";
        HeaderMenuExportServer = "🛡️  Esporta Pacchetto Server (.zip)";
        HeaderMenuImportShareCode = "🔑  Importa da Codice Condivisione (VMM)...";
        HeaderMenuImportVmmProfile = "📥  Importa Profilo (.vmmprofile)";
        HeaderMenuImportR2z = "📦  Importa da archivio r2modman (.r2z)";
        HeaderMenuImportR2Code = "⚡  Importa da Codice r2modman...";
        HeaderMenuOpenProfileFolder = "📁  Apri Cartella del Profilo Attivo";
        HeaderBannerUpdateAvailable = "🎉 Aggiornamento Disponibile!";
        HeaderBannerInstallUpdate = "Installa v{0}";
        FooterBannerUpdateAvailable = "È disponibile una nuova versione v{0} di Valheim Mod Manager!";
        FooterBannerDismissTooltip = "Ignora per ora";
        HeaderInstallBepInExButton = "Installa BepInEx 1-Click";
        HeaderInstallBepInExShort = "Installa";
        HeaderRestoreVanillaButton = "Ripristina Vanilla";
        HeaderRestoreVanillaShort = "Ripristina";
        HeaderLaunchGameButton = "▶  AVVIA VALHEIM";
        HeaderLaunchServerButton = "🛡️  Server";
        HeaderLaunchServerTooltip = "Avvia Valheim Dedicated Server con il profilo selezionato";

        // Tab 1: Installed Mods
        TabInstalledMods = "📦  Mod Installate";
        SearchInstalledPlaceholder = "Cerca tra le mod installate (filtro automatico)...";
        CheckUpdatesButton = "Verifica Aggiornamenti";
        UpdateAllModsButton = "Aggiorna Tutte le Mod";
        InstallFromFileButton = "➕  Installa da File...";
        ManualModBadge = "Manuale";
        FilePickerManualModTitle = "Seleziona mod da installare (.zip o .dll)";
        ModAuthorPrefix = "Autore: {0}";
        ModUpdateAvailableBadge = "Aggiornamento v{0} disp.";

        // Tab 2: Online Catalog
        TabOnlineCatalog = "🌐  Esplora Mod Online";
        SearchCatalogPlaceholder = "Cerca per nome, autore o descrizione (digitazione automatica)...";
        RefreshCatalogButton = "🔄  Aggiorna Catalogo";
        ModByPrefix = "di {0}";
        ModDownloadsLabel = "Download: {0:N0}";
        ModVotesLabel = "Voti: {0:N0}";
        ModInstallFromPrefix = "Installa da {0}";
        ModSourceLabel = "Fonte:";
        ModVersionLabel = "Versione:";
        SourceFilterAll = "Tutte le sorgenti";
        SourceFilterThunderstore = "Solo Thunderstore";
        SourceFilterHexium = "Solo Hexium";

        // Tab 3: Settings
        TabSettings = "⚙️  Impostazioni & Gioco";
        LanguageSectionTitle = "LINGUA APPLICAZIONE";
        LanguageSectionDescription = "Seleziona la lingua dell'interfaccia (il cambio è immediato):";
        GameFolderTitle = "CONFIGURAZIONE CARTELLA VALHEIM";
        GameFolderDescription = "Percorso cartella Valheim (deve contenere valheim.exe):";
        GameFolderPlaceholder = @"es. C:\Program Files (x86)\Steam\steamapps\common\Valheim";
        BrowseButton = "📂 Sfoglia...";
        AutoDetectSteamButton = "Rileva automaticamente da librerie Steam";
        SourcesTitle = "SORGENTI MOD DISPONIBILI";
        SourcesThunderstoreStatus = "(API v1 Valheim Community attiva)";
        SourcesHexiumStatus = "(API OpenAPI compatibile Thunderstore attiva)";
        SourcesNexusStatus = "(Architettura pianificata, supporto previsto nelle future versioni)";
        StorageSectionTitle = "ARCHIVIAZIONE DATI & PROFILI";
        StorageSectionDescription = "Cartella locale in cui il manager archivia i profili, le mod e le configurazioni (.cfg):";
        OpenProfilesFolderButton = "📂 Apri Cartella Profili";
        OpenActiveProfileFolderButton = "📂 Apri Profilo Attivo";
        AppUpdateSectionTitle = "AGGIORNAMENTO VALHEIM MOD MANAGER";
        AppVersionCurrentPrefix = "Versione attuale: v{0}";
        CheckAppUpdatesButton = "🔄 Controlla Aggiornamenti";
        DownloadAppUpdateButton = "Scarica v{0}";
        LaunchOptionsTitle = "CONFIGURAZIONE OPZIONI DI AVVIO";
        LaunchOptionsDescription = "Parametri da passare a Valheim all'avvio:";
        LaunchOptionsPlaceholder = "es. -console";
        QuickPresetsTitle = "Preset rapidi:";
        ClearLaunchOptionsButton = "Svuota opzioni";
        TooltipPresetConsole = "Abilita la console comandi in-game (tasto F5)";
        TooltipPresetExclusive = "Forza la modalità schermo intero esclusivo";
        TooltipPresetVulkan = "Avvia il gioco con le API grafiche Vulkan";

        // Modals
        NewProfileTitle = "Crea Nuovo Profilo";
        NewProfilePrompt = "Inserisci il nome per il nuovo set di mod:";
        NewProfilePlaceholder = "es. Server Amici, Vanilla+, Hardcore";
        CreateProfileButton = "Crea Profilo";
        ImportR2Title = "Importa da Codice r2modman / Thunderstore";
        ImportR2Prompt = "Incolla il codice alfanumerico di esportazione (es. 018f...):";
        ImportR2Placeholder = "Incolla codice di condivisione...";
        ImportR2Button = "Scarica & Importa";
        ImportShareCodeTitle = "Importa Profilo da Codice di Condivisione";
        ImportShareCodePrompt = "Incolla il codice generato da Valheim Mod Manager (vmm1-...):";
        ImportShareCodePlaceholder = "Incolla codice di condivisione (vmm1-...)...";
        ImportShareCodeButton = "Importa Profilo";
        ShareProfileTitle = "Condividi Profilo";
        ShareProfilePrompt = "Copia questo codice compresso per condividere il profilo con i tuoi amici:";
        ShareProfileCopyButton = "Copia Codice Negli Appunti";
        ShareProfileCopiedTooltip = "Copiato!";
        AppUpdateDialogTitle = "Aggiornamento Disponibile!";
        ReleaseNotesLabel = "Note di rilascio:";
        DownloadingPackageLabel = "Download del pacchetto in corso...";
        PackageDownloadedSuccess = "✅ Pacchetto scaricato nei Download!";
        OpenFolderButton = "Apri cartella";
        GitHubPageButton = "🌐 Pagina GitHub";
        DownloadPackageButton = "Scarica Pacchetto";

        // BepInEx Status
        BepInExConfigured = "BepInEx: Configurato & Attivo ({0})";
        BepInExConfiguredShort = "BepInEx: Attivo ({0})";
        BepInExHooksPending = "BepInEx: Installato nel profilo, ganci di gioco da applicare";
        BepInExHooksPendingShort = "BepInEx: Ganci da applicare";
        BepInExNotInstalled = "BepInEx: Non installato (necessario per caricare le mod)";
        BepInExNotInstalledShort = "BepInEx: Non installato";
        BepInExTooltipConfigured = "BepInEx {0} è configurato e attivo nel profilo selezionato.";
        BepInExTooltipHooksPending = "BepInEx è presente nel profilo ma i file di hooking devono essere applicati alla cartella del gioco. Clicca su Installa per applicarli.";
        BepInExTooltipNotInstalled = "BepInEx non è installato in questo profilo (richiesto per caricare le mod). Clicca su Installa per scaricarlo e configurarlo.";

        // Detailed Status Messages
        StatusFolderNotExists = "La cartella specificata non esiste.";
        StatusExeNotFound = "Attenzione: 'valheim.exe' non trovato nella cartella selezionata.";
        StatusGamePathConfigured = "Cartella di Valheim configurata con successo!";
        StatusLaunchArgsEmpty = "Opzioni di avvio rimosse.";
        StatusLaunchArgsSaved = "Opzioni di avvio personalizzate impostate con successo!";
        StatusCatalogLoading = "Caricamento catalogo locale...";
        StatusCatalogFirstRun = "Primo avvio: aggiornamento catalogo da Thunderstore & Hexium...";
        StatusCatalogReady = "Catalogo pronto ({0} pacchetti disponibili).";
        StatusCatalogUpdating = "Aggiornamento catalogo online da Thunderstore e Hexium in corso...";
        StatusCatalogError = "Errore aggiornamento catalogo: {0}";
        StatusDownloadingMod = "Download e installazione di {0} v{1} da {2}...";
        StatusInstallingDependency = "Installazione dipendenza: {0} v{1}...";
        StatusModInstalledInProfile = "{0} v{1} installata da {2} nel profilo [{3}]!";
        StatusModInstallError = "Errore durante l'installazione di {0}: {1}";
        StatusModEnabled = "attivata";
        StatusModDisabled = "disattivata";
        StatusModToggleError = "Errore durante la modifica di {0}: {1}";
        StatusModUninstalled = "{0} disinstallata.";
        StatusAllModsAlreadyUpToDate = "Tutte le mod sono già aggiornate all'ultima versione!";
        StatusAllModsUpdatedSuccess = "Tutte le mod sono state aggiornate con successo!";
        StatusSpecifyGameFolderFirst = "Specificare prima la cartella di Valheim nelle impostazioni.";
        StatusDownloadingBepInEx = "Download del pacchetto ufficiale BepInExPack_Valheim...";
        StatusBepInExInstalledSuccess = "BepInEx installato e configurato con successo!";
        StatusBepInExInstallError = "Errore installazione BepInEx: {0}";
        StatusVanillaRestored = "Gioco ripristinato allo stato Vanilla originale (ganci rimossi).";
        StatusGamePathNotFound = "Percorso di Valheim non trovato.";
        StatusGameLaunched = "Valheim avviato con il profilo [{0}]!";
        StatusGameLaunchError = "Errore avvio gioco: {0}";
        StatusServerNotDetected = "Valheim Dedicated Server non rilevato nella libreria Steam (App ID 896660).";
        StatusServerLaunched = "Valheim Dedicated Server avviato con il profilo [{0}]!";
        StatusServerLaunchError = "Errore avvio server: {0}";
        StatusProfileCreated = "Nuovo profilo [{0}] creato con successo.";
        StatusProfileCreateError = "Errore creazione profilo: {0}";
        StatusProfileCloned = "Profilo [{0}] clonato con successo.";
        StatusProfileCloneError = "Errore clonazione profilo: {0}";
        StatusDefaultProfileCannotDelete = "Il profilo predefinito 'Default' non può essere cancellato.";
        StatusProfileDeleted = "Profilo [{0}] eliminato.";
        StatusProfileDeleteError = "Errore eliminazione profilo: {0}";
        StatusR2Importing = "Download e importazione profilo da codice r2modman in corso...";
        StatusR2ImportSuccess = "Profilo [{0}] importato con successo ({1} mod risolte)!";
        StatusR2ImportError = "Errore importazione codice: {0}";
        StatusR2FileImporting = "Importazione profilo r2modman da file {0}...";
        StatusVmmImportSuccess = "Profilo [{0}] importato con successo!";
        StatusVmmExportSuccess = "Profilo esportato in {0}";
        StatusVmmExportError = "Errore esportazione: {0}";
        StatusShareCodeCopied = "Codice di condivisione profilo copiato negli appunti!";
        StatusServerExportSuccess = "Pacchetto server esportato con successo in {0}";
        StatusServerExportError = "Errore durante l'esportazione del pacchetto server: {0}";
        StatusShareImportSuccess = "Profilo [{0}] importato con successo ({1} mod installate)!";
        StatusManualModInstalling = "Installazione mod manuale '{0}' in corso...";
        StatusManualModSuccess = "Mod manuale '{0}' installata con successo!";
        StatusManualModError = "Errore durante l'installazione della mod manuale: {0}";
        StatusDropNotSupported = "Formato file '{0}' non supportato. Seleziona file .zip o .dll.";
        StatusAppUpdateChecking = "Verifica aggiornamenti applicazione in corso...";
        StatusAppUpdateAvailable = "Nuova versione v{0} disponibile!";
        StatusAppUpdateLatest = "Stai già utilizzando l'ultima versione disponibile.";
        StatusAppUpdateCheckFailed = "Impossibile verificare gli aggiornamenti: {0}";
        StatusNoPackageForPlatform = "Nessun pacchetto compatibile trovato per questa piattaforma.";
        StatusAppUpdateDownloading = "Download aggiornamento v{0} in corso...";
        StatusAppUpdateDownloaded = "Aggiornamento scaricato con successo in {0}";
        StatusAppUpdateDownloadError = "Errore durante il download dell'aggiornamento: {0}";
        StatusCannotOpenFolder = "Impossibile aprire la cartella: {0}";
        StatusCannotOpenBrowser = "Impossibile aprire il browser: {0}";
    }
}
