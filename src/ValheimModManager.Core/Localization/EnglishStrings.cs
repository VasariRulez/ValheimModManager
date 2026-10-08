namespace ValheimModManager.Core.Localization;

public sealed record EnglishStrings : AppStrings
{
    public static EnglishStrings Instance { get; } = new();

    private EnglishStrings()
    {
        // General / Common
        AppTitle = "VALHEIM MOD MANAGER";
        CommonSave = "Save";
        CommonCancel = "Cancel";
        CommonClose = "Close";
        CommonUpdate = "Update";
        CommonInstall = "Install";
        CommonInstalled = "Installed";
        CommonUninstall = "Uninstall";
        CommonReady = "Ready";
        CommonLoading = "Loading...";

        // Header Toolbar
        HeaderProfileLabel = "Profile:";
        HeaderTooltipNewProfile = "Create new profile";
        HeaderTooltipCloneProfile = "Clone active profile";
        HeaderTooltipDeleteProfile = "Delete active profile";
        HeaderTooltipExportProfile = "Export profile (.vmmprofile)";
        HeaderTooltipImportProfile = "Profile import options";
        HeaderTooltipProfileOptions = "More profile options (clone, delete, export, import)";
        HeaderMenuCloneProfile = "Clone active profile";
        HeaderMenuDeleteProfile = "Delete active profile";
        HeaderMenuExportProfile = "Export Profile (.vmmprofile)";
        HeaderMenuImportVmmProfile = "Import Profile (.vmmprofile)";
        HeaderMenuImportR2z = "Import from r2modman archive (.r2z)";
        HeaderMenuImportR2Code = "Import from r2modman Code...";
        HeaderBannerUpdateAvailable = "🎉 Update Available!";
        HeaderBannerInstallUpdate = "Install v{0}";
        HeaderInstallBepInExButton = "Install BepInEx 1-Click";
        HeaderInstallBepInExShort = "Install";
        HeaderRestoreVanillaButton = "Restore Vanilla";
        HeaderRestoreVanillaShort = "Restore";
        HeaderLaunchGameButton = "▶  LAUNCH VALHEIM";
        HeaderLaunchServerButton = "🛡️  Server";
        HeaderLaunchServerTooltip = "Launch Valheim Dedicated Server with the selected profile";

        // Tab 1: Installed Mods
        TabInstalledMods = "📦  Installed Mods";
        SearchInstalledPlaceholder = "Search installed mods (auto-filtering)...";
        CheckUpdatesButton = "Check for Updates";
        UpdateAllModsButton = "Update All Mods";
        ModAuthorPrefix = "Author: {0}";
        ModUpdateAvailableBadge = "Update v{0} avail.";

        // Tab 2: Online Catalog
        TabOnlineCatalog = "🌐  Browse Online Mods";
        SearchCatalogPlaceholder = "Search by name, author or description (auto-type)...";
        RefreshCatalogButton = "🔄  Refresh Catalog";
        ModByPrefix = "by {0}";
        ModDownloadsLabel = "Downloads: {0:N0}";
        ModVotesLabel = "Rating: {0:N0}";
        ModInstallFromPrefix = "Install from {0}";
        ModSourceLabel = "Source:";
        ModVersionLabel = "Version:";
        SourceFilterAll = "All sources";
        SourceFilterThunderstore = "Thunderstore only";
        SourceFilterHexium = "Hexium only";

        // Tab 3: Settings
        TabSettings = "⚙️  Settings & Game";
        LanguageSectionTitle = "APPLICATION LANGUAGE";
        LanguageSectionDescription = "Select interface language (updates immediately):";
        GameFolderTitle = "VALHEIM FOLDER CONFIGURATION";
        GameFolderDescription = "Valheim folder path (must contain valheim.exe):";
        GameFolderPlaceholder = @"e.g. C:\Program Files (x86)\Steam\steamapps\common\Valheim";
        BrowseButton = "📂 Browse...";
        AutoDetectSteamButton = "Auto-detect from Steam libraries";
        SourcesTitle = "AVAILABLE MOD SOURCES";
        SourcesThunderstoreStatus = "(Valheim Community API v1 active)";
        SourcesHexiumStatus = "(Thunderstore-compatible OpenAPI active)";
        SourcesNexusStatus = "(Planned architecture, coming in future releases)";
        AppUpdateSectionTitle = "VALHEIM MOD MANAGER UPDATES";
        AppVersionCurrentPrefix = "Current version: v{0}";
        CheckAppUpdatesButton = "🔄 Check for Updates";
        DownloadAppUpdateButton = "Download v{0}";
        LaunchOptionsTitle = "LAUNCH OPTIONS CONFIGURATION";
        LaunchOptionsDescription = "Parameters passed to Valheim on launch:";
        LaunchOptionsPlaceholder = "e.g. -console";
        QuickPresetsTitle = "Quick presets:";
        ClearLaunchOptionsButton = "Clear options";
        TooltipPresetConsole = "Enables in-game command console (F5 key)";
        TooltipPresetExclusive = "Forces exclusive fullscreen mode";
        TooltipPresetVulkan = "Launches the game using Vulkan graphics API";

        // Modals
        NewProfileTitle = "Create New Profile";
        NewProfilePrompt = "Enter name for the new mod set:";
        NewProfilePlaceholder = "e.g. Friends Server, Vanilla+, Hardcore";
        CreateProfileButton = "Create Profile";
        ImportR2Title = "Import from r2modman / Thunderstore Code";
        ImportR2Prompt = "Paste alphanumeric export code (e.g. 018f...):";
        ImportR2Placeholder = "Paste share code...";
        ImportR2Button = "Download & Import";
        AppUpdateDialogTitle = "Update Available!";
        ReleaseNotesLabel = "Release notes:";
        DownloadingPackageLabel = "Downloading package...";
        PackageDownloadedSuccess = "✅ Package downloaded to Downloads folder!";
        OpenFolderButton = "Open folder";
        GitHubPageButton = "🌐 GitHub Page";
        DownloadPackageButton = "Download Package";

        // BepInEx Status
        BepInExConfigured = "BepInEx: Configured & Active ({0})";
        BepInExConfiguredShort = "BepInEx: Active ({0})";
        BepInExHooksPending = "BepInEx: Installed in profile, game hooks pending";
        BepInExHooksPendingShort = "BepInEx: Hooks pending";
        BepInExNotInstalled = "BepInEx: Not installed (required to load mods)";
        BepInExNotInstalledShort = "BepInEx: Not installed";
        BepInExTooltipConfigured = "BepInEx {0} is configured and active in the selected profile.";
        BepInExTooltipHooksPending = "BepInEx is present in profile, but hook files must be deployed to the game folder. Click Install to deploy them.";
        BepInExTooltipNotInstalled = "BepInEx is not installed in this profile (required to load mods). Click Install to download and configure it.";

        // Detailed Status Messages
        StatusFolderNotExists = "The specified folder does not exist.";
        StatusExeNotFound = "Warning: 'valheim.exe' not found in selected folder.";
        StatusGamePathConfigured = "Valheim folder configured successfully!";
        StatusLaunchArgsEmpty = "Launch options cleared.";
        StatusLaunchArgsSaved = "Custom launch options saved successfully!";
        StatusCatalogLoading = "Loading local catalog...";
        StatusCatalogFirstRun = "First launch: updating catalog from Thunderstore & Hexium...";
        StatusCatalogReady = "Catalog ready ({0} packages available).";
        StatusCatalogUpdating = "Updating online catalog from Thunderstore and Hexium...";
        StatusCatalogError = "Catalog update error: {0}";
        StatusDownloadingMod = "Downloading and installing {0} v{1} from {2}...";
        StatusInstallingDependency = "Installing dependency: {0} v{1}...";
        StatusModInstalledInProfile = "{0} v{1} installed from {2} in profile [{3}]!";
        StatusModInstallError = "Error installing {0}: {1}";
        StatusModEnabled = "enabled";
        StatusModDisabled = "disabled";
        StatusModToggleError = "Error modifying {0}: {1}";
        StatusModUninstalled = "{0} uninstalled.";
        StatusAllModsAlreadyUpToDate = "All mods are already up to date!";
        StatusAllModsUpdatedSuccess = "All mods were updated successfully!";
        StatusSpecifyGameFolderFirst = "Specify Valheim folder in settings first.";
        StatusDownloadingBepInEx = "Downloading official BepInExPack_Valheim package...";
        StatusBepInExInstalledSuccess = "BepInEx installed and configured successfully!";
        StatusBepInExInstallError = "Error installing BepInEx: {0}";
        StatusVanillaRestored = "Game restored to original Vanilla state (hooks removed).";
        StatusGamePathNotFound = "Valheim path not found.";
        StatusGameLaunched = "Valheim launched with profile [{0}]!";
        StatusGameLaunchError = "Game launch error: {0}";
        StatusServerNotDetected = "Valheim Dedicated Server not detected in Steam library (App ID 896660).";
        StatusServerLaunched = "Valheim Dedicated Server launched with profile [{0}]!";
        StatusServerLaunchError = "Server launch error: {0}";
        StatusProfileCreated = "New profile [{0}] created successfully.";
        StatusProfileCreateError = "Error creating profile: {0}";
        StatusProfileCloned = "Profile [{0}] cloned successfully.";
        StatusProfileCloneError = "Error cloning profile: {0}";
        StatusDefaultProfileCannotDelete = "Default profile 'Default' cannot be deleted.";
        StatusProfileDeleted = "Profile [{0}] deleted.";
        StatusProfileDeleteError = "Error deleting profile: {0}";
        StatusR2Importing = "Downloading and importing profile from r2modman code...";
        StatusR2ImportSuccess = "Profile [{0}] imported successfully ({1} mods resolved)!";
        StatusR2ImportError = "Error importing code: {0}";
        StatusR2FileImporting = "Importing r2modman profile from file {0}...";
        StatusVmmImportSuccess = "Profile [{0}] imported successfully!";
        StatusVmmExportSuccess = "Profile exported to {0}";
        StatusVmmExportError = "Export error: {0}";
        StatusAppUpdateChecking = "Checking for application updates...";
        StatusAppUpdateAvailable = "New version v{0} available!";
        StatusAppUpdateLatest = "You are already using the latest version available.";
        StatusAppUpdateCheckFailed = "Failed to check for updates: {0}";
        StatusNoPackageForPlatform = "No compatible package found for this platform.";
        StatusAppUpdateDownloading = "Downloading update v{0}...";
        StatusAppUpdateDownloaded = "Update downloaded successfully to {0}";
        StatusAppUpdateDownloadError = "Error downloading update: {0}";
        StatusCannotOpenFolder = "Cannot open folder: {0}";
        StatusCannotOpenBrowser = "Cannot open browser: {0}";
    }
}
