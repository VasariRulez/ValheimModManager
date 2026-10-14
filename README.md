# Valheim Mod Manager (VMM)

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Avalonia UI 12](https://img.shields.io/badge/Avalonia%20UI-12.1.3-8A2BE2?logo=avalonia&logoColor=white)](https://avaloniaui.net/)
[![CI/CD](https://github.com/VasariRulez/ValheimModManager/actions/workflows/release.yml/badge.svg)](https://github.com/VasariRulez/ValheimModManager/actions/workflows/release.yml)
[![Release](https://img.shields.io/github/v/release/VasariRulez/ValheimModManager?logo=github&color=blue)](https://github.com/VasariRulez/ValheimModManager/releases)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%28Steam%20Deck%29-blue)](https://github.com/VasariRulez/ValheimModManager)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

> 🌐 **Language / Lingua**: **🇬🇧 English** | [**🇮🇹 Italiano**](README.it.md)

**Valheim Mod Manager** is a modern, blazing-fast, cross-platform mod manager for **Valheim**, built with **.NET 10** and **Avalonia UI 12**.

Designed to overcome the limitations of legacy mod managers, it features native multi-source catalog integration (**Thunderstore** and **Hexium**), isolated profile architecture, instant profile sharing via **Share Codes (`vmm1-...`)**, seamless **r2modman** interoperability, one-click **Dedicated Server Pack** export, **Steam Overlay** launch injection, and full compatibility across **Windows** desktops and **Steam Deck / Linux**.

---

## ✨ Key Features

### 🌐 Unified Multi-Source Catalog, Pagination & Cached Thumbnails
- **Thunderstore & Hexium**: Browse and download directly from both official Thunderstore and [Hexium](https://valheim.hexium.gg/) repositories.
- **Smart Grouping & Version Tie-Resolution**: Automatically groups identical mods across stores (by canonical author and name) into a single card, displaying clear provenance badges (`⚡ Thunderstore vX.Y.Z` / `🔷 Hexium vX.Y.Z`)—side-by-side when stores tie on the latest version, or grouping older versions under a subtle secondary pill (`+1 older source`).
- **Classic Pagination (`100 mods/page`) & Virtualized 60 FPS UI**: Browse the entire catalog effortlessly with a bottom pagination bar (`Showing 1–100 of X mods`, `⏮`, `◀ Previous`, `Page X of Y`, `Next ▶`, `⏭`), featuring instant <1ms page transitions, debounced multi-click coalescing, and native `ListBox` UI virtualization (rendering only visible cards).
- **Compact JSON Cache & SHA-256 Delta Sync**: Slimmed local catalog storage (-73% disk footprint while preserving 100% of version history) paired with incremental SHA-256 chunk delta syncing and one-click failsafe cache rebuild controls in Settings and on the *Refresh Catalog* right-click menu.
- **Visual Lazy Loading & Downscaled 96px Icon Storage**: On-demand icon loading triggered exclusively when cards enter the visual tree, downscaling thumbnails to compact 96px PNGs on disk (-83% cache size, 0.19ms decode) and trimming steady-state RAM usage down to ~40 MB.
- **Comprehensive Card Stats**: Total downloads, community ratings, and last updated date (`📅 dd/MM/yyyy`) displayed right on each catalog card.

### 🎯 Weighted Relevance Search & Advanced Sorting
- **6 Sorting Criteria**: Sort the online catalog by **🎯 Relevance**, **⬇ Downloads**, **🕒 Last updated**, **★ Rating**, **🔤 Mod name**, or **👤 Author**, with a one-click direction toggle button (`↓` / `↑`).
- **5-Tier Weighted Relevance Search**: Typing in the search bar (300ms debounce) automatically switches sorting to *Relevance*, ranking results across 5 transparent tiers: *Exact Name Match > Name Prefix > Name Contains > Author Contains > Description Contains*.
- **Alphabetical Installed Mods (`A → Z`) & Instant Filter**: Installed mods are always kept in clean alphabetical order by name, with a responsive 200ms debounced search bar matching both mod name and author.

### 🧩 BepInEx 5 Management, Cross-Store Updates & Manual Installer
- **Automatic BepInEx 5 Setup**: Transparent detection, download, installation, and Doorstop hook verification (`winhttp.dll` / `doorstop_config.ini`) per profile.
- **Recursive Dependency Resolution**: Automatically resolves and installs the full prerequisite tree for any selected mod.
- **Cross-Store Update Detection**: Checks **all available stores** for newer versions of your installed mods (even if a mod installed from Thunderstore receives a newer release on Hexium or vice versa, highlighting the source on the badge e.g. `Update v1.3.1 avail. (🔷 Hexium)`).
- **Manual Mod Installer (Drag & Drop)**: Install local mods (e.g. downloaded from Nexus Mods) simply by dragging & dropping `.zip` or `.dll` files into the window or via the *➕ Install from File...* button, tagged with a *Manual* badge.
- **Instant Enable/Disable Toggle & Clean Uninstall**: Toggle mods on/off in place (`.dll` ↔ `.dll.disabled`) and cleanly remove mod files and empty directories upon uninstallation.

### 📂 Isolated Profiles, Share Codes (`vmm1-...`), r2modman & Dedicated Server
- **Complete Profile Isolation**: Every profile maintains its own independent `BepInEx/plugins`, `patchers`, and `config` folders, with one-click shortcuts to open profile directories in your system file manager.
- **Instant Profile Share Codes (`vmm1-...`)**: Export and import complete profiles (including your modlist and all `BepInEx/config/*.cfg` files) as a single compressed text string ready to paste on Discord or chat.
- **`.vmmprofile` Archive Export/Import**: Full file-based backup and restore for profiles.
- **r2modman Interoperability**:
  - Direct import from exported r2modman profile archives (`.r2z`).
  - Online import via r2modman profile share codes (UUID `018f...`).
- **Dedicated Server Pack Export (`.zip`)**: Generate a pre-configured `.zip` bundle containing BepInEx, plugins, and config files from the active profile, ready to extract onto a Valheim Dedicated Server.

### 🎮 Launch via Steam (Steam Overlay), Custom Launch Args & Auto-Updater
- **Launch via Steam with Overlay Enabled**: Starts Valheim through the Steam client (`-applaunch 892970`) while injecting Doorstop/BepInEx for the active profile—preserving full **Steam Overlay** functionality (`Shift+Tab`, friend invites, `F12` screenshots, Steam Input controllers). Includes a right-click context menu on *LAUNCH VALHEIM* to choose between Steam launch and direct executable launch.
- **Custom Launch Arguments**: Configure custom CLI flags with quick-append preset buttons (`-console`, `-windowed`, `-popupwindow`, `-gfx-enable-native-jobs`).
- **Automatic Steam Detection & Process Guard**: Automatically locates Valheim on Windows (Registry + `libraryfolders.vdf`) and Linux / Steam Deck (native, Flatpak, microSD) and monitors the running game process.
- **Built-in Self-Updater with Native Markdown Viewer**: Automatically checks GitHub Releases for app updates, renders release notes with **native Markdown formatting**, and updates/restarts the app in one click.
- **Bilingual UI (🇬🇧 English / 🇮🇹 Italiano)**: Real-time language switching from the top header bar.

---

## 🏛️ Solution Architecture

The solution is organized into clean, modular projects:

```text
c:\repo\ValheinModManager\
├── src\
│   ├── ValheimModManager.Core\             # Domain & Services (Catalog, Install, Profile, BepInEx, Update, Share, R2Modman)
│   ├── ValheimModManager.Platform.Windows\ # Windows Registry Steam locator & process monitor
│   ├── ValheimModManager.Platform.Linux\   # Steam Deck / Flatpak locator & Linux process monitor
│   └── ValheimModManager.App\              # Avalonia 12 Desktop App (MVVM, Fluent Dark Theme, EN/IT)
├── tests\
│   └── ValheimModManager.Tests\            # 77 Automated Tests (xUnit)
├── scripts\
│   ├── publish.ps1                         # Post-build publishing script (PowerShell)
│   └── publish.sh                          # Post-build publishing script (Bash / CI)
├── .github\workflows\
│   └── release.yml                         # GitHub Actions CI/CD release pipeline
├── CHANGELOG.md                            # Complete bilingual release history (IT/EN)
├── README.md                               # English documentation
├── README.it.md                            # Italian documentation
└── ValheimModManager.slnx                  # .NET 10 Solution file
```

---

## 🚀 Development Quickstart

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (v10.0.100 or newer).

### Build & Run
```bash
# Restore and build the solution
dotnet build ValheimModManager.slnx

# Run the desktop application
dotnet run --project src/ValheimModManager.App/ValheimModManager.App.csproj
```

### Running Automated Tests
The test suite includes **77 unit and integration tests** (xUnit) covering package installation, SemVer comparison, dependency resolution, multi-source grouping and sorting, weighted relevance search, cross-store update detection, compact dependency serialization, `vmm1` Share Codes, r2modman import, manual mod installation, Steam launch arguments, and localization:

```bash
dotnet test ValheimModManager.slnx
```

---

## 📦 Post-Build Publishing & Packaging

The repository includes scripts to produce **Self-Contained**, **Single-File**, **ReadyToRun** binaries (instant startup with no .NET runtime installation required on user machines).

### From Windows (PowerShell)
```powershell
# Full publish for both targets (win-x64 and linux-x64) after running tests:
.\scripts\publish.ps1

# Publish for Windows only:
.\scripts\publish.ps1 -Runtime win-x64

# Publish for Linux / Steam Deck skipping tests:
.\scripts\publish.ps1 -Runtime linux-x64 -SkipTests

# Clean and publish with a custom version string:
.\scripts\publish.ps1 -Version "1.1.0" -Clean
```

### From Linux / Steam Deck / CI (Bash)
```bash
chmod +x ./scripts/publish.sh

# Full publish:
./scripts/publish.sh

# Linux target only:
./scripts/publish.sh -r linux-x64
```

### Generated Output
Artifacts are written to the `dist/` directory (git-ignored):
- `ValheimModManager-<version>-win-x64.zip` (self-contained `ValheimModManager.exe`)
- `ValheimModManager-<version>-linux-x64.tar.gz` (self-contained ELF binary `ValheimModManager`)
- `SHA256SUMS.txt` (SHA-256 checksums for all generated archives)

---

## 🔄 CI/CD & Automated Releases

The GitHub Actions workflow in [`.github/workflows/release.yml`](.github/workflows/release.yml) automatically:
1. Triggers when a version tag is pushed (e.g. `git tag v1.1.0 && git push origin v1.1.0`) or via manual workflow dispatch.
2. Runs the build and all 77 tests in parallel on Windows and Ubuntu runners.
3. Extracts the matching release notes section from [`CHANGELOG.md`](CHANGELOG.md) and publishes the GitHub Release with `.zip`, `.tar.gz`, and `SHA256SUMS.txt` assets.

---

## 📄 License

Distributed under the MIT License. See [`LICENSE`](LICENSE) for details.
