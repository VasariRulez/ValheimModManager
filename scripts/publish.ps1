<#
.SYNOPSIS
    Script di pubblicazione automatizzata post-build per Valheim Mod Manager.

.DESCRIPTION
    Compila, testa, pubblica e pacchettizza l'applicazione Avalonia .NET 10 per
    Windows (win-x64) e Linux / Steam Deck (linux-x64).
    Genera archivi compressi (.zip / .tar.gz) e checksum SHA-256.

.PARAMETER Runtime
    Target runtime (RID). Valori supportati: "all", "win-x64", "linux-x64". Default: "all".

.PARAMETER Configuration
    Configurazione di compilazione ("Release" o "Debug"). Default: "Release".

.PARAMETER Version
    Versione dell'applicazione. Se omessa, viene letta da ValheimModManager.App.csproj.

.PARAMETER OutputDir
    Cartella di destinazione per i file pubblicati e gli archivi. Default: "dist".

.PARAMETER SkipTests
    Se specificato, salta l'esecuzione della suite di unit test prima del publish.

.PARAMETER FrameworkDependent
    Se specificato, genera una build che richiede .NET 10 già installato nel sistema
    anziché una distribuzione Self-Contained.

.PARAMETER NoArchive
    Se specificato, non genera gli archivi compressi (.zip/.tar.gz).

.PARAMETER Clean
    Se specificato, pulisce preventivamente la cartella di output.

.EXAMPLE
    .\scripts\publish.ps1
    .\scripts\publish.ps1 -Runtime win-x64
    .\scripts\publish.ps1 -Runtime linux-x64 -SkipTests
    .\scripts\publish.ps1 -Version "1.1.0" -Configuration Release
#>

[CmdletBinding()]
param (
    [string]$Runtime = "all",
    [string]$Configuration = "Release",
    [string]$Version = "",
    [string]$OutputDir = "dist",
    [switch]$SkipTests,
    [switch]$FrameworkDependent,
    [switch]$NoArchive,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

# Evita interruzioni anomale su warning/diagnostica stderr dei comandi nativi in PowerShell 7.4+
if (Get-Variable -Name "PSNativeCommandUseErrorActionPreference" -ErrorAction SilentlyContinue) {
    $PSNativeCommandUseErrorActionPreference = $false
}

$ScriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..")).Path
$AppProj = Join-Path $RepoRoot "src\ValheimModManager.App\ValheimModManager.App.csproj"
$Solution = Join-Path $RepoRoot "ValheimModManager.slnx"
$DistPath = Join-Path $RepoRoot $OutputDir

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Valheim Mod Manager - Automated Post-Build Publisher    " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Repo Root:     $RepoRoot"
Write-Host "Configurazione: $Configuration"

# 1. Recupero Versione
if ([string]::IsNullOrWhiteSpace($Version)) {
    if (Test-Path $AppProj) {
        $projContent = [xml](Get-Content $AppProj)
        $Version = $projContent.Project.PropertyGroup.Version | Select-Object -First 1
    }
    if ([string]::IsNullOrWhiteSpace($Version)) {
        $Version = "1.0.0"
    }
}
Write-Host "Versione:       $Version"

# 2. Pulizia cartella output se richiesta
if ($Clean -and (Test-Path $DistPath)) {
    Write-Host "`n[Pulizia] Rimozione cartella $DistPath..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $DistPath
}

if (-not (Test-Path $DistPath)) {
    New-Item -ItemType Directory -Path $DistPath -Force | Out-Null
}

# 3. Esecuzione Test preventivi
if (-not $SkipTests) {
    Write-Host "`n[1/4] Esecuzione suite di test automatizzati..." -ForegroundColor Green
    dotnet test $Solution -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Error "I test automatizzati sono falliti! Pubblicazione interrotta."
        exit 1
    }
    Write-Host "Tutti i test sono stati superati con successo!" -ForegroundColor Green
} else {
    Write-Host "`n[1/4] Test automatizzati ignorati (-SkipTests specificato)." -ForegroundColor Yellow
}

# 4. Riconoscimento Runtimes da pubblicare
$targetRuntimes = @()
if ($Runtime -eq "all") {
    $targetRuntimes = @("win-x64", "linux-x64")
} else {
    $targetRuntimes = @($Runtime)
}

$selfContained = -not $FrameworkDependent
$generatedPackages = @()

