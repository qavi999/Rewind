param(
    [string]$Release = "v0.2",
    [string]$Build = "0.7",
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

if (![string]::IsNullOrEmpty($Version)) {
    if ($Version -match "^(v?[\d\.]+)[\\/]([\d\.]+)$") {
        $Release = $matches[1]
        $Build = $matches[2]
    } elseif ($Version.StartsWith("v")) {
        $Release = $Version
    } else {
        $Release = "v$Version"
    }
}

$rootDir = Split-Path -Parent $PSScriptRoot
$srcDir = Join-Path $rootDir "src"
$buildDir = Join-Path $rootDir "builds\$Release\$Build"
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
    "$srcDir\AssemblyInfo.cs",
    "$srcDir\Settings.cs",
    "$srcDir\AppLocalization.cs",
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

Write-Host "Kompilacja samodzielnego Rewind $Release ($Build) (Standalone All-In-One EXE)..." -ForegroundColor Cyan
$cmdArgs = @($targetArg, $platformArg, $optimizeArg, $iconArg, $resFfmpeg, $resIcon, $resSound, $outArg) + $refs + $sources
& $csc $cmdArgs

if ($LASTEXITCODE -eq 0 -and (Test-Path "$buildDir\Rewind.exe")) {
    $exeSizeMB = [math]::Round((Get-Item "$buildDir\Rewind.exe").Length / 1MB, 2)
    Write-Host "Sukces! Samodzielny plik EXE gotowy: $buildDir\Rewind.exe ($exeSizeMB MB)" -ForegroundColor Green

    # Kompilacja instalatora RewindSetup.exe
    Write-Host "Kompilacja instalatora RewindSetup.exe..." -ForegroundColor Cyan
    $installerSources = @("$srcDir\AssemblyInfo.cs", "$srcDir\Installer.cs")
    $installerOutArg = "/out:`"$buildDir\RewindSetup.exe`""
    $resPayload = "/resource:`"$buildDir\Rewind.exe`",rewind_payload.exe"
    $installerArgs = @($targetArg, $platformArg, $optimizeArg, $iconArg, $resIcon, $resPayload, $installerOutArg) + $refs + $installerSources
    & $csc $installerArgs

    if ($LASTEXITCODE -eq 0 -and (Test-Path "$buildDir\RewindSetup.exe")) {
        $setupSizeMB = [math]::Round((Get-Item "$buildDir\RewindSetup.exe").Length / 1MB, 2)
        Write-Host "Sukces! Instalator gotowy: $buildDir\RewindSetup.exe ($setupSizeMB MB)" -ForegroundColor Green
    }

    # =========================================================================
    # Budowanie i pakowanie wersji dla systemu Linux
    # =========================================================================
    Write-Host "Pakowanie wersji dla systemu Linux..." -ForegroundColor Cyan
    $srcLinux = Join-Path $srcDir "linux"
    $assetsB64 = Join-Path $srcLinux "assets_b64.py"
    $rewindPy = Join-Path $srcLinux "rewind.py"
    $logoPng = Join-Path $rootDir "assets\logo.png"

    if ((Test-Path $assetsB64) -and (Test-Path $rewindPy)) {
        $assetsContent = [System.IO.File]::ReadAllText($assetsB64, [System.Text.Encoding]::UTF8)
        $rewindContent = [System.IO.File]::ReadAllText($rewindPy, [System.Text.Encoding]::UTF8)
        if ($rewindContent.Contains("from assets_b64 import")) {
            $rewindContent = $rewindContent -replace 'try:\s+from assets_b64 import[^\n]+\nexcept ImportError:\s+CLIP_WAV_B64 = ""\s+LOGO_PNG_B64 = ""', $assetsContent
        }
        $rewindContent = $rewindContent.Replace("`r`n", "`n")

        $standaloneBin = Join-Path $buildDir "rewind-linux"
        [System.IO.File]::WriteAllText($standaloneBin, $rewindContent, (New-Object System.Text.UTF8Encoding($false)))

        # Compress rewind-linux into gzip base64 payload to embed inside RewindSetup
        $rawBytes = [System.Text.Encoding]::UTF8.GetBytes($rewindContent)
        $ms = New-Object System.IO.MemoryStream
        $gz = New-Object System.IO.Compression.GZipStream($ms, [System.IO.Compression.CompressionMode]::Compress)
        $gz.Write($rawBytes, 0, $rawBytes.Length)
        $gz.Dispose()
        $payloadGzB64 = [Convert]::ToBase64String($ms.ToArray())
        $ms.Dispose()

        # Build standalone self-contained installer (RewindSetup) with embedded app payload
        $installerPy = Join-Path $srcLinux "installer.py"
        $standaloneInstaller = Join-Path $buildDir "RewindSetup"
        if (Test-Path $installerPy) {
            $installerContent = [System.IO.File]::ReadAllText($installerPy, [System.Text.Encoding]::UTF8)
            if ($installerContent.Contains("from assets_b64 import")) {
                $installerContent = $installerContent -replace 'try:\s+from assets_b64 import[^\n]+\nexcept ImportError:\s+LOGO_PNG_B64 = ""', $assetsContent
            }
            $installerContent = $installerContent.Replace('PAYLOAD_GZ_B64 = ""', "PAYLOAD_GZ_B64 = `"`"`"$payloadGzB64`"`"`"")
            $installerContent = $installerContent.Replace("`r`n", "`n")
            [System.IO.File]::WriteAllText($standaloneInstaller, $installerContent, (New-Object System.Text.UTF8Encoding($false)))
        }

        Copy-Item (Join-Path $srcLinux "install.sh") (Join-Path $buildDir "install.sh") -Force
        Copy-Item (Join-Path $srcLinux "uninstall.sh") (Join-Path $buildDir "uninstall.sh") -Force

        # Ensure shell scripts have LF line endings
        foreach ($fName in @("install.sh", "uninstall.sh", "RewindSetup")) {
            $fPath = Join-Path $buildDir $fName
            if (Test-Path $fPath) {
                $txt = [System.IO.File]::ReadAllText($fPath).Replace("`r`n", "`n")
                [System.IO.File]::WriteAllText($fPath, $txt, (New-Object System.Text.UTF8Encoding($false)))
            }
        }

        # Create tar.gz archive - contains ONLY the installer setup and launch wrappers!
        # The main application is extracted upon running RewindSetup.
        $tarArchiveName = "Rewind-$Release-Linux.tar.gz"
        $tempPackDir = Join-Path $env:TEMP "rewind_linux_pack_$( [Guid]::NewGuid().ToString('N') )"
        $subPackDir = Join-Path $tempPackDir "Rewind-$Release-Linux"
        New-Item -ItemType Directory -Path $subPackDir -Force | Out-Null

        if (Test-Path $standaloneInstaller) {
            Copy-Item $standaloneInstaller (Join-Path $subPackDir "RewindSetup") -Force
        }
        Copy-Item (Join-Path $srcLinux "install.sh") (Join-Path $subPackDir "install.sh") -Force
        Copy-Item (Join-Path $srcLinux "uninstall.sh") (Join-Path $subPackDir "uninstall.sh") -Force
        Copy-Item (Join-Path $srcLinux "README.md") (Join-Path $subPackDir "README.md") -Force

        foreach ($fName in @("install.sh", "uninstall.sh", "RewindSetup")) {
            $fPath = Join-Path $subPackDir $fName
            if (Test-Path $fPath) {
                $txt = [System.IO.File]::ReadAllText($fPath).Replace("`r`n", "`n")
                [System.IO.File]::WriteAllText($fPath, $txt, (New-Object System.Text.UTF8Encoding($false)))
            }
        }

        $tarOut = Join-Path $buildDir $tarArchiveName
        Push-Location $tempPackDir
        try {
            & tar.exe -czf $tarArchiveName "Rewind-$Release-Linux"
            Copy-Item $tarArchiveName $tarOut -Force
        } finally {
            Pop-Location
            Remove-Item -Path $tempPackDir -Recurse -Force -ErrorAction SilentlyContinue
        }

        $tarSizeKB = [math]::Round((Get-Item $tarOut).Length / 1KB, 1)
        Write-Host "Sukces! Pakiet Linux gotowy: $tarOut ($tarSizeKB KB)" -ForegroundColor Green
    }

    # Clean any stale files from build directory
    $validFiles = @("Rewind.exe", "RewindSetup.exe", "RewindSetup", "install.sh", "uninstall.sh", "Rewind-$Release-Linux.tar.gz")
    Get-ChildItem -Path $buildDir -File | Where-Object { $validFiles -notcontains $_.Name } | Remove-Item -Force -ErrorAction SilentlyContinue

    # Mirror to v.02 if Release is v0.2 so both folder name formats exist
    if ($Release -eq "v0.2") {
        $altDir = Join-Path $rootDir "builds\v.02\$Build"
        if (!(Test-Path $altDir)) {
            New-Item -ItemType Directory -Path $altDir -Force | Out-Null
        }
        Get-ChildItem -Path $buildDir -File | ForEach-Object {
            Copy-Item $_.FullName (Join-Path $altDir $_.Name) -Force -ErrorAction SilentlyContinue
        }
    }
} else {
    Write-Host "Błąd kompilacji!" -ForegroundColor Red
    exit 1
}
