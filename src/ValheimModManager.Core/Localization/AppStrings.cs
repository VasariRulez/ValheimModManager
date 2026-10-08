namespace ValheimModManager.Core.Localization;

public record AppStrings
{
    // General / Common
    public string AppTitle { get; init; } = "";
    public string CommonSave { get; init; } = "";
    public string CommonCancel { get; init; } = "";
    public string CommonClose { get; init; } = "";
    public string CommonUpdate { get; init; } = "";
    public string CommonInstall { get; init; } = "";
    public string CommonInstalled { get; init; } = "";
    public string CommonUninstall { get; init; } = "";
    public string CommonReady { get; init; } = "";
    public string CommonLoading { get; init; } = "";

    // Header Toolbar
    public string HeaderProfileLabel { get; init; } = "";
    public string HeaderTooltipNewProfile { get; init; } = "";
    public string HeaderTooltipCloneProfile { get; init; } = "";
    public string HeaderTooltipDeleteProfile { get; init; } = "";
    public string HeaderTooltipExportProfile { get; init; } = "";
    public string HeaderTooltipImportProfile { get; init; } = "";
    public string HeaderTooltipProfileOptions { get; init; } = "";
    public string HeaderMenuCloneProfile { get; init; } = "";
    public string HeaderMenuDeleteProfile { get; init; } = "";
    public string HeaderMenuExportProfile { get; init; } = "";
    public string HeaderMenuImportVmmProfile { get; init; } = "";
    public string HeaderMenuImportR2z { get; init; } = "";
    public string HeaderMenuImportR2Code { get; init; } = "";
    public string HeaderBannerUpdateAvailable { get; init; } = "";
    public string HeaderBannerInstallUpdate { get; init; } = "";
    public string HeaderInstallBepInExButton { get; init; } = "";
    public string HeaderInstallBepInExShort { get; init; } = "";
    public string HeaderRestoreVanillaButton { get; init; } = "";
    public string HeaderRestoreVanillaShort { get; init; } = "";
    public string HeaderLaunchGameButton { get; init; } = "";
    public string HeaderLaunchServerButton { get; init; } = "";
    public string HeaderLaunchServerTooltip { get; init; } = "";

    // Tab 1: Installed Mods
    public string TabInstalledMods { get; init; } = "";
    public string SearchInstalledPlaceholder { get; init; } = "";
    public string CheckUpdatesButton { get; init; } = "";
    public string UpdateAllModsButton { get; init; } = "";
    public string ModAuthorPrefix { get; init; } = "";
    public string ModUpdateAvailableBadge { get; init; } = "";

    // Tab 2: Online Catalog
    public string TabOnlineCatalog { get; init; } = "";
    public string SearchCatalogPlaceholder { get; init; } = "";
    public string RefreshCatalogButton { get; init; } = "";
    public string ModByPrefix { get; init; } = "";
    public string ModDownloadsLabel { get; init; } = "";
    public string ModVotesLabel { get; init; } = "";
    public string ModInstallFromPrefix { get; init; } = "";
    public string ModSourceLabel { get; init; } = "";
    public string ModVersionLabel { get; init; } = "";
    public string SourceFilterAll { get; init; } = "";
    public string SourceFilterThunderstore { get; init; } = "";
    public string SourceFilterHexium { get; init; } = "";

    // Tab 3: Settings
    public string TabSettings { get; init; } = "";
    public string LanguageSectionTitle { get; init; } = "";
    public string LanguageSectionDescription { get; init; } = "";
    public string GameFolderTitle { get; init; } = "";
    public string GameFolderDescription { get; init; } = "";
    public string GameFolderPlaceholder { get; init; } = "";
    public string BrowseButton { get; init; } = "";
    public string AutoDetectSteamButton { get; init; } = "";
    public string SourcesTitle { get; init; } = "";
    public string SourcesThunderstoreStatus { get; init; } = "";
    public string SourcesHexiumStatus { get; init; } = "";
    public string SourcesNexusStatus { get; init; } = "";
    public string AppUpdateSectionTitle { get; init; } = "";
    public string AppVersionCurrentPrefix { get; init; } = "";
    public string CheckAppUpdatesButton { get; init; } = "";
    public string DownloadAppUpdateButton { get; init; } = "";
    public string LaunchOptionsTitle { get; init; } = "";
    public string LaunchOptionsDescription { get; init; } = "";
    public string LaunchOptionsPlaceholder { get; init; } = "";
    public string QuickPresetsTitle { get; init; } = "";
    public string ClearLaunchOptionsButton { get; init; } = "";
    public string TooltipPresetConsole { get; init; } = "";
    public string TooltipPresetExclusive { get; init; } = "";
    public string TooltipPresetVulkan { get; init; } = "";

