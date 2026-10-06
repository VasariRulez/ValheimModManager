# Valheim Mod Manager (VMM)

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Avalonia UI 12](https://img.shields.io/badge/Avalonia%20UI-12.1.3-8A2BE2?logo=avalonia&logoColor=white)](https://avaloniaui.net/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%28Steam%20Deck%29-blue)](https://github.com/)
[![Tests](https://img.shields.io/badge/Tests-23%20passed-brightgreen)](https://github.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Valheim Mod Manager** è un mod manager moderno, ultra-performante e multipiattaforma per **Valheim**, sviluppato con **.NET 10** e **Avalonia UI 12**.

Progettato per superare le limitazioni dei manager tradizionali, integra il supporto nativo multi-fonte (Thunderstore e Hexium), un'architettura a profili isolati, l'interoperabilità con r2modman e la piena compatibilità sia per sistemi desktop **Windows** che per **Steam Deck / Linux**.

---

## ✨ Funzionalità Principali

### 🌐 Catalogo Multi-Fonte Unificato
- **Thunderstore & Hexium**: Download e consultazione integrata dai cataloghi ufficiali di Thunderstore e [Hexium](https://valheim.hexium.gg/).
- **Raggruppamento Intelligente**: Riconosce quando la stessa mod è distribuita su più piattaforme (es. tramite il medesimo autore e nome) e raggruppa le release in una sola scheda.
- **Confronto Versioni SemVer**: Mostra chiaramente quale fonte offre la versione più aggiornata (con badge dedicato ⭐ *Più recente*) e consente all'utente di scegliere da quale store installare.
- **Provider Estensibile**: Architettura modulare basata su `IModProvider`, pronta per l'aggiunta di ulteriori sorgenti (es. Nexus Mods).

### ⚡ Ricerca & Filtri Reattivi con Debounce
- **Debounce 300ms** sul catalogo online: la ricerca parte fluidamente durante la digitazione senza bloccare l'interfaccia.
- **Debounce 200ms** sull'elenco delle mod installate per un filtraggio istantaneo.
- Selettore sorgente reattivo (*Tutte le fonti*, *Thunderstore*, *Hexium*).

### 🧩 Gestione BepInEx 5 & Lifecycle Mod
- **BepInEx Automatico**: Rilevamento, installazione e configurazione trasparente di BepInEx 5 per ogni profilo con verifica dei ganci di gioco (`winhttp.dll` / Doorstop).
- **Risoluzione Dipendenze Ricorsiva**: Installa a catena tutti i prerequisiti necessari per ciascun pacchetto.
- **Toggle Attivazione/Disattivazione**: Commuta istantaneamente lo stato di una mod rinominando i binari (`.dll` <-> `.dll.disabled`) senza rischiare corruzioni.
- **Disinstallazione Pulita**: Rimuove i file installati ed elimina automaticamente le cartelle vuote residue.
- **Aggiornamenti in un Click**: Rilevamento automatico delle versioni più recenti e pulsante dedicato *Aggiorna Tutte le Mod*.

### 📂 Sistema di Profili Indipendenti
- **Isolamento Totale**: Ogni profilo mantiene la propria cartella `BepInEx/plugins` e `BepInEx/config` dedicata.
- **Strumenti Completi di Profilo**: Creazione rapida, duplicazione/clonazione, esportazione e importazione in formato compresso `.vmmprofile`.
- **Interoperabilità con r2modman**:
  - Importazione diretta da pacchetti profilo esportati da r2modman (`.r2z`).
  - Importazione remota tramite codice di condivisione alfanumerico r2modman (es. codici `018f...`).

### 🎮 Rilevamento Gioco & Process Supervision
- **Rilevamento Automatico Steam**:
  - **Windows**: lettura del Registro di sistema e scansione di `libraryfolders.vdf`.
  - **Linux / Steam Deck**: scansione dei percorsi Steam nativi, Flatpak e microSD.
- **Selezione Manuale Cartella**: Permette di specificare percorsi personalizzati con validazione immediata della presenza di `valheim.exe`.
- **Process Guard**: Monitora lo stato di esecuzione di Valheim per prevenire sovrascritture di file bloccati durante il gioco.

---

## 🏛️ Architettura della Soluzione

La soluzione è strutturata in progetti indipendenti e modulari:

```text
c:\repo\ValheinModManager\
├── src\
│   ├── ValheimModManager.Core\             # Dominio, Servizi (Catalog, Install, Profile, BepInEx, Update, R2Modman)
│   ├── ValheimModManager.Platform.Windows\ # Rilevamento Steam via Registry e monitoraggio processi Windows
│   ├── ValheimModManager.Platform.Linux\   # Percorsi Steam Deck / Flatpak e processi Linux
│   └── ValheimModManager.App\              # Applicazione Desktop Avalonia 12 (MVVM, Fluent Dark Theme)
├── tests\
│   └── ValheimModManager.Tests\            # 23 Test automatizzati (xUnit)
├── scripts\
│   ├── publish.ps1                         # Script di pubblicazione post-build (PowerShell)
│   └── publish.sh                          # Script di pubblicazione post-build (Bash / CI)
├── .github\workflows\
│   └── release.yml                         # Pipeline CI/CD GitHub Actions su tag release
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
La suite comprende test di regressione per installazione pacchetti, comparazione SemVer, risoluzione dipendenze, raggruppamento multi-fonte, import r2modman e toggle mod:

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
.\scripts\publish.ps1 -Version "1.0.1" -Clean
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
- `ValheimModManager-<version>-win-x64.zip` (eseguibile autosufficiente `ValheimModManager.App.exe`)
- `ValheimModManager-<version>-linux-x64.tar.gz` (binario ELF autosufficiente `ValheimModManager.App`)
- `SHA256SUMS.txt` (checksum crittografici SHA-256 di tutti gli archivi generati)

---

## 🔄 CI/CD & Rilascio Automatico

Il workflow GitHub Actions situato in [`.github/workflows/release.yml`](.github/workflows/release.yml) è già configurato per:
1. Avviarsi automaticamente al push di una tag di versione (es. `git tag v1.0.0 && git push origin v1.0.0`) o tramite esecuzione manuale da GitHub (*Run workflow*).
2. Eseguire la compilazione e i test in parallelo su runner Windows e Ubuntu.
3. Creare automaticamente la GitHub Release allegando gli archivi `.zip` e `.tar.gz` con le note di rilascio e i checksum.

---

## 📄 Licenza

Distribuito con licenza MIT. Consulta il file `LICENSE` per ulteriori dettagli.
