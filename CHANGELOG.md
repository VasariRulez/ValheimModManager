# Changelog

Tutte le modifiche rilevanti a questo progetto sono documentate in questo file.
Il formato è basato su [Keep a Changelog](https://keepachangelog.com/).

---

## [1.0.8] - 2026-10-08

### 🇮🇹 Italiano

#### Aggiunto
- **Avvio tramite Steam & Iniezione Steam Overlay**: Il pulsante "AVVIA VALHEIM" ora avvia il gioco tramite il client Steam (`-applaunch 892970`), abilitando completamente l'overlay di gioco (Shift+Tab, inviti amici, screenshot F12 e controller Steam Input), pur mantenendo l'iniezione automatica di Doorstop e BepInEx per il profilo selezionato.
- **Menu contestuale di avvio rapido**: Facendo clic destro sul pulsante "AVVIA VALHEIM" è ora possibile scegliere tra *"▶  Avvia con Steam (Overlay abilitato)"* e *"⚡  Avvia direttamente (senza Steam)"*.
- **Opzione nelle Impostazioni**: Aggiunta la casella di spunta *"Avvia tramite Steam (Overlay Steam abilitato)"* nelle impostazioni (sezione Opzioni di avvio, attiva per impostazione predefinita) per personalizzare il comportamento di avvio predefinito.
- **Visualizzazione Markdown nativa**: Le note di rilascio e il changelog nella finestra di aggiornamento vengono ora renderizzati con formattazione Markdown completa (titoli gerarchici, elenchi puntati con bullet point dedicati, grassetti e font monospazio per il codice inline).
- **Automazione Release GitHub**: Estrattore automatico della sezione di versione da `CHANGELOG.md` integrato nella pipeline CI/CD GitHub Actions.

#### Modificato
- **Fallback automatico affidabile**: Se il client Steam non è installato o non viene rilevato (oppure per il Dedicated Server), il gioco viene avviato direttamente tramite l'eseguibile senza bloccare l'utente.

---

### 🇬🇧 English

#### Added
- **Launch via Steam & Steam Overlay Injection**: The "LAUNCH VALHEIM" button now launches the game via the Steam client (`-applaunch 892970`), enabling in-game overlay features (Shift+Tab, friend invites, F12 screenshots, and Steam Input controller support) while seamlessly loading Doorstop and BepInEx for the active profile.
- **Quick Launch Context Menu**: Right-clicking the "LAUNCH VALHEIM" button now allows choosing between *"▶  Launch with Steam (Overlay enabled)"* and *"⚡  Launch directly (without Steam)"*.
- **Settings Toggle**: Added a *"Launch via Steam (Steam Overlay enabled)"* toggle under Settings (Launch Options section, enabled by default) to customize the launch behavior.
- **Native Markdown Release Notes**: Update dialog notes and changelogs are now formatted using a lightweight native Markdown viewer (hierarchical headers, bullet lists, bold highlights, and monospaced inline code).
- **Automated GitHub Release Notes**: Automated extractor step in GitHub Actions CI/CD to populate release bodies directly from `CHANGELOG.md`.

#### Changed
- **Resilient Fallback**: If the Steam client is not installed or cannot be located (or when launching Dedicated Server), the launcher falls back to starting the game executable directly.

---

## [1.0.7.1] - 2026-10-08

### 🇮🇹 Italiano

#### Aggiunto
- **Icone del Menu Opzioni Profilo**: Aggiunte icone visuali coerenti e descrittive (`📋`, `🗑️`, `🔗`, `📤`, `🛡️`, `🔑`, `📥`, `📦`, `⚡`, `📁`) a ciascuna voce del menu a tendina delle opzioni del profilo con spaziatura e allineamento uniforme.

---

### 🇬🇧 English

#### Added
- **Profile Options Menu Icons**: Decorated all profile dropdown menu items with cohesive, descriptive icons (`📋`, `🗑️`, `🔗`, `📤`, `🛡️`, `🔑`, `📥`, `📦`, `⚡`, `📁`) and consistent alignment.

---

## [1.0.7] - 2026-10-08

### 🇮🇹 Italiano

#### Aggiunto
- **Condivisione Profilo con Share Code**: Generazione di codici compressi Base64/Deflate (`vmm1-...`) contenenti la modlist completa e i file di configurazione `BepInEx/config/*.cfg`.
- **Importazione da Share Code VMM**: Finestra di dialogo dedicata per importare profili tramite codice `vmm1-...`, con risoluzione e download automatico dal catalogo online.
- **Esportazione Pacchetto Server Dedicato**: Creazione rapida di archivi `.zip` pre-configurati per installare il profilo selezionato su un server dedicato Valheim.
- **Installatore Manuale di Mod (Drag & Drop)**: Supporto all'installazione di mod locali (da NexusMods o altrove) trascinando file `.zip` o `.dll` nella finestra o tramite il pulsante *"➕ Installa da File..."*, con badge visivo viola *"Manuale"*.
- **Accesso Rapido alle Cartelle dei Profili**: Aggiunti pulsanti dedicati *"Apri Cartella Profili"* e *"Apri Profilo Attivo"* nella scheda Impostazioni e nel menu rapido profilo per aprire direttamente le cartelle in Esplora Risorse (Windows) o file manager (Linux).

---

### 🇬🇧 English

#### Added
- **Profile Sharing via Share Codes**: Export profiles as compact Base64/Deflate strings (`vmm1-...`) embedding mod manifests and all `BepInEx/config/*.cfg` files.
- **Import from VMM Share Code**: Dedicated modal dialog to import `vmm1-...` codes with automated catalog resolution and mod downloads.
- **Dedicated Server Package Export**: One-click `.zip` export configured for Valheim Dedicated Server deployments.
- **Manual Mod Installer (Drag & Drop)**: Install local mods (`.zip` and `.dll` from NexusMods or local files) by dragging them into the application or using the *"➕ Install from File..."* button, tagged with a purple *"Manual"* badge.
- **Quick Profile Folders Access**: Added *"Open Profiles Folder"* and *"Open Active Profile"* buttons in Settings and the profile dropdown menu for instant navigation in Windows Explorer or Linux file managers.

---

## [1.0.6] - 2026-10-08

### 🇮🇹 Italiano

#### Aggiunto
- **Localizzazione Multilingua Completa**: Supporto dinamico per **Italiano** e **Inglese** con cambio istantaneo a runtime dalla scheda Impostazioni.
- **Toolbar Header Responsive**: Riprogettazione dell'intestazione a 3 zone (profilo a sinistra, pill BepInEx al centro, azioni di avvio ancorate a destra) per evitare sovrapposizioni e tagli di testo al ridimensionamento della finestra.
- **Footer Banner per Aggiornamenti**: Notifica di nuova versione disponibile spostata in un banner discreto a fondo pagina con pulsante di dismiss ("Ignora per ora").

---

### 🇬🇧 English

#### Added
- **Full Bilingual Localization**: Complete **Italian** and **English** interface with instant runtime switching from the Settings tab.
- **Responsive Header Toolbar**: Redesigned 3-zone layout (profile controls on the left, centered BepInEx pill, launch actions anchored to the right) preventing clipping on window resizing.
- **Dismissible Footer Update Banner**: Moved new version notifications to a sleek bottom footer banner with a dismiss option.

---

## [1.0.5.4] - 2026-10-07

### 🇮🇹 Italiano

#### Aggiunto
- **Sincronizzazione Automatica Versione**: La versione dell'applicazione viene ora letta dinamicamente dal tag Git (`AssemblyInformationalVersionAttribute` / `AssemblyFileVersionAttribute`) sia a runtime che nell'interfaccia.

---

### 🇬🇧 English

#### Added
- **Automated Version Synchronization**: Application version is dynamically resolved from Git tags and assembly attributes at runtime.

---

## [1.0.5.3] - 2026-10-07

### 🇮🇹 Italiano

#### Corretto
- **Risoluzione errore NETSDK1094 in CI**: Disabilitato `PublishReadyToRun` negli script di pubblicazione PowerShell e Bash per garantire compilazioni multipiattaforma stabili su GitHub Actions.

---

### 🇬🇧 English

#### Fixed
- **CI NETSDK1094 Build Fix**: Disabled `PublishReadyToRun` in publish scripts to ensure reliable cross-platform builds on GitHub Actions.

---

## [1.0.5.2] - 2026-10-07

### 🇮🇹 Italiano

#### Corretto
- **Ripristino NuGet in Ambiente Windows CI**: Aggiunto ripristino per-RID (`win-x64`) e flag `--no-restore` in `publish.ps1` per evitare errori di compilazione nel runner Windows.

---

### 🇬🇧 English

#### Fixed
- **Windows CI NuGet Restore Fix**: Added per-RID restore (`win-x64`) and `--no-restore` flag in `publish.ps1` for consistent Windows runner execution.

---

## [1.0.5.1] - 2026-10-07

### 🇮🇹 Italiano

#### Corretto
- **Configurazione NuGet Esplicita**: Aggiunto file `nuget.config` per garantire la risoluzione sicura dei feed NuGet standard durante i workflow di GitHub Actions.

---

### 🇬🇧 English

#### Fixed
- **Explicit NuGet Configuration**: Added root `nuget.config` to guarantee reliable NuGet feed resolution across GitHub Actions runners.

---

## [1.0.5] - 2026-10-07

### 🇮🇹 Italiano

#### Aggiunto
- **Controllo Aggiornamenti In-App**: Integrazione con le API di GitHub Releases per verificare automaticamente la disponibilità di nuove versioni.
- **Download Diretto Pacchetti**: Finestra modale con visualizzazione delle note di rilascio e download con barra di avanzamento del pacchetto `.zip` / `.tar.gz` corrispondente al sistema operativo in uso.
- **Normalizzazione Versione BepInEx**: Rimozione automatica dei metadati di build (`+commit`) e priorità al `FileVersion` per una visualizzazione pulita della versione installata.

---

### 🇬🇧 English

#### Added
- **In-App Update Checker**: Integrated GitHub Releases API checking to notify users when a new version is released.
- **Direct Package Downloader**: Dialog displaying release notes with a real-time download progress bar for the appropriate platform asset (`.zip` / `.tar.gz`).
- **BepInEx Version Normalization**: Stripped build metadata (`+commit`) and prioritized `FileVersion` for clean version display.

---

## [1.0.4] - 2026-10-06

### 🇮🇹 Italiano

#### Aggiunto
- **Configurazione Opzioni di Avvio**: Possibilità di specificare parametri di avvio personalizzati per Valheim salvati automaticamente nello stato locale.
- **Preset Rapidi di Avvio**: Pulsanti a singolo clic per aggiungere parametri comuni come `-console`, `-window-mode exclusive` e `-force-vulkan`.
- **Layout Impostazioni a Due Colonne**: Riorganizzazione grafica della scheda Impostazioni con colonna dedicata per le opzioni di gioco.

---

### 🇬🇧 English

#### Added
- **Custom Launch Options Configuration**: Ability to specify custom launch arguments passed to Valheim, persisted in local application state.
- **Quick Launch Presets**: One-click preset buttons for common arguments (`-console`, `-window-mode exclusive`, `-force-vulkan`).
- **Two-Column Settings Layout**: Redesigned Settings tab layout with a dedicated column for launch parameters and presets.

---

## [1.0.3] - 2026-10-06

### 🇮🇹 Italiano

#### Aggiunto
- **Icona e Branding Ufficiale**: Aggiunta l'icona applicativa vettoriale (`Assets/icon.ico`) e branding integrato nella barra del titolo e nei manifest di sistema.
- **Ridenominazione Eseguibile**: Eseguibile compilato rinominato ufficialmente in `ValheimModManager` (`ValheimModManager.exe` su Windows).

---

### 🇬🇧 English

#### Added
- **Official Icon & Branding**: Added custom application icon (`Assets/icon.ico`) and window title branding.
- **Executable Renaming**: Standardized output executable name to `ValheimModManager` (`ValheimModManager.exe` on Windows).

---

## [1.0.2] - 2026-10-06

### 🇮🇹 Italiano

#### Corretto
- **Script di Packaging PowerShell**: Risolto errore sulla proprietà `.Count` per file scalari singoli durante la compressione degli archivi di rilascio.

---

### 🇬🇧 English

#### Fixed
- **PowerShell Packaging Script Fix**: Resolved `.Count` property resolution error on single scalar file objects during release archive compression.

---

## [1.0.1] - 2026-10-06

### 🇮🇹 Italiano

#### Corretto
- **Gestione Errori Script Windows**: Migliorata la gestione degli errori e la verbosità di log nello script `publish.ps1`.

---

### 🇬🇧 English

#### Fixed
- **Windows Publish Error Handling**: Improved error checking and logging in `publish.ps1`.

---

## [1.0.0] - 2026-10-06

### 🇮🇹 Italiano

#### Aggiunto
- **Rilascio Iniziale di Valheim Mod Manager**:
  - Supporto per sorgenti mod multiple: **Thunderstore Community** e **valheim.hexium.gg**.
  - Navigazione catalogo online con ricerca in tempo reale (debounced) e filtri per sorgente.
  - Risoluzione automatica delle dipendenze e download asincrono pacchetti.
  - Gestione profili multipli isolati: creazione, clonazione ed eliminazione profili mod.
  - Installazione e configurazione con 1 clic di **BepInExPack_Valheim** con iniezione Doorstop (`winhttp.dll`).
  - Importazione ed esportazione profili in formato nativo `.vmmprofile`.
  - Compatibilità con **r2modman**: importazione da archivi `.r2z` e codici di esportazione alfanumerici.
  - Rilevamento automatico installazioni Steam su **Windows** e **Linux / Steam Deck** (Steam Flatpak e Proton).
  - Supporto per avvio client Valheim e Valheim Dedicated Server.
  - Interfaccia grafica desktop moderna in **Avalonia 12** con tema Fluent Dark e architettura MVVM.
  - Script automatizzati di build e packaging per Windows e Linux (`scripts/publish.ps1`, `scripts/publish.sh`).

---

### 🇬🇧 English

#### Added
- **Initial Public Release of Valheim Mod Manager**:
  - Multi-source mod catalog support: **Thunderstore Community** and **valheim.hexium.gg**.
  - Online catalog browser with debounced search and source filtering.
  - Automated dependency resolution and asynchronous package downloading.
  - Isolated multi-profile management: create, clone, and delete mod profiles.
  - 1-click **BepInExPack_Valheim** deployment and Doorstop injection (`winhttp.dll`).
  - Native `.vmmprofile` profile import and export.
  - **r2modman** compatibility: import from `.r2z` archives and alphanumeric share codes.
  - Automatic Steam install detection on **Windows** and **Linux / Steam Deck** (Flatpak & Proton).
  - Dual launch support for Valheim Client and Valheim Dedicated Server.
  - Modern desktop user interface powered by **Avalonia 12** with Fluent Dark theme and MVVM pattern.
  - Automated cross-platform publish scripts (`scripts/publish.ps1`, `scripts/publish.sh`).
