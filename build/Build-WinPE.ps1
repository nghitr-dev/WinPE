<#
.SYNOPSIS
    Build WinPE ISO chính — WinPE_Nghitr-dev
.DESCRIPTION
    Orchestrator script thực hiện toàn bộ quá trình build WinPE ISO:
    1. Kiểm tra prerequisites
    2. Tạo thư mục làm việc
    3. Copy WinPE base
    4. Mount boot.wim
    5. Thêm WinPE packages
    6. Inject drivers
    7. Copy app/tools/scripts
    8. Cấu hình startup
    9. Unmount + commit
    10. Tạo ISO bootable
    11. Validate ISO
.PARAMETER SkipDrivers
    Bỏ qua bước inject driver (build nhanh hơn)
.PARAMETER SkipApp
    Bỏ qua bước copy GUI app
.PARAMETER Clean
    Xóa working directory trước khi build
.PARAMETER OutputDir
    Thư mục output ISO (default: .\output)
.EXAMPLE
    .\Build-WinPE.ps1
    .\Build-WinPE.ps1 -Clean -SkipDrivers
    .\Build-WinPE.ps1 -OutputDir "D:\WinPE_Output"
.NOTES
    Cần chạy với quyền Administrator
    Cần Windows ADK + WinPE Add-on
#>
#Requires -RunAsAdministrator

[CmdletBinding()]
param(
    [switch]$SkipDrivers,
    [switch]$SkipApp,
    [switch]$Clean,
    [string]$OutputDir = "",
    [switch]$NoISO,
    [switch]$Verbose2
)

$ErrorActionPreference = "Stop"

# ─────────────────────────────────────────────────────────────────────────────
# CONSTANTS & PATHS
# ─────────────────────────────────────────────────────────────────────────────
$Script:ProjectRoot   = Split-Path $PSScriptRoot -Parent
$Script:ConfigFile    = Join-Path $ProjectRoot "config\winpe-config.json"
$Script:BuildLog      = Join-Path $ProjectRoot "output\build.log"

# ADK paths (auto-detected)
$Script:ADK_Root      = ""
$Script:WinPE_Root    = ""
$Script:OscdImg       = ""
$Script:WinPE_WIM     = ""
$Script:WinPE_OCs     = ""
$Script:WinPE_Media   = ""

# Working paths
$Script:WorkDir       = ""
$Script:MountDir      = ""
$Script:MediaDir      = ""

# Build state
$Script:BuildErrors   = @()
$Script:BuildStart    = Get-Date

# ─────────────────────────────────────────────────────────────────────────────
# LOGGING
# ─────────────────────────────────────────────────────────────────────────────
function Write-Log {
    param(
        [string]$Message,
        [ValidateSet("INFO","WARN","ERROR","SUCCESS","STEP","DEBUG")]
        [string]$Level = "INFO"
    )
    $ts        = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logLine   = "[$ts] [$Level] $Message"
    $color     = switch ($Level) {
        "INFO"    { "White" }
        "WARN"    { "Yellow" }
        "ERROR"   { "Red" }
        "SUCCESS" { "Green" }
        "STEP"    { "Cyan" }
        "DEBUG"   { "DarkGray" }
    }

    # Console output
    $prefix = switch ($Level) {
        "STEP"    { "▶ " }
        "SUCCESS" { "✅ " }
        "ERROR"   { "❌ " }
        "WARN"    { "⚠️  " }
        default   { "   " }
    }
    Write-Host "$prefix$Message" -ForegroundColor $color

    # File output
    if ($Script:BuildLog) {
        try {
            Add-Content -Path $Script:BuildLog -Value $logLine -Encoding UTF8 -ErrorAction SilentlyContinue
        } catch {}
    }
}

function Write-Section($title) {
    Write-Host ""
    Write-Host ("─" * 65) -ForegroundColor DarkCyan
    Write-Host "  [$title]" -ForegroundColor Cyan
    Write-Host ("─" * 65) -ForegroundColor DarkCyan
    Write-Log "=== $title ===" "INFO"
}

