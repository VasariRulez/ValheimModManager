<#
.SYNOPSIS
    Genera le icone dell'applicazione per Valheim Mod Manager a partire da un'immagine sorgente PNG.

.DESCRIPTION
    Crea un file .ico multi-risoluzione (16, 24, 32, 48, 64, 128, 256 px) compatibile con Windows
    e diverse versioni di PNG ad alta qualità per Avalonia UI e Linux desktop.

.PARAMETER SourcePath
    Percorso dell'immagine sorgente PNG (default: .BRAIN/Icon.png).

.PARAMETER TargetDir
    Cartella di destinazione degli asset generati (default: src/ValheimModManager.App/Assets).
#>

[CmdletBinding()]
param (
    [string]$SourcePath = "",
    [string]$TargetDir = ""
)

$ErrorActionPreference = "Stop"

$ScriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..")).Path

if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $SourcePath = Join-Path $RepoRoot ".BRAIN\Icon.png"
}

if ([string]::IsNullOrWhiteSpace($TargetDir)) {
    $TargetDir = Join-Path $RepoRoot "src\ValheimModManager.App\Assets"
}

if (-not (Test-Path $SourcePath)) {
    Write-Error "Immagine sorgente non trovata in: $SourcePath"
    exit 1
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Valheim Mod Manager - Generatore Icone Applicazione    " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Sorgente:    $SourcePath"
Write-Host "Destinazione: $TargetDir"

if (-not (Test-Path $TargetDir)) {
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
}

Add-Type -AssemblyName System.Drawing

$srcImage = [System.Drawing.Image]::FromFile($SourcePath)
Write-Host "Risoluzione sorgente: $($srcImage.Width)x$($srcImage.Height) px" -ForegroundColor Green

function Resize-Bitmap {
    param (
        [System.Drawing.Image]$Source,
        [int]$Width,
        [int]$Height
    )
    $destRect = [System.Drawing.Rectangle]::new(0, 0, $Width, $Height)
    $destImage = [System.Drawing.Bitmap]::new($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $destImage.SetResolution($Source.HorizontalResolution, $Source.VerticalResolution)

    $graphics = [System.Drawing.Graphics]::FromImage($destImage)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    $wrapMode = [System.Drawing.Imaging.ImageAttributes]::new()
    $wrapMode.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
    $graphics.DrawImage($Source, $destRect, 0, 0, $Source.Width, $Source.Height, [System.Drawing.GraphicsUnit]::Pixel, $wrapMode)

    $wrapMode.Dispose()
    $graphics.Dispose()
    return $destImage
}

# 1. Generazione PNG varie dimensioni
$pngSizes = @(
    @{ Name = "icon.png"; Width = 512; Height = 512 },
    @{ Name = "icon-256.png"; Width = 256; Height = 256 },
    @{ Name = "icon-32.png"; Width = 32; Height = 32 }
)

foreach ($item in $pngSizes) {
    $outPath = Join-Path $TargetDir $item.Name
    $bmp = Resize-Bitmap -Source $srcImage -Width $item.Width -Height $item.Height
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Generato: $($item.Name) ($($item.Width)x$($item.Height))" -ForegroundColor Cyan
}

# 2. Generazione .ICO multi-frame
$icoSizes = @(16, 24, 32, 48, 64, 128, 256)
$icoPngStreams = [System.Collections.Generic.List[byte[]]]::new()

foreach ($sz in $icoSizes) {
    $bmp = Resize-Bitmap -Source $srcImage -Width $sz -Height $sz
    $ms = [System.IO.MemoryStream]::new()
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $icoPngStreams.Add($ms.ToArray())
    $bmp.Dispose()
    $ms.Dispose()
}

$srcImage.Dispose()

$icoPath = Join-Path $TargetDir "icon.ico"
$fs = [System.IO.FileStream]::new($icoPath, [System.IO.FileMode]::Create)
$bw = [System.IO.BinaryWriter]::new($fs)

# ICONDIR header
$bw.Write([uint16]0)                 # Reserved (must be 0)
$bw.Write([uint16]1)                 # Resource type (1 for icon)
$bw.Write([uint16]$icoSizes.Count)   # Number of images

# Calculate offset after header and directory entries
$offset = 6 + ($icoSizes.Count * 16)

# Write ICONDIRENTRY for each size
for ($i = 0; $i -lt $icoSizes.Count; $i++) {
    $sz = $icoSizes[$i]
    $data = $icoPngStreams[$i]
    $bWidth = if ($sz -ge 256) { [byte]0 } else { [byte]$sz }
    $bHeight = if ($sz -ge 256) { [byte]0 } else { [byte]$sz }

    $bw.Write([byte]$bWidth)          # Width (0 = 256)
    $bw.Write([byte]$bHeight)         # Height (0 = 256)
    $bw.Write([byte]0)                # Color count (0 if >= 8bpp)
    $bw.Write([byte]0)                # Reserved
    $bw.Write([uint16]1)              # Color planes
    $bw.Write([uint16]32)             # Bits per pixel
    $bw.Write([uint32]$data.Length)   # Size of image data
    $bw.Write([uint32]$offset)        # Offset to image data

    $offset += $data.Length
}

# Write image data blocks
for ($i = 0; $i -lt $icoSizes.Count; $i++) {
    $bw.Write($icoPngStreams[$i])
}

$bw.Flush()
$bw.Dispose()
$fs.Dispose()

Write-Host "Generato: icon.ico con 7 frame ($($icoSizes -join ', ') px)" -ForegroundColor Cyan
Write-Host "`nOperazione completata con successo!" -ForegroundColor Green
