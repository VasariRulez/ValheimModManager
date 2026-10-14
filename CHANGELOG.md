# Changelog

Tutte le modifiche rilevanti a questo progetto sono documentate in questo file.
Il formato è basato su [Keep a Changelog](https://keepachangelog.com/).

## [1.1.0] - 2026-10-14

### 🇮🇹 Italiano

#### Prestazioni e Architettura
- **Virtualizzazione UI del Catalogo Mod (`ListBox`)**:
  - Sostituito l'`ItemsControl` a layout infinito con una `ListBox` virtualizzata nativa: anziché istanziare tutti e 100 i card contemporaneamente (~5.000 controlli UI sul thread principale), Avalonia mantiene in memoria visiva **esclusivamente gli 8–12 card visibili a schermo**, azzerando i tempi di rendering e rendendo lo scorrimento fluidissimo a 60 fps.
- **Lazy Loading Visuale delle Miniature (Eliminazione ThreadPool Starvation)**:
  - Il caricamento delle icone è ora guidato direttamente dall'ingresso del card nell'albero visivo (`AttachedToVisualTree` / `DataContextChanged`).
  - Eliminato il loop anticipato che accodava 100 download contemporanei nel ThreadPool: vengono elaborate solo le miniature visibili a video, azzerando la contesa CPU/rete e lasciando l'interfaccia costantemente reattiva ai click.
- **Paginazione Reattiva con Coalescing dei Click Rapidi**:
  - Il cambio pagina (Prima, Precedente, Successiva, Ultima) e i relativi indicatori numerici rispondono **istantaneamente (< 1 ms)** al click.
  - Introdotto un meccanismo di debounce e cancellazione asincrona a 80 ms: cliccando più volte consecutivamente su "Successiva", i caricamenti intermedi vengono abortiti senza congelare l'applicazione o bloccare il cambio di scheda.
- **Salvataggio Icone Compresse a 96px su Disco (-83% Spazio Cache)**:
  - Le miniature scaricate dai provider (spesso caricate dagli autori a 1024×1024 o 2048×2048 px) vengono automaticamente ridimensionate e compresse a 96 px prima della scrittura nella cache su disco (`%LOCALAPPDATA%\ValheimModManager\cache\icons\`).
  - La dimensione media su disco scende da ~80 KB a **13,3 KB** per icona (-83% di spazio su disco).
  - La decodifica futura da disco impiega appena **0,19 ms** per immagine.
- **Abbattimento dell'Impronta di Memoria RAM (-96% a Regime, ~40 MB Idle)**:
  - Working Set fisico in esecuzione ridotto da oltre 1.050 MB a soli **~40 MB a regime** (con cali a ~20 MB durante il cambio pagina e picco controllato a 130–140 MB durante lo scroll completo della pagina).
  - Cache LRU in memoria RAM ridotta da 1.000 a **120 icone**, prevenendo l'accumulo di texture Skia/GPU durante la navigazione di decine di pagine.
  - Introdotto il rilascio automatico delle pagine di memoria fisica su Windows (`SetProcessWorkingSetSize`) e compattazione heap GC post-avvio e al cambio pagina.
  - Ottimizzazione fast-path monosorgente in `GroupMods` per oltre il 95% del catalogo (riutilizzo di `Array.Empty<string>()` e array compatti).

#### Corretto
- **Risoluzione `System.ObjectDisposedException` durante la Navigazione**:
  - Rimossa la distruzione prematura (`Dispose`) sui `CancellationTokenSource` con task in corso e sulle `Bitmap` di Avalonia ancora referenziate dalla pipeline di rendering.
- **Eliminazione Freeze UI durante il Download delle Immagini**:
  - Spostato tutto l'I/O di rete e di disco su thread worker in background con isolamento completo dal SynchronizationContext UI (`ConfigureAwait(false)`).

---

### 🇬🇧 English

#### Performance & Architecture
- **Virtualized Catalog UI (`ListBox`)**:
  - Replaced infinite-height `ScrollViewer` and `ItemsControl` with a fully virtualized native `ListBox`: instead of materializing all 100 cards simultaneously (~5,000 UI controls on the main thread), Avalonia only instantiates the **8–12 cards currently visible on screen**, achieving buttery-smooth 60 fps scrolling.
- **Native Visual Lazy Loading for Mod Thumbnails**:
  - Icon loading is now driven by visual tree attachment events (`AttachedToVisualTree` / `DataContextChanged`).
  - Removed eager 100-item download loops that previously starved the .NET ThreadPool: only thumbnails for cards currently in view are requested, eliminating UI freezes and CPU contention.
- **Responsive Pagination with Multi-Click Coalescing**:
  - Page navigation controls (First, Previous, Next, Last) and page indicators update **instantly (< 1 ms)** upon click.
  - Added an 80 ms debounced cancellation mechanism: clicking "Next" rapidly aborts intermediate page renders, preventing input queue lockups and allowing seamless tab switching.
- **Downscaled 96px Icon Storage on Disk (-83% Disk Footprint)**:
  - High-resolution thumbnails from providers (often uploaded at 1024×1024 or 2048×2048 px) are automatically resized to 96 px and saved as compact PNGs in `%LOCALAPPDATA%\ValheimModManager\cache\icons\`.
  - Average icon file size dropped from ~80 KB to **13.3 KB** (-83% disk savings).
  - Subsequent disk decodes take only **0.19 ms** per icon.
- **Drastic RAM Footprint Reduction (-96% at Idle, ~40 MB Steady-State)**:
  - Real-world physical Working Set dropped from >1,050 MB to **~40 MB at idle** (temporarily dipping to ~20 MB on page changes, with peak capped at 130–140 MB under full 100-card scroll).
  - In-memory thumbnail LRU cache capped at **120 bitmaps** (down from 1,000) to eliminate Skia/GPU texture accumulation across long browsing sessions.
  - Added background Windows Working Set trimming (`SetProcessWorkingSetSize`) and debounced GC compaction on startup and page transitions.
  - Fast-path single-source mod grouping in `GroupMods` for >95% of catalog items, sharing empty category arrays and avoiding temporary collections.

#### Fixed
- **Prevented `System.ObjectDisposedException` during Navigation**:
  - Eliminated premature disposal of active `CancellationTokenSource` instances and Avalonia `Bitmap` objects still referenced by the UI rendering pipeline.
- **Eliminated UI Freezes during Icon Downloads**:
  - Fully offloaded disk I/O and network requests to background worker threads with explicit `ConfigureAwait(false)`.

---

## [1.0.11] - 2026-10-12

### 🇮🇹 Italiano

#### Aggiunto
- **Paginazione Classica nel Catalogo Online**:
  - Barra di paginazione fissa nella parte inferiore della scheda *Esplora Mod Online* con contatore dei risultati filtrati (`Mostrate 1–100 di X mod`) e controlli rapidi per **Prima pagina (`⏮`)**, **Precedente (`◀`)**, indicatore **`Pagina X di Y`**, **Successiva (`▶`)** e **Ultima pagina (`⏭`)**.
  - Navigazione tra le pagine istantanea (**0 ms**) sulla lista già ordinata in memoria e ritorno automatico in cima allo scorrimento (`ScrollToHome`) ad ogni cambio pagina.
  - Reset intelligente a *Pagina 1* quando si modificano ricerca testuale, filtro sorgente o criterio/direzione di ordinamento, mantenendo invece la pagina attiva al cambio lingua o allo svuotamento della cache icone.
- **Delta Sync Incrementale sui Chunk SHA-256**:
  - Salvataggio degli hash SHA-256 dei chunk del catalogo in `thunderstore.chunks.json` per scaricare durante gli aggiornamenti solo i chunk effettivamente modificati (riducendo drasticamente traffico di rete e tempi di aggiornamento).
  - Esclusione automatica del manifest chunk per gli store privi di hash immutabile nell'URL (come Hexium), garantendone sempre l'aggiornamento puntuale.
- **Strumenti di Manutenzione e Ripristino Cache (Catalogo + Icone)**:
  - Aggiunto il comando **🧹 Svuota Cache e Riscarica Catalogo** per eliminare la cache JSON locale e forzare il download integrale da zero di tutti gli store.
  - Aggiunto il comando **🖼️ Svuota Cache Icone** per rimuovere tutte le miniature salvate in RAM e su disco (`%LOCALAPPDATA%\ValheimModManager\cache\icons\`) e ricaricare al volo quelle visibili.
  - Entrambi i comandi sono accessibili sia dalla scheda *⚙ Impostazioni* sia tramite **clic destro** sul pulsante *🔄 Aggiorna Catalogo*.

#### Migliorato
- **Snellimento Cache JSON del Catalogo (-73% su disco, 100% versioni preservate)**:
  - Eliminata la duplicazione ridondante di `Description` e `IconUrl` all'interno delle singole `ModVersion` e introdotto il serializzatore compatto `DependencyJsonConverter` (`"Autore-Mod-1.0.0"`), riducendo `thunderstore.json` da ~443 MB a ~118 MB e `hexium.json` da ~21,7 MB a ~7,4 MB senza perdere alcuna versione storica e con auto-compattazione trasparente delle cache esistenti.

---

### 🇬🇧 English

#### Added
- **Classic Pagination in Online Catalog**:
  - Fixed bottom pagination bar in the *Browse Online Mods* tab featuring a filtered results counter (`Showing 1–100 of X mods`) and navigation controls for **First page (`⏮`)**, **Previous (`◀`)**, **`Page X of Y`** indicator, **Next (`▶`)**, and **Last page (`⏭`)**.
  - Instant **0 ms** page switching over the pre-sorted in-memory list with automatic scroll-to-top (`ScrollToHome`) on page change.
  - Smart reset to *Page 1* when search text, source filter, or sort criterion/direction changes, while preserving the current page when switching language or clearing the icon cache.
- **Incremental SHA-256 Chunk Delta Sync**:
  - Tracks SHA-256 chunk hashes in `thunderstore.chunks.json` so manual catalog refreshes only download chunks that actually changed, dramatically cutting network usage and refresh times.
  - Automatically skips chunk manifest caching for providers without immutable SHA-256 URLs (such as Hexium) so they always refresh reliably.
- **Cache Maintenance & Failsafe Controls (Catalog + Icons)**:
  - Added **🧹 Clear Cache & Rebuild Catalog** to wipe local JSON caches and re-download all online catalogs from scratch.
  - Added **🖼️ Clear Icon Cache** to purge all mod thumbnails from RAM and disk (`%LOCALAPPDATA%\ValheimModManager\cache\icons\`) and immediately reload visible icons.
  - Both actions are available in the *⚙ Settings* tab and via the **right-click context menu** on the *🔄 Refresh Catalog* button.

#### Improved
- **Slimmed Catalog JSON Cache (-73% disk footprint, 100% version history preserved)**:
  - Omitted duplicate per-version `Description` and `IconUrl` fields on `ModVersion` entries and introduced the compact `DependencyJsonConverter` (`"Author-Mod-1.0.0"`), shrinking `thunderstore.json` from ~443 MB to ~118 MB and `hexium.json` from ~21.7 MB to ~7.4 MB while keeping 100% of historical versions and automatically compacting legacy caches in the background.

---

## [1.0.10] - 2026-10-12

### 🇮🇹 Italiano

#### Aggiunto
- **Ordinamento Avanzato del Catalogo e Direzione (`↓` / `↑`)**:
  - 6 criteri di ordinamento selezionabili nel catalogo online: **🎯 Rilevanza**, **⬇ Download**, **🕒 Ultimo aggiornamento**, **★ Valutazione**, **🔤 Nome mod** e **👤 Autore**.
  - Pulsante dedicato per invertire in qualsiasi momento la direzione di ordinamento (Crescente `↑` / Decrescente `↓`) con impostazione automatica intelligente in base al criterio scelto.
- **Ricerca Testuale Ponderata per Rilevanza**:
  - Passaggio automatico a *🎯 Rilevanza* appena si inizia a digitare nella barra di ricerca (e ritorno automatico a *⬇ Download* quando la ricerca viene svuotata).
  - Sistema di punteggio a 5 fasce trasparenti (Match esatto sul Nome > Prefisso sul Nome > Nome contiene > Autore contiene > Descrizione contiene) per mostrare sempre in cima le mod più pertinenti.
- **Etichette Descrittive e Data di Aggiornamento sulle Schede**:
  - Etichette bilingui sopra i controlli della barra di ricerca e filtri (*Cerca mod:*, *Sorgente:*, *Ordina per:*).
  - Visualizzazione della data di ultimo aggiornamento (`📅 gg/mm/aaaa`) su ogni scheda del catalogo accanto a download e valutazioni.
- **Rilevamento Aggiornamenti Cross-Store**:
  - Il controllo aggiornamenti delle mod installate confronta ora le versioni disponibili su **tutti gli store** (Thunderstore, Hexium, ecc.), mantenendo la priorità sullo store originale a parità di versione.
  - Se un aggiornamento più recente proviene da uno store diverso rispetto a quello di installazione, il badge nella schermata *Mod Installate* ne indica chiaramente la fonte (es. `Aggiornamento v1.3.1 disp. (🔷 Hexium)`).

#### Corretto
- **Ordinamento Alfabetico Mod Installate (`A → Z`)**: L'elenco nella scheda *Mod Installate* è ora sempre ordinato alfabeticamente per nome della mod (anziché per ordine cronologico di installazione), mantenendo la posizione stabile anche dopo l'aggiornamento di una mod.
- **Ricaricamento Automatico Badge Aggiornamenti all'Avvio**: Risolto un problema per cui, al termine del caricamento iniziale del catalogo da cache o dopo un aggiornamento online, la schermata *Mod Installate* non mostrava subito i badge di aggiornamento finché non veniva effettuata una ricerca.
- **Ricerca Mod Installate per Autore**: La barra di ricerca nella scheda *Mod Installate* filtra ora anche per nome dell'autore oltre che per nome della mod.

---

### 🇬🇧 English

#### Added
- **Advanced Catalog Sorting & Direction Toggle (`↓` / `↑`)**:
  - 6 sorting criteria in the online catalog: **🎯 Relevance**, **⬇ Downloads**, **🕒 Last updated**, **★ Rating**, **🔤 Mod name**, and **👤 Author**.
  - Dedicated direction toggle button (Ascending `↑` / Descending `↓`) with smart default direction based on the selected criterion.
- **Weighted Relevance Text Search**:
  - Automatic switch to *🎯 Relevance* as soon as the user starts typing in the search box (and automatic return to *⬇ Downloads* when cleared).
  - 5-tier relevance scoring system (Exact Name Match > Name Prefix > Name Contains > Author Contains > Description Contains) ensuring the most relevant mods always appear at the top.
- **Filter Bar Labels & Card Last-Updated Date**:
  - Descriptive bilingual labels above search and filter controls (*Search mods:*, *Source:*, *Sort by:*).
  - Last updated date (`📅 dd/MM/yyyy`) displayed on every catalog card next to downloads and ratings.
- **Cross-Store Update Detection**:
  - Update checks for installed mods now inspect **all available stores** (Thunderstore, Hexium, etc.) for the newest version, preferring the original store on version ties.
  - When a newer version is found on a different store than the installed one, the update badge in *Installed Mods* explicitly shows the source (e.g. `Update v1.3.1 avail. (🔷 Hexium)`).

#### Fixed
- **Alphabetical Sorting of Installed Mods (`A → Z`)**: The *Installed Mods* list is now consistently sorted alphabetically by mod name (instead of raw installation order), keeping each mod in place even after updating.
- **Automatic Update Badge Refresh on Startup**: Fixed an issue where the *Installed Mods* tab did not immediately display available update badges after the catalog finished loading from cache or refreshing online until a search was triggered.
- **Author Search in Installed Mods**: The search bar in *Installed Mods* now matches both mod name and author namespace.

---

## [1.0.9] - 2026-10-09

### 🇮🇹 Italiano

#### Aggiunto
- **Anteprime Miniature Mod (Thumbnail) nel Catalogo**: Visualizzazione asincrona e non bloccante delle immagini di anteprima delle mod con sistema di caching a due livelli: cache in-memory e cache persistente su disco (`%LOCALAPPDATA%\ValheimModManager\cache\icons\`). Fallback elegante con icona 📦 per mod prive di immagine remota.
- **Riprogettazione Layout Scheda Catalogo**:
  - Riga 1: Nome mod in risalto (15px bold) e Autore all'interno di un `WrapPanel` per prevenire troncamenti con titoli lunghi.
  - Riga 2: Descrizione della mod su 2 righe con wrapping nativo.
  - Riga 3: Statistiche (Download e Voti) affiancate dai badge degli store.
  - Margine di sicurezza di 16px verso la colonna delle azioni a destra (Selettore Fonte, Versione e pulsante Installa) sempre perfettamente allineata e centrata.
- **Logica Multi-Store Intelligente & Risoluzione Parità**:
  - Badge di provenienza chiari e compatti (`⚡ Thunderstore vX.Y.Z` / `🔷 Hexium vX.Y.Z`).
  - Il badge di provenienza viene mostrato anche per mod mono-store, garantendo trasparenza sull'origine del pacchetto.
  - In caso di parità di versione tra più store (es. Thunderstore e Hexium alla stessa versione), vengono mostrati entrambi i badge affiancati riconoscendo entrambe le piattaforme senza preferenze arbitrarie.
  - Se uno store offre una versione precedente, viene mostrata solo la versione più recente e le altre vengono raggruppate in un pill secondario discreto (`+1 fonte precedente`) con tooltip descrittivo.
- **Documentazione Storica Completa**: Changelog retroattivo completo da v1.0.0 a v1.0.8 documentato in `CHANGELOG.md`.

---

### 🇬🇧 English

#### Added
- **Asynchronous Mod Thumbnail Previews in Catalog**: Non-blocking image preview loading with a two-layer cache: high-performance in-memory cache and persistent disk cache (`%LOCALAPPDATA%\ValheimModManager\cache\icons\`). Graceful fallback placeholder 📦 for mods without remote icons.
- **Redesigned Catalog Row Layout**:
  - Row 1: Bold mod title (15px) and author in a responsive `WrapPanel` preventing overflows or clipping on long names.
  - Row 2: Mod description (up to 2 lines) with native text wrapping.
  - Row 3: Mod statistics (Downloads & Ratings) alongside store badges.
  - 16px safety padding isolating right-side action controls (Source Selector, Version Selector, and Install button).
- **Smart Multi-Store Display & Version Tie-Breaker**:
  - Clean and concise provenance badges (`⚡ Thunderstore vX.Y.Z` / `🔷 Hexium vX.Y.Z`).
  - Source provenance badge displayed even for single-source mods for transparency.
  - In case of version ties across multiple repositories, both store badges are displayed side-by-side without arbitrary bias.
  - If a repository has an older release, only the up-to-date source badge is displayed, while legacy versions are grouped under a subtle secondary pill (`+1 older source`) with an informative tooltip.
- **Complete Historical Changelog**: Comprehensive retroactive release history from v1.0.0 to v1.0.8 documented in `CHANGELOG.md`.

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
