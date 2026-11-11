param(
    [string]$Version = "0.5"
)

$ErrorActionPreference = "Stop"

$rootDir = Split-Path -Parent $PSScriptRoot
$srcDir = Join-Path $rootDir "src"
$buildDir = Join-Path $rootDir "builds\$Version"
$toolsDir = Join-Path $rootDir "tools"

if (!(Test-Path $buildDir)) {
    New-Item -ItemType Directory -Path $buildDir -Force | Out-Null
}

$ffmpegExe = Join-Path $toolsDir "ffmpeg.exe"
$ffmpegGz = Join-Path $toolsDir "ffmpeg.gz"

if (!(Test-Path $ffmpegGz) -and (Test-Path $ffmpegExe)) {
    Write-Host "Kompresowanie ffmpeg do ffmpeg.gz..." -ForegroundColor Yellow
    $inStream = [System.IO.File]::OpenRead($ffmpegExe)
    $outStream = [System.IO.File]::Create($ffmpegGz)
    $gzStream = New-Object System.IO.Compression.GZipStream($outStream, [System.IO.Compression.CompressionMode]::Compress)
    $inStream.CopyTo($gzStream)
    $gzStream.Dispose()
    $outStream.Dispose()
    $inStream.Dispose()
}

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$wpf = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"

$sources = @(
    "$srcDir\Settings.cs",
    "$srcDir\NativeMethods.cs",
    "$srcDir\SoundEffect.cs",
    "$srcDir\WasapiLoopback.cs",
    "$srcDir\BufferEngine.cs",
    "$srcDir\MainWindow.cs",
    "$srcDir\Program.cs"
)

$refs = @(
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Xaml.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Web.Extensions.dll",
    "/reference:`"$wpf\WindowsBase.dll`"",
    "/reference:`"$wpf\PresentationCore.dll`"",
    "/reference:`"$wpf\PresentationFramework.dll`""
)

$iconArg = "/win32icon:`"$srcDir\app.ico`""
$resFfmpeg = "/resource:`"$ffmpegGz`",ffmpeg.gz"
$resIcon = "/resource:`"$srcDir\app.ico`",app.ico"
$resSound = "/resource:`"$srcDir\clip.wav`",clip.wav"
$outArg = "/out:`"$buildDir\Rewind.exe`""
$targetArg = "/target:winexe"
$platformArg = "/platform:x64"
$optimizeArg = "/optimize+"

# Stop any running Rewind instances to avoid file lock
Get-Process -Name Rewind,Flashback,ClipFlow -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 300

# Remove old exe to be certain we don't report false positive
if (Test-Path "$buildDir\Rewind.exe") {
    Remove-Item "$buildDir\Rewind.exe" -Force -ErrorAction SilentlyContinue
}

Write-Host "Kompilacja samodzielnego Rewind v$Version (Standalone All-In-One EXE)..." -ForegroundColor Cyan
$cmdArgs = @($targetArg, $platformArg, $optimizeArg, $iconArg, $resFfmpeg, $resIcon, $resSound, $outArg) + $refs + $sources
& $csc $cmdArgs

if ($LASTEXITCODE -eq 0 -and (Test-Path "$buildDir\Rewind.exe")) {
    $exeSizeMB = [math]::Round((Get-Item "$buildDir\Rewind.exe").Length / 1MB, 2)
    Write-Host "Sukces! Samodzielny plik EXE gotowy: $buildDir\Rewind.exe ($exeSizeMB MB)" -ForegroundColor Green

    # Kompilacja instalatora RewindSetup.exe
    Write-Host "Kompilacja instalatora RewindSetup.exe..." -ForegroundColor Cyan
    $installerSources = @("$srcDir\Installer.cs")
    $installerOutArg = "/out:`"$buildDir\RewindSetup.exe`""
    $resPayload = "/resource:`"$buildDir\Rewind.exe`",rewind_payload.exe"
    $installerArgs = @($targetArg, $platformArg, $optimizeArg, $iconArg, $resIcon, $resPayload, $installerOutArg) + $refs + $installerSources
    & $csc $installerArgs

    if ($LASTEXITCODE -eq 0 -and (Test-Path "$buildDir\RewindSetup.exe")) {
        $setupSizeMB = [math]::Round((Get-Item "$buildDir\RewindSetup.exe").Length / 1MB, 2)
        Write-Host "Sukces! Instalator gotowy: $buildDir\RewindSetup.exe ($setupSizeMB MB)" -ForegroundColor Green
    }

    # Clean any stale non-exe files from build directory
    Get-ChildItem -Path $buildDir -File | Where-Object { $_.Name -ne "Rewind.exe" -and $_.Name -ne "RewindSetup.exe" } | Remove-Item -Force -ErrorAction SilentlyContinue

    # Refresh Windows Shell Icon Cache
    try {
        Add-Type -TypeDefinition @"
        using System;
        using System.Runtime.InteropServices;
        public class ShellNotify {
            [DllImport("shell32.dll")]
            public static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
        }
"@
        [ShellNotify]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)
    } catch {}
} else {
    Write-Host "Błąd kompilacji!" -ForegroundColor Red
    exit 1
}