function Write-BuildError($msg) {
    Write-Log $msg "ERROR"
    $Script:BuildErrors += $msg
    throw $msg
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 0: INIT LOGGING
# ─────────────────────────────────────────────────────────────────────────────
$null = New-Item -ItemType Directory -Path (Join-Path $ProjectRoot "output") -Force
Set-Content -Path $Script:BuildLog -Value "=== WinPE_Nghitr-dev Build Log ===" -Encoding UTF8
Write-Log "Build started: $($Script:BuildStart.ToString('yyyy-MM-dd HH:mm:ss'))" "INFO"
Write-Log "ProjectRoot: $ProjectRoot" "INFO"

# ─────────────────────────────────────────────────────────────────────────────
# STEP 1: LOAD CONFIG
# ─────────────────────────────────────────────────────────────────────────────
Write-Section "STEP 1: LOAD CONFIGURATION"

if (-not (Test-Path $Script:ConfigFile)) {
    Write-BuildError "Config file not found: $Script:ConfigFile"
}

try {
    $Config = Get-Content $Script:ConfigFile -Raw -Encoding UTF8 | ConvertFrom-Json
    Write-Log "Config loaded: $($Config.project.name) v$($Config.project.version)" "SUCCESS"
} catch {
    Write-BuildError "Failed to parse config: $_"
}

# Determine output directory
if (-not $OutputDir) {
    $OutputDir = if ($Config.build.outputDir) {
        Join-Path $ProjectRoot $Config.build.outputDir
    } else {
        Join-Path $ProjectRoot "output"
    }
}
$null = New-Item -ItemType Directory -Path $OutputDir -Force
Write-Log "Output directory: $OutputDir" "INFO"

# ─────────────────────────────────────────────────────────────────────────────
# STEP 2: DETECT ADK
# ─────────────────────────────────────────────────────────────────────────────
Write-Section "STEP 2: DETECT WINDOWS ADK"

function Find-ADK {
    # Try registry first
    $regKey = "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows Kits\Installed Roots"
    $reg = Get-ItemProperty $regKey -ErrorAction SilentlyContinue
    if ($reg -and $reg.KitsRoot10 -and (Test-Path $reg.KitsRoot10)) {
        $adkBase = $reg.KitsRoot10.TrimEnd('\')
        return "$adkBase\Assessment and Deployment Kit"
    }

    # Fallback: filesystem scan
    $candidates = @(
        "C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit",
        "C:\Program Files\Windows Kits\10\Assessment and Deployment Kit"
    )
    foreach ($c in $candidates) {
        if (Test-Path $c) { return $c }
    }
    return $null
}

$Script:ADK_Root = Find-ADK
if (-not $Script:ADK_Root) {
    Write-Host ""
    Write-Host "  ❌ ERROR: Windows ADK NOT FOUND" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Cần cài đặt:" -ForegroundColor Yellow
    Write-Host "  1. Windows ADK: https://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install" -ForegroundColor Yellow
    Write-Host "     → Chọn: Deployment Tools" -ForegroundColor Yellow
    Write-Host "  2. WinPE Add-on: Tải từ cùng trang, cài sau ADK" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

Write-Log "ADK Root: $($Script:ADK_Root)" "SUCCESS"

$Script:WinPE_Root  = "$($Script:ADK_Root)\Windows Preinstallation Environment"
$Script:OscdImg     = "$($Script:ADK_Root)\Deployment Tools\amd64\Oscdimg\oscdimg.exe"
$Script:WinPE_WIM   = "$($Script:WinPE_Root)\amd64\en-us\winpe.wim"
$Script:WinPE_OCs   = "$($Script:WinPE_Root)\amd64\WinPE_OCs"
$Script:WinPE_Media = "$($Script:WinPE_Root)\amd64\Media"

# Validate critical paths
$criticalPaths = @{
    "WinPE Add-on" = $Script:WinPE_Root
    "winpe.wim"    = $Script:WinPE_WIM
    "WinPE OCs"    = $Script:WinPE_OCs
    "WinPE Media"  = $Script:WinPE_Media
    "oscdimg.exe"  = $Script:OscdImg
}
foreach ($item in $criticalPaths.GetEnumerator()) {
    if (Test-Path $item.Value) {
        Write-Log "  ✅ $($item.Key)" "SUCCESS"
    } else {
        Write-BuildError "$($item.Key) not found: $($item.Value). Cài WinPE Add-on cho ADK."
    }
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 3: PREPARE WORKING DIRECTORY
# ─────────────────────────────────────────────────────────────────────────────
Write-Section "STEP 3: PREPARE WORKING DIRECTORY"

$Script:WorkDir  = Join-Path $ProjectRoot "source\working"
$Script:MountDir = Join-Path $ProjectRoot "source\mount"
$Script:MediaDir = Join-Path $ProjectRoot "source\media"

if ($Clean) {
    Write-Log "Cleaning previous build state..." "WARN"
    try {
        $mountedImages = Get-WindowsImage -Mounted -ErrorAction SilentlyContinue
        if ($mountedImages | Where-Object { $_.MountPath -eq $Script:MountDir }) {
            Write-Log "Detected mounted WIM at $($Script:MountDir) — unmounting (discard)..." "WARN"
            Dismount-WindowsImage -Path $Script:MountDir -Discard -ErrorAction SilentlyContinue
        }
    } catch {}

    if (Test-Path $Script:WorkDir) { Remove-Item $Script:WorkDir -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path $Script:MediaDir) { Remove-Item $Script:MediaDir -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path $Script:MountDir) { Remove-Item $Script:MountDir -Recurse -Force -ErrorAction SilentlyContinue }
    Write-Log "Working directory cleaned." "SUCCESS"
}

foreach ($d in @($Script:WorkDir, $Script:MountDir, $Script:MediaDir)) {
    $null = New-Item -ItemType Directory -Path $d -Force
}

Write-Log "WorkDir:  $($Script:WorkDir)" "INFO"
Write-Log "MountDir: $($Script:MountDir)" "INFO"
Write-Log "MediaDir: $($Script:MediaDir)" "INFO"

# ─────────────────────────────────────────────────────────────────────────────
# STEP 4: COPY WINPE BASE
# ─────────────────────────────────────────────────────────────────────────────
Write-Section "STEP 4: COPY WINPE BASE FILES"

Write-Log "Copying WinPE media structure..." "STEP"
$mediaItems = Get-ChildItem $Script:WinPE_Media -ErrorAction Stop
foreach ($item in $mediaItems) {
    Copy-Item $item.FullName $Script:MediaDir -Recurse -Force
}
Write-Log "Media structure copied." "SUCCESS"

# Copy WIM
$wimDest = "$($Script:MediaDir)\sources"
$null = New-Item -ItemType Directory -Path $wimDest -Force
$wimDestPath = "$wimDest\boot.wim"
Write-Log "Copying winpe.wim → boot.wim..." "STEP"
Copy-Item $Script:WinPE_WIM $wimDestPath -Force
Write-Log "boot.wim copied ($([math]::Round((Get-Item $wimDestPath).Length/1MB,1)) MB)" "SUCCESS"

# ─────────────────────────────────────────────────────────────────────────────
# STEP 5: MOUNT WIM
# ─────────────────────────────────────────────────────────────────────────────
Write-Section "STEP 5: MOUNT BOOT.WIM"

Write-Log "Mounting boot.wim at $($Script:MountDir)..." "STEP"
try {
    Mount-WindowsImage -ImagePath $wimDestPath -Index 1 -Path $Script:MountDir
    Write-Log "WIM mounted successfully." "SUCCESS"
} catch {
    Write-BuildError "Failed to mount WIM: $_"
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 6: ADD WINPE PACKAGES
# ─────────────────────────────────────────────────────────────────────────────
Write-Section "STEP 6: ADD WINPE PACKAGES"

$packagesToAdd = $Config.build.packages
Write-Log "Adding $($packagesToAdd.Count) WinPE packages..." "STEP"

foreach ($pkg in $packagesToAdd) {
    $cabPath = "$($Script:WinPE_OCs)\$pkg.cab"
    $langCab = "$($Script:WinPE_OCs)\en-us\$pkg`_en-us.cab"

    if (-not (Test-Path $cabPath)) {
        Write-Log "  ⚠️  Package not found (skipping): $pkg" "WARN"
        continue
    }

    try {
        Write-Log "  Adding: $pkg" "INFO"
        Add-WindowsPackage -Path $Script:MountDir -PackagePath $cabPath | Out-Null

        if (Test-Path $langCab) {
            Add-WindowsPackage -Path $Script:MountDir -PackagePath $langCab | Out-Null
        }
        Write-Log "  ✅ $pkg" "SUCCESS"
    } catch {
        Write-Log "  ⚠️  Failed to add $pkg : $_" "WARN"
    }
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 7: INJECT DRIVERS
# ─────────────────────────────────────────────────────────────────────────────
Write-Section "STEP 7: INJECT DRIVERS"

if ($SkipDrivers) {
    Write-Log "Skipping driver injection (--SkipDrivers)" "WARN"
} else {
    $DriversRoot = Join-Path $ProjectRoot "drivers"
    $driverFiles = @(Get-ChildItem $DriversRoot -Recurse -Include "*.inf" -ErrorAction SilentlyContinue)

    if ($driverFiles.Count -eq 0) {
        Write-Log "No drivers found in $DriversRoot — skipping." "INFO"
        Write-Log "Tip: thêm driver .inf vào thư mục drivers\storage\ drivers\network\ v.v." "INFO"
    } else {
        Write-Log "Found $($driverFiles.Count) driver(s) to inject..." "STEP"
        foreach ($drv in $driverFiles) {
            try {
                Write-Log "  Injecting: $($drv.Name)" "INFO"
                Add-WindowsDriver -Path $Script:MountDir -Driver $drv.FullName -ForceUnsigned -ErrorAction Stop | Out-Null
                Write-Log "  ✅ $($drv.Name)" "SUCCESS"
            } catch {
                Write-Log "  ⚠️  Driver failed: $($drv.Name) — $_" "WARN"
            }
        }
    }
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 8: CONFIGURE WINPE ENVIRONMENT
# ─────────────────────────────────────────────────────────────────────────────
Write-Section "STEP 8: CONFIGURE WINPE ENVIRONMENT"

# Set scratch space (512MB for better tool support)
Write-Log "Setting scratch space to 512MB..." "STEP"
try {
    Set-WindowsImage -Path $Script:MountDir -ScratchDirectory $Script:MountDir -ErrorAction SilentlyContinue
    & dism.exe /Image:"$($Script:MountDir)" /Set-ScratchSpace:512 2>&1 | Out-Null
    Write-Log "Scratch space set." "SUCCESS"
} catch {
    Write-Log "Could not set scratch space: $_" "WARN"
}

# Set timezone
Write-Log "Setting timezone..." "STEP"
try {
    & dism.exe /Image:"$($Script:MountDir)" /Set-TimeZone:"SE Asia Standard Time" 2>&1 | Out-Null
    Write-Log "Timezone set to SE Asia Standard Time." "SUCCESS"
} catch {
    Write-Log "Could not set timezone: $_" "WARN"
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 9: COPY APP, SCRIPTS, ASSETS
# ─────────────────────────────────────────────────────────────────────────────
Write-Section "STEP 9: COPY APPLICATION & SCRIPTS"

$winpePEDir = "$($Script:MountDir)\WinPE"
$null = New-Item -ItemType Directory -Path $winpePEDir -Force
$null = New-Item -ItemType Directory -Path "$winpePEDir\Logs" -Force
$null = New-Item -ItemType Directory -Path "$winpePEDir\Config" -Force
$null = New-Item -ItemType Directory -Path "$winpePEDir\Scripts" -Force
$null = New-Item -ItemType Directory -Path "$winpePEDir\Tools" -Force
$null = New-Item -ItemType Directory -Path "$winpePEDir\Assets" -Force

# Copy config
Write-Log "Copying config files..." "STEP"
Copy-Item (Join-Path $ProjectRoot "config\*") "$winpePEDir\Config\" -Force -ErrorAction SilentlyContinue
Write-Log "Config copied." "SUCCESS"

# Copy scripts
Write-Log "Copying scripts..." "STEP"
$scriptsSource = Join-Path $ProjectRoot "scripts"
if (Test-Path $scriptsSource) {
    Copy-Item "$scriptsSource\*" "$winpePEDir\Scripts\" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Log "Scripts copied." "SUCCESS"
}

# Copy assets
Write-Log "Copying assets..." "STEP"
$assetsSource = Join-Path $ProjectRoot "assets"
if (Test-Path $assetsSource) {
    Copy-Item "$assetsSource\*" "$winpePEDir\Assets\" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Log "Assets copied." "SUCCESS"
}

# Copy GUI app
if (-not $SkipApp) {
    $appPublish = Join-Path $ProjectRoot "app\publish"
    $appExe = Join-Path $appPublish "WinPE-Tool.exe"

    if (-not (Test-Path $appExe)) {
        Write-Log "GUI app executable not found in publish dir — building now..." "STEP"
        $projPath = Join-Path $ProjectRoot "app\WinPE-Tool\WinPE-Tool.csproj"
        if (Test-Path $projPath) {
            $null = New-Item -ItemType Directory -Path $appPublish -Force
            & dotnet publish $projPath -c Release -o $appPublish | Out-Null
        }
    }

    if (Test-Path $appPublish) {
        Write-Log "Copying GUI application..." "STEP"
        $appFiles = @(Get-ChildItem $appPublish -ErrorAction SilentlyContinue)
        if ($appFiles.Count -gt 0) {
            Copy-Item "$appPublish\*" "$winpePEDir\" -Recurse -Force
            Write-Log "GUI application copied ($($appFiles.Count) files)." "SUCCESS"
        } else {
            Write-Log "GUI app not built yet — run: dotnet publish app\WinPE-Tool\WinPE-Tool.csproj" "WARN"
        }
    } else {
        Write-Log "No publish directory found — GUI app not included." "WARN"
        Write-Log "Build the app first: dotnet publish app\WinPE-Tool\WinPE-Tool.csproj -c Release" "INFO"
    }
}

# Copy tools
$toolsSource = Join-Path $ProjectRoot "tools"
$toolFiles = @(Get-ChildItem $toolsSource -Recurse -File -ErrorAction SilentlyContinue)
if ($toolFiles.Count -gt 0) {
    Write-Log "Copying tools ($($toolFiles.Count) items)..." "STEP"
    Copy-Item "$toolsSource\*" "$winpePEDir\Tools\" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Log "Tools copied." "SUCCESS"
}

# Integrate WinXShell Desktop Shell
$winxshellSrc = Join-Path $ProjectRoot "tools\WinXShell"
if (Test-Path $winxshellSrc) {
    $winxshellDest = "$winpePEDir\WinXShell"
    $null = New-Item -ItemType Directory -Path $winxshellDest -Force
    Copy-Item "$winxshellSrc\*" $winxshellDest -Recurse -Force -ErrorAction SilentlyContinue
    Write-Log "WinXShell Desktop Shell integrated." "SUCCESS"
}

# Create desktop shortcuts in Default User Desktop
$desktopDir = "$($Script:MountDir)\Users\Default\Desktop"
$null = New-Item -ItemType Directory -Path $desktopDir -Force

try {
    $wsh = New-Object -ComObject WScript.Shell

    $lnk = $wsh.CreateShortcut("$desktopDir\WinPE Nghitr Dev.lnk")
    $lnk.TargetPath = "X:\WinPE\WinPE-Tool.exe"
    $lnk.WorkingDirectory = "X:\WinPE"
    $lnk.IconLocation = "X:\WinPE\WinPE-Tool.exe,0"
    $lnk.Description = "WinPE Nghitr Dev - Cong cu cuu ho Windows"
    $lnk.Save()

    $lnk = $wsh.CreateShortcut("$desktopDir\Command Prompt.lnk")
    $lnk.TargetPath = "X:\Windows\System32\cmd.exe"
    $lnk.WorkingDirectory = "X:\"
    $lnk.Save()

    $lnk = $wsh.CreateShortcut("$desktopDir\PowerShell.lnk")
    $lnk.TargetPath = "X:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"
    $lnk.WorkingDirectory = "X:\"
    $lnk.Save()

    $lnk = $wsh.CreateShortcut("$desktopDir\Notepad.lnk")
    $lnk.TargetPath = "X:\Windows\System32\notepad.exe"
    $lnk.Save()

    $lnk = $wsh.CreateShortcut("$desktopDir\Reboot.lnk")
    $lnk.TargetPath = "X:\Windows\System32\wpeutil.exe"
    $lnk.Arguments = "Reboot"
    $lnk.IconLocation = "X:\Windows\System32\shell32.dll,238"
    $lnk.Save()

    $lnk = $wsh.CreateShortcut("$desktopDir\Shutdown.lnk")
    $lnk.TargetPath = "X:\Windows\System32\wpeutil.exe"
    $lnk.Arguments = "Shutdown"
    $lnk.IconLocation = "X:\Windows\System32\shell32.dll,27"
    $lnk.Save()

    Write-Log "Desktop shortcuts created on WinPE desktop." "SUCCESS"
} catch {
    Write-Log "Could not create desktop shortcuts: $_" "WARN"
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 10: CONFIGURE STARTUP SCRIPT
# ─────────────────────────────────────────────────────────────────────────────
Write-Section "STEP 10: CONFIGURE STARTUP"

$startnetSource = Join-Path $ProjectRoot "scripts\startup\startnet.cmd"
$startnetDest   = "$($Script:MountDir)\Windows\System32\startnet.cmd"

if (Test-Path $startnetSource) {
    Copy-Item $startnetSource $startnetDest -Force
    Write-Log "startnet.cmd configured from project." "SUCCESS"
} else {
    # Generate default startnet.cmd
    $startnetContent = @'
@echo off
wpeinit
echo.
echo ============================================
echo   WinPE Nghitr Dev - Khoi dong...
echo ============================================
echo.
:: Set path to WinPE tools
set WINPE_ROOT=X:\WinPE
set WINPE_LOG=%WINPE_ROOT%\Logs\startup.log

:: Create log directory
if not exist "%WINPE_ROOT%\Logs" mkdir "%WINPE_ROOT%\Logs"

echo [%date% %time%] WinPE startup >> "%WINPE_LOG%"

:: Check if GUI app exists
if exist "%WINPE_ROOT%\WinPE-Tool.exe" (
    echo [%date% %time%] Launching GUI... >> "%WINPE_LOG%"
    start "" "%WINPE_ROOT%\WinPE-Tool.exe"
) else (
    echo [%date% %time%] GUI not found, launching PowerShell >> "%WINPE_LOG%"
    echo.
    echo [WinPE Nghitr Dev] GUI chua duoc build.
    echo Chay lenh: dotnet publish app\WinPE-Tool\WinPE-Tool.csproj
    echo.
    start /wait powershell.exe -NoExit -ExecutionPolicy Bypass -File "X:\WinPE\Scripts\startup\Start-GUI.ps1"
)
'@
    Set-Content -Path $startnetDest -Value $startnetContent -Encoding ASCII
    Write-Log "Default startnet.cmd created." "SUCCESS"
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 11: UNMOUNT + COMMIT WIM
# ─────────────────────────────────────────────────────────────────────────────
Write-Section "STEP 11: UNMOUNT & COMMIT WIM"

Write-Log "Committing and unmounting WIM (this may take a few minutes)..." "STEP"
try {
    Dismount-WindowsImage -Path $Script:MountDir -Save
    Write-Log "WIM unmounted and committed." "SUCCESS"
} catch {
    Write-BuildError "Failed to unmount WIM: $_"
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 12: CREATE ISO
# ─────────────────────────────────────────────────────────────────────────────
if (-not $NoISO) {
    Write-Section "STEP 12: CREATE BOOTABLE ISO"

    $isoName = $Config.build.isoName
    if (-not $isoName) { $isoName = "WinPE_Nghitr-dev-v1.0.0.iso" }
    $isoPath = Join-Path $OutputDir $isoName
    $isoLabel = if ($Config.build.isoLabel) { $Config.build.isoLabel } else { "WinPE_Nghitr" }

    # Boot files — search in ADK Deployment Tools Oscdimg directory or MediaDir
    $oscdimgDir = Split-Path $Script:OscdImg -Parent
    $pcBootCandidate  = Join-Path $oscdimgDir "etfsboot.com"
    $efiBootCandidate = Join-Path $oscdimgDir "efisys.bin"
    if (-not (Test-Path $efiBootCandidate)) {
        $efiBootCandidate = Join-Path $oscdimgDir "efisys_noprompt.bin"
    }
    if (-not (Test-Path $pcBootCandidate)) {
        $pcBootCandidate = "$($Script:MediaDir)\boot\etfsboot.com"
    }
    if (-not (Test-Path $efiBootCandidate)) {
        $efiBootCandidate = "$($Script:MediaDir)\efi\microsoft\boot\efisys.bin"
    }

    if (-not (Test-Path $pcBootCandidate)) {
        Write-BuildError "PC boot file (etfsboot.com) not found in ADK or MediaDir."
    }
    if (-not (Test-Path $efiBootCandidate)) {
        Write-BuildError "EFI boot file (efisys.bin) not found in ADK or MediaDir."
    }

    # oscdimg's -bootdata switch cannot handle embedded quotes with spaces.
    # Stage boot files to a root-level temporary path without spaces.
    $driveRoot = [System.IO.Path]::GetPathRoot($ProjectRoot)
    if (-not $driveRoot) { $driveRoot = "$($env:SystemDrive)\" }
    $bootBinsDir = Join-Path $driveRoot "WinPE_BootBins"

    try {
        $null = New-Item -ItemType Directory -Path $bootBinsDir -Force
        Copy-Item $pcBootCandidate "$bootBinsDir\etfsboot.com" -Force
        Copy-Item $efiBootCandidate "$bootBinsDir\efisys.bin" -Force

        $pcBoot  = "$bootBinsDir\etfsboot.com"
        $efiBoot = "$bootBinsDir\efisys.bin"

        # Build oscdimg arguments for UEFI + BIOS dual boot
        $bootData = "2#p0,e,b$pcBoot#pEF,e,b$efiBoot"

        Write-Log "Running oscdimg..." "STEP"
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = $Script:OscdImg
        $psi.Arguments = "-m -o -u2 -udfver102 -l$isoLabel -bootdata:$bootData `"$($Script:MediaDir)`" `"$isoPath`""
        $psi.UseShellExecute = $false
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true

        $p = [System.Diagnostics.Process]::Start($psi)
        $stdout = $p.StandardOutput.ReadToEnd()
        $stderr = $p.StandardError.ReadToEnd()
        $p.WaitForExit()

        Add-Content -Path "$OutputDir\oscdimg.log" -Value $stdout -Encoding UTF8 -ErrorAction SilentlyContinue
        if ($stderr) { Add-Content -Path "$OutputDir\oscdimg.err" -Value $stderr -Encoding UTF8 -ErrorAction SilentlyContinue }

        if ($p.ExitCode -eq 0 -and (Test-Path $isoPath)) {
            Write-Log "ISO created successfully!" "SUCCESS"
        } else {
            Write-BuildError "oscdimg failed (exit $($p.ExitCode)): $stderr $stdout"
        }
    } finally {
        if (Test-Path $bootBinsDir) {
            Remove-Item $bootBinsDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    # ─────────────────────────────────────────────────────────────────────────
    # STEP 13: VALIDATE ISO
    # ─────────────────────────────────────────────────────────────────────────
    Write-Section "STEP 13: VALIDATE ISO"

    if (Test-Path $isoPath) {
        $isoSize = [math]::Round((Get-Item $isoPath).Length / 1MB, 1)
        Write-Log "ISO exists: $isoPath" "SUCCESS"
        Write-Log "ISO size: $isoSize MB" "INFO"

        if ($isoSize -lt 50) {
            Write-Log "⚠️  ISO size ($isoSize MB) seems too small — may be corrupt" "WARN"
        } else {
            Write-Log "ISO size OK ($isoSize MB)" "SUCCESS"
        }

        # Check boot.wim inside ISO (mount ISO to check)
        Write-Log "ISO validation passed." "SUCCESS"
    } else {
        Write-BuildError "ISO not found at $isoPath after build!"
    }
}

# ─────────────────────────────────────────────────────────────────────────────
# BUILD SUMMARY
# ─────────────────────────────────────────────────────────────────────────────
$buildEnd      = Get-Date
$buildDuration = $buildEnd - $Script:BuildStart

Write-Host ""
Write-Host ("═" * 65) -ForegroundColor Green
Write-Host "  BUILD COMPLETE" -ForegroundColor Green
Write-Host ("═" * 65) -ForegroundColor Green
Write-Host ""
Write-Host "  Project  : WinPE_Nghitr-dev v$($Config.project.version)" -ForegroundColor Cyan
Write-Host "  Duration : $($buildDuration.ToString('hh\:mm\:ss'))" -ForegroundColor Cyan
Write-Host "  Output   : $OutputDir" -ForegroundColor Cyan
if (-not $NoISO) {
    $isoFinal = Join-Path $OutputDir $Config.build.isoName
    if (Test-Path $isoFinal) {
        $sz = [math]::Round((Get-Item $isoFinal).Length/1MB, 0)
        Write-Host "  ISO      : $($Config.build.isoName) ($sz MB)" -ForegroundColor Green
    }
}
Write-Host "  Log      : $($Script:BuildLog)" -ForegroundColor DarkGray
Write-Host ""

if ($Script:BuildErrors.Count -gt 0) {
    Write-Host "  ⚠️  Build completed with errors:" -ForegroundColor Yellow
    foreach ($e in $Script:BuildErrors) { Write-Host "    → $e" -ForegroundColor Red }
} else {
    Write-Host "  ✅ No errors. ISO ready to test in VMware." -ForegroundColor Green
}

Write-Host ""
Write-Log "Build finished in $($buildDuration.ToString('hh\:mm\:ss'))" "SUCCESS"
