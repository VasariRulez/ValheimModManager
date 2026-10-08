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

#### Modificato
- **Fallback automatico affidabile**: Se il client Steam non è installato o non viene rilevato (oppure per il Dedicated Server), il gioco viene avviato direttamente tramite l'eseguibile senza bloccare l'utente.

---

### 🇬🇧 English

#### Added
- **Launch via Steam & Steam Overlay Injection**: The "LAUNCH VALHEIM" button now launches the game via the Steam client (`-applaunch 892970`), enabling in-game overlay features (Shift+Tab, friend invites, F12 screenshots, and Steam Input controller support) while seamlessly loading Doorstop and BepInEx for the active profile.
- **Quick Launch Context Menu**: Right-clicking the "LAUNCH VALHEIM" button now allows choosing between *"▶  Launch with Steam (Overlay enabled)"* and *"⚡  Launch directly (without Steam)"*.
- **Settings Toggle**: Added a *"Launch via Steam (Steam Overlay enabled)"* toggle under Settings (Launch Options section, enabled by default) to customize the launch behavior.

#### Changed
- **Resilient Fallback**: If the Steam client is not installed or cannot be located (or when launching Dedicated Server), the launcher falls back to starting the game executable directly.
