# Valheim Mod Manager (VMM)

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Avalonia UI 12](https://img.shields.io/badge/Avalonia%20UI-12.1.3-8A2BE2?logo=avalonia&logoColor=white)](https://avaloniaui.net/)
[![CI/CD](https://github.com/VasariRulez/ValheimModManager/actions/workflows/release.yml/badge.svg)](https://github.com/VasariRulez/ValheimModManager/actions/workflows/release.yml)
[![Release](https://img.shields.io/github/v/release/VasariRulez/ValheimModManager?logo=github&color=blue)](https://github.com/VasariRulez/ValheimModManager/releases)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%28Steam%20Deck%29-blue)](https://github.com/VasariRulez/ValheimModManager)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Valheim Mod Manager** è un mod manager moderno, ultra-performante e multipiattaforma per **Valheim**, sviluppato con **.NET 10** e **Avalonia UI 12**.

Progettato per superare le limitazioni dei manager tradizionali, integra il supporto nativo multi-fonte (**Thunderstore** e **Hexium**), un'architettura a profili isolati, la condivisione istantanea tramite **Share Code (`vmm1-...`)**, l'interoperabilità con **r2modman**, l'esportazione per **Server Dedicati**, il lancio tramite **Steam con Overlay abilitato** e la piena compatibilità sia per sistemi desktop **Windows** che per **Steam Deck / Linux**.

---

## ✨ Funzionalità Principali

### 🌐 Catalogo Multi-Fonte Unificato & Miniature con Cache
- **Thunderstore & Hexium**: Consultazione e download integrati dai cataloghi ufficiali di Thunderstore e [Hexium](https://valheim.hexium.gg/).
- **Raggruppamento Intelligente & Parità Versioni**: Riconosce quando la stessa mod è pubblicata su più store (tramite autore e nome canonico) e raggruppa le release in un'unica scheda, mostrando i badge di provenienza (`⚡ Thunderstore vX.Y.Z` / `🔷 Hexium vX.Y.Z`) — entrambi affiancati in caso di parità di versione, oppure raggruppando le fonti meno recenti in un pill discreto (`+1 fonte precedente`).
- **Anteprime Miniature (Thumbnail) Asincrone**: Caricamento non bloccante delle icone delle mod con caching a due livelli (RAM ad alte prestazioni + cache persistente su disco in `%LOCALAPPDATA%\ValheimModManager\cache\icons\`) e fallback `📦`.
- **Statistiche Complete su Scheda**: Numero totale di download, valutazioni della community e data di ultimo aggiornamento (`📅 gg/mm/aaaa`).

### 🎯 Ricerca Ponderata per Rilevanza & Ordinamento Avanzato
- **6 Criteri di Ordinamento**: Selettore dedicato per ordinare il catalogo per **🎯 Rilevanza**, **⬇ Download**, **🕒 Ultimo aggiornamento**, **★ Valutazione**, **🔤 Nome mod** o **👤 Autore**, con pulsante per invertire la direzione (`↓` / `↑`).
- **Ricerca Multi-Campo a 5 Fasce di Rilevanza**: Digitando nella barra di ricerca (con debounce a 300ms), l'ordinamento passa automaticamente a *Rilevanza* classificando i risultati per: *Match esatto sul Nome > Prefisso sul Nome > Nome contiene > Autore contiene > Descrizione contiene*.
- **Elenco Mod Installate Ordinato (`A → Z`) & Filtro Rapido**: Visualizzazione sempre ordinata alfabeticamente per nome della mod con ricerca reattiva (debounce 200ms) per nome o autore.

### 🧩 Gestione BepInEx 5, Aggiornamenti Cross-Store & Installazione Manuale
- **BepInEx Automatico**: Rilevamento, download, installazione e configurazione trasparente di BepInEx 5 per ogni profilo con verifica dei file Doorstop (`winhttp.dll` / `doorstop_config.ini`).
- **Risoluzione Dipendenze Ricorsiva**: Risolve e installa automaticamente l'intero albero dei prerequisiti richiesti da ciascuna mod.
- **Controllo Aggiornamenti Cross-Store**: Verifica la disponibilità di nuove versioni su **tutti gli store disponibili** (anche se una mod installata da Thunderstore riceve un aggiornamento più recente su Hexium o viceversa, segnalandolo sul badge es. `Aggiornamento v1.3.1 disp. (🔷 Hexium)`).
- **Installazione Manuale (Drag & Drop)**: Supporta l'installazione di mod locali (es. scaricate da Nexus Mods) trascinando file `.zip` o `.dll` direttamente nella finestra dell'app o tramite il pulsante *➕ Installa da File...*, contrassegnandole con il badge *Manuale*.
- **Toggle Attivazione/Disattivazione & Disinstallazione Pulita**: Abilita o disabilita istantaneamente le mod (`.dll` ↔ `.dll.disabled`) e rimuove in modo pulito file e cartelle residue in fase di disinstallazione.

### 📂 Profili Isolati, Share Code (`vmm1-...`), r2modman & Server Dedicato
- **Isolamento Totale**: Ogni profilo mantiene la propria cartella `BepInEx/plugins`, `patchers` e `config` indipendente, con accesso rapido alle cartelle su disco in un clic.
- **Condivisione Rapida con Share Code (`vmm1-...`)**: Esporta e importa interi profili (inclusa la lista mod e tutti i file di configurazione `BepInEx/config/*.cfg`) tramite una singola stringa testuale compressa copiabile negli appunti.
- **Esportazione/Importazione `.vmmprofile`**: Salvataggio e ripristino completo dei profili su file.
- **Interoperabilità con r2modman**:
  - Importazione diretta da archivi profilo esportati da r2modman (`.r2z`).
  - Importazione online tramite codice di condivisione r2modman (UUID `018f...`).
- **Esportazione Pacchetto Server Dedicato (`.zip`)**: Genera in un clic un archivio `.zip` preconfigurato contenente BepInEx, plugin e configurazioni del profilo attivo, pronto per essere estratto su un server dedicato Valheim.

### 🎮 Avvio tramite Steam (Steam Overlay), Parametri Custom & Auto-Updater
- **Avvio tramite Steam con Overlay Abilitato**: Avvia Valheim attraverso il client Steam (`-applaunch 892970`) iniettando automaticamente il profilo BepInEx attivo e preservando tutte le funzionalità di **Steam Overlay** (`Shift+Tab`, inviti amici, screenshot `F12`, Steam Input). Include menu contestuale (clic destro su *AVVIA VALHEIM*) per scegliere tra avvio con Steam o avvio diretto dell'eseguibile.
- **Parametri di Avvio Personalizzati**: Supporto ad argomenti CLI personalizzati con pulsanti rapidi (`-console`, `-windowed`, `-popupwindow`, `-gfx-enable-native-jobs`).
- **Rilevamento Automatico Steam & Process Guard**: Individua automaticamente Valheim su Windows (Registro + `libraryfolders.vdf`) e Linux / Steam Deck (nativo, Flatpak, microSD) e monitora il processo di gioco in esecuzione.
- **Auto-Updater Integrato con Changelog Markdown**: Rileva nuove versioni dell'app su GitHub Releases, mostra le note di rilascio formattate con **Markdown nativo** e aggiorna/riavvia automaticamente l'applicazione.
- **Interfaccia Bilingue (🇮🇹 Italiano / 🇬🇧 English)**: Localizzazione completa in tempo reale selezionabile dalla barra superiore.

---

## 🏛️ Architettura della Soluzione

La soluzione è strutturata in progetti indipendenti e modulari:

```text
c:\repo\ValheinModManager\
├── src\
│   ├── ValheimModManager.Core\             # Dominio, Servizi (Catalog, Install, Profile, BepInEx, Update, Share, R2Modman)
│   ├── ValheimModManager.Platform.Windows\ # Rilevamento Steam via Registry e monitoraggio processi Windows
│   ├── ValheimModManager.Platform.Linux\   # Percorsi Steam Deck / Flatpak e processi Linux
│   └── ValheimModManager.App\              # Applicazione Desktop Avalonia 12 (MVVM, Fluent Dark Theme, IT/EN)
├── tests\
│   └── ValheimModManager.Tests\            # 73 Test automatizzati (xUnit)
├── scripts\
│   ├── publish.ps1                         # Script di pubblicazione post-build (PowerShell)
│   └── publish.sh                          # Script di pubblicazione post-build (Bash / CI)
├── .github\workflows\
│   └── release.yml                         # Pipeline CI/CD GitHub Actions su tag release
├── CHANGELOG.md                            # Storico completo delle versioni (IT/EN)
└── ValheimModManager.slnx                  # Solution file .NET 10
```

---

## 🚀 Guida per lo Sviluppo (Quickstart)

### Prerequisiti
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (versione 10.0.100 o superiore).

### Compilazione & Esecuzione
```bash
# Ripristino e compilazione della soluzione
dotnet build ValheimModManager.slnx

# Avvio dell'applicazione desktop
dotnet run --project src/ValheimModManager.App/ValheimModManager.App.csproj
```

### Esecuzione dei Test Automatizzati
La suite comprende **73 test unitari e di integrazione** (xUnit) per installazione pacchetti, comparazione SemVer, risoluzione dipendenze, raggruppamento e ordinamento multi-fonte, ricerca per rilevanza, aggiornamenti cross-store, Share Code `vmm1`, import r2modman, installazione manuale, avvio Steam e localizzazione:

```bash
dotnet test ValheimModManager.slnx
```

---

## 📦 Pubblicazione Post-Build & Packaging

Il repository include script dedicati per generare pacchetti distribuiti **Self-Contained**, **Single-File** e compilati con **ReadyToRun** (tempo di avvio istantaneo e nessun runtime .NET richiesto all'utente finale).

### Da Windows (PowerShell)
```powershell
# Pubblicazione completa per entrambi i target (win-x64 e linux-x64) con test preventivi:
.\scripts\publish.ps1

# Pubblica solo per Windows:
.\scripts\publish.ps1 -Runtime win-x64

# Pubblica solo per Linux / Steam Deck saltando i test:
.\scripts\publish.ps1 -Runtime linux-x64 -SkipTests

# Pulizia e pubblicazione con numero di versione personalizzato:
.\scripts\publish.ps1 -Version "1.0.10" -Clean
```

### Da Linux / Steam Deck / CI (Bash)
```bash
chmod +x ./scripts/publish.sh

# Pubblicazione completa:
./scripts/publish.sh

# Solo target Linux:
./scripts/publish.sh -r linux-x64
```

### Output Generato
I file vengono generati nella cartella `dist/` (esclusa da git):
- `ValheimModManager-<version>-win-x64.zip` (eseguibile autosufficiente `ValheimModManager.exe`)
- `ValheimModManager-<version>-linux-x64.tar.gz` (binario ELF autosufficiente `ValheimModManager`)
- `SHA256SUMS.txt` (checksum crittografici SHA-256 di tutti gli archivi generati)

---

## 🔄 CI/CD & Rilascio Automatico

Il workflow GitHub Actions situato in [`.github/workflows/release.yml`](.github/workflows/release.yml) è configurato per:
1. Avviarsi automaticamente al push di una tag di versione (es. `git tag v1.0.10 && git push origin v1.0.10`) o tramite esecuzione manuale da GitHub (*Run workflow*).
2. Eseguire la compilazione e i 73 test in parallelo su runner Windows e Ubuntu.
3. Estrarre automaticamente le note di rilascio della versione corrispondente da [`CHANGELOG.md`](CHANGELOG.md) e pubblicare la GitHub Release allegando gli archivi `.zip`, `.tar.gz` e `SHA256SUMS.txt`.

---

## 📄 Licenza

Distribuito con licenza MIT. Consulta il file [`LICENSE`](LICENSE) per ulteriori dettagli.