# 5. Esecuzione Publish per ciascun runtime
Write-Host "`n[2/4] Compilazione e Pubblicazione pacchetti..." -ForegroundColor Green

foreach ($rid in $targetRuntimes) {
    Write-Host "`n--> Pubblicazione target: $rid (SelfContained=$selfContained, SingleFile=True)..." -ForegroundColor Magenta
    $targetDist = Join-Path $DistPath $rid

    if (Test-Path $targetDist) {
        Remove-Item -Recurse -Force $targetDist
    }
    New-Item -ItemType Directory -Path $targetDist -Force | Out-Null

    $publishArgs = @(
        "publish", $AppProj,
        "-c", $Configuration,
        "-r", $rid,
        "--self-contained", $selfContained.ToString().ToLower(),
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:PublishReadyToRun=true",
        "-p:PublishTrimmed=false",
        "-p:EnableCompressionInSingleFile=true",
        "-p:Version=$Version",
        "-o", $targetDist,
        "--nologo"
    )

    dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Pubblicazione fallita per il runtime $rid."
        exit 1
    }

    # Pulizia file debug (.pdb) dalla cartella di distribuzione
    Get-ChildItem -Path $targetDist -Filter "*.pdb" | Remove-Item -Force -ErrorAction SilentlyContinue

    Write-Host "Target $rid generato correttamente in: $targetDist" -ForegroundColor Green

    # 6. Creazione Archivio compresso
    if (-not $NoArchive) {
        Write-Host "[3/4] Creazione archivio compresso per $rid..." -ForegroundColor Green
        
        if ($rid -like "win*") {
            $zipName = "ValheimModManager-$Version-$rid.zip"
            $zipPath = Join-Path $DistPath $zipName
            if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
            
            Compress-Archive -Path "$targetDist\*" -DestinationPath $zipPath -Force
            $generatedPackages += $zipPath
            Write-Host "Archivio creato: $zipPath" -ForegroundColor Cyan
        }
        elseif ($rid -like "linux*") {
            $tarName = "ValheimModManager-$Version-$rid.tar.gz"
            $tarPath = Join-Path $DistPath $tarName
            if (Test-Path $tarPath) { Remove-Item -Force $tarPath }

            # Usa tar se presente (supportato nativamente in Win10/Win11 e Linux)
            $tarExe = Get-Command "tar" -ErrorAction SilentlyContinue
            if ($tarExe) {
                & tar.exe -czf $tarPath -C $targetDist .
                $generatedPackages += $tarPath
                Write-Host "Archivio creato (.tar.gz): $tarPath" -ForegroundColor Cyan
            } else {
                # Fallback su zip se tar non è disponibile
                $zipName = "ValheimModManager-$Version-$rid.zip"
                $zipPath = Join-Path $DistPath $zipName
                if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
                Compress-Archive -Path "$targetDist\*" -DestinationPath $zipPath -Force
                $generatedPackages += $zipPath
                Write-Host "Archivio creato (.zip fallback): $zipPath" -ForegroundColor Cyan
            }
        }
    }
}

# 7. Calcolo Checksum SHA-256
$allArchiveFiles = @(Get-ChildItem -Path $DistPath -File | Where-Object { $_.Name -like "*.zip" -or $_.Name -like "*.tar.gz" })
if ($allArchiveFiles.Count -gt 0) {
    Write-Host "`n[4/4] Calcolo Checksum SHA-256..." -ForegroundColor Green
    $checksumFile = Join-Path $DistPath "SHA256SUMS.txt"
    $checksumLines = @()

    foreach ($pkg in $allArchiveFiles) {
        $hash = (Get-FileHash -Path $pkg.FullName -Algorithm SHA256).Hash.ToLower()
        $checksumLines += "$hash  $($pkg.Name)"
    }

    $checksumLines | Set-Content -Path $checksumFile -Encoding utf8
    Write-Host "Checksum salvati in: $checksumFile" -ForegroundColor Cyan
}

# 8. Riepilogo Finale
Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "  PUBBLICAZIONE COMPLETATA CON SUCCESSO!                  " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "Cartella artefatti: $DistPath`n"

Get-ChildItem -Path $DistPath -File | ForEach-Object {
    $sizeMb = [math]::Round($_.Length / 1MB, 2)
    Write-Host ("  {0,-42} {1,8} MB" -f $_.Name, $sizeMb) -ForegroundColor White
}
Write-Host ""