    // Modals
    public string NewProfileTitle { get; init; } = "";
    public string NewProfilePrompt { get; init; } = "";
    public string NewProfilePlaceholder { get; init; } = "";
    public string CreateProfileButton { get; init; } = "";
    public string ImportR2Title { get; init; } = "";
    public string ImportR2Prompt { get; init; } = "";
    public string ImportR2Placeholder { get; init; } = "";
    public string ImportR2Button { get; init; } = "";
    public string AppUpdateDialogTitle { get; init; } = "";
    public string ReleaseNotesLabel { get; init; } = "";
    public string DownloadingPackageLabel { get; init; } = "";
    public string PackageDownloadedSuccess { get; init; } = "";
    public string OpenFolderButton { get; init; } = "";
    public string GitHubPageButton { get; init; } = "";
    public string DownloadPackageButton { get; init; } = "";

    // BepInEx Status
    public string BepInExConfigured { get; init; } = "";
    public string BepInExConfiguredShort { get; init; } = "";
    public string BepInExHooksPending { get; init; } = "";
    public string BepInExHooksPendingShort { get; init; } = "";
    public string BepInExNotInstalled { get; init; } = "";
    public string BepInExNotInstalledShort { get; init; } = "";
    public string BepInExTooltipConfigured { get; init; } = "";
    public string BepInExTooltipHooksPending { get; init; } = "";
    public string BepInExTooltipNotInstalled { get; init; } = "";

    // Detailed Status Messages
    public string StatusFolderNotExists { get; init; } = "";
    public string StatusExeNotFound { get; init; } = "";
    public string StatusGamePathConfigured { get; init; } = "";
    public string StatusLaunchArgsEmpty { get; init; } = "";
    public string StatusLaunchArgsSaved { get; init; } = "";
    public string StatusCatalogLoading { get; init; } = "";
    public string StatusCatalogFirstRun { get; init; } = "";
    public string StatusCatalogReady { get; init; } = "";
    public string StatusCatalogUpdating { get; init; } = "";
    public string StatusCatalogError { get; init; } = "";
    public string StatusDownloadingMod { get; init; } = "";
    public string StatusInstallingDependency { get; init; } = "";
    public string StatusModInstalledInProfile { get; init; } = "";
    public string StatusModInstallError { get; init; } = "";
    public string StatusModEnabled { get; init; } = "";
    public string StatusModDisabled { get; init; } = "";
    public string StatusModToggleError { get; init; } = "";
    public string StatusModUninstalled { get; init; } = "";
    public string StatusAllModsAlreadyUpToDate { get; init; } = "";
    public string StatusAllModsUpdatedSuccess { get; init; } = "";
    public string StatusSpecifyGameFolderFirst { get; init; } = "";
    public string StatusDownloadingBepInEx { get; init; } = "";
    public string StatusBepInExInstalledSuccess { get; init; } = "";
    public string StatusBepInExInstallError { get; init; } = "";
    public string StatusVanillaRestored { get; init; } = "";
    public string StatusGamePathNotFound { get; init; } = "";
    public string StatusGameLaunched { get; init; } = "";
    public string StatusGameLaunchError { get; init; } = "";
    public string StatusServerNotDetected { get; init; } = "";
    public string StatusServerLaunched { get; init; } = "";
    public string StatusServerLaunchError { get; init; } = "";
    public string StatusProfileCreated { get; init; } = "";
    public string StatusProfileCreateError { get; init; } = "";
    public string StatusProfileCloned { get; init; } = "";
    public string StatusProfileCloneError { get; init; } = "";
    public string StatusDefaultProfileCannotDelete { get; init; } = "";
    public string StatusProfileDeleted { get; init; } = "";
    public string StatusProfileDeleteError { get; init; } = "";
    public string StatusR2Importing { get; init; } = "";
    public string StatusR2ImportSuccess { get; init; } = "";
    public string StatusR2ImportError { get; init; } = "";
    public string StatusR2FileImporting { get; init; } = "";
    public string StatusVmmImportSuccess { get; init; } = "";
    public string StatusVmmExportSuccess { get; init; } = "";
    public string StatusVmmExportError { get; init; } = "";
    public string StatusAppUpdateChecking { get; init; } = "";
    public string StatusAppUpdateAvailable { get; init; } = "";
    public string StatusAppUpdateLatest { get; init; } = "";
    public string StatusAppUpdateCheckFailed { get; init; } = "";
    public string StatusNoPackageForPlatform { get; init; } = "";
    public string StatusAppUpdateDownloading { get; init; } = "";
    public string StatusAppUpdateDownloaded { get; init; } = "";
    public string StatusAppUpdateDownloadError { get; init; } = "";
    public string StatusCannotOpenFolder { get; init; } = "";
    public string StatusCannotOpenBrowser { get; init; } = "";
}
