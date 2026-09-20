<#
.SYNOPSIS
    Kiểm tra toàn bộ môi trường build WinPE_Nghitr-dev
.DESCRIPTION
    Script kiểm tra tất cả prerequisite trước khi build WinPE ISO:
    - Windows ADK + WinPE Add-on
    - PowerShell version
    - .NET Framework
    - Build tools (oscdimg, dism)
    - Disk space
    - Git
    - VMware (optional)
.NOTES
    Chạy bằng PowerShell với quyền Administrator
    Run: .\build\Get-Environment.ps1
#>

param(
    [switch]$Detailed,
    [switch]$ExportReport
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

# ─────────────────────────────────────────────
# Config
# ─────────────────────────────────────────────
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$ADK_Root    = "C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit"
$WinPE_Root  = "$ADK_Root\Windows Preinstallation Environment"
$OscdImg     = "$ADK_Root\Deployment Tools\amd64\Oscdimg\oscdimg.exe"
$ADK_DISM    = "$ADK_Root\Deployment Tools\amd64\DISM\dism.exe"
$WinPE_WIM   = "$WinPE_Root\amd64\en-us\winpe.wim"
$WinPE_OCs   = "$WinPE_Root\amd64\WinPE_OCs"
$WinPE_Media = "$WinPE_Root\amd64\Media"

$Results = @()
$Warnings = @()
$Errors = @()

# ─────────────────────────────────────────────
# Helpers
# ─────────────────────────────────────────────
function Write-Header($text) {
    Write-Host ""
    Write-Host ("=" * 60) -ForegroundColor DarkCyan
    Write-Host "  $text" -ForegroundColor Cyan
    Write-Host ("=" * 60) -ForegroundColor DarkCyan
}

function Write-Check($label, $status, $detail = "", $level = "OK") {
    $icon = switch ($level) {
        "OK"   { "✅" }
        "WARN" { "⚠️ " }
        "FAIL" { "❌" }
        "INFO" { "ℹ️ " }
    }
    $color = switch ($level) {
        "OK"   { "Green" }
        "WARN" { "Yellow" }
        "FAIL" { "Red" }
        "INFO" { "Cyan" }
    }
    $msg = "$icon  $label"
    if ($detail) { $msg += " — $detail" }
    Write-Host $msg -ForegroundColor $color

    $script:Results += [PSCustomObject]@{
        Label  = $label
        Status = $level
        Detail = $detail
    }
    if ($level -eq "WARN") { $script:Warnings += $label }
    if ($level -eq "FAIL") { $script:Errors += $label }
}

# ─────────────────────────────────────────────
# SECTION 1: System
# ─────────────────────────────────────────────
Write-Header "1. SYSTEM"

$os = Get-WmiObject Win32_OperatingSystem
$osVer = "$($os.Caption) Build $($os.BuildNumber) ($($os.OSArchitecture))"
Write-Check "Operating System" "OK" $osVer "INFO"

$psVer = $PSVersionTable.PSVersion.ToString()
$psOK = $PSVersionTable.PSVersion.Major -ge 5
Write-Check "PowerShell" ($psOK ? "OK" : "WARN") "v$psVer" ($psOK ? "OK" : "WARN")

# Admin check
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
Write-Check "Administrator Rights" ($isAdmin ? "OK" : "FAIL") ($isAdmin ? "Running as Admin" : "NOT running as Admin — some checks may fail") ($isAdmin ? "OK" : "FAIL")

# ─────────────────────────────────────────────
# SECTION 2: .NET
# ─────────────────────────────────────────────
Write-Header "2. .NET FRAMEWORK"

$net48 = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -ErrorAction SilentlyContinue
if ($net48 -and $net48.Release -ge 528040) {
    Write-Check ".NET Framework 4.8+" "OK" "v$($net48.Version) (Release $($net48.Release))" "OK"
} elseif ($net48) {
    Write-Check ".NET Framework 4.x" "WARN" "v$($net48.Version) — Khuyến nghị 4.8+" "WARN"
} else {
    Write-Check ".NET Framework 4.8" "FAIL" "Không tìm thấy — cần cho WinForms GUI" "FAIL"
}

# ─────────────────────────────────────────────
# SECTION 3: Windows ADK
# ─────────────────────────────────────────────
Write-Header "3. WINDOWS ADK"

if (Test-Path $ADK_Root) {
    Write-Check "ADK Root" "OK" $ADK_Root "OK"
} else {
    Write-Check "ADK Root" "FAIL" "Không tìm thấy tại $ADK_Root" "FAIL"
    Write-Host "    → Cài từ: https://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install" -ForegroundColor Yellow
}

# ADK version từ registry
$adkReg = Get-ItemProperty "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows Kits\Installed Roots" -ErrorAction SilentlyContinue
if ($adkReg -and $adkReg.KitsRoot10) {
    Write-Check "ADK Registry" "OK" "KitsRoot10 = $($adkReg.KitsRoot10)" "OK"
}

if (Test-Path $OscdImg) {
    $oscdVer = (Get-Item $OscdImg).VersionInfo.FileVersion
    Write-Check "oscdimg.exe" "OK" "v$oscdVer — ISO creation ready" "OK"
} else {
    Write-Check "oscdimg.exe" "FAIL" "Không tìm thấy tại $OscdImg" "FAIL"
}

if (Test-Path $ADK_DISM) {
    Write-Check "ADK DISM" "OK" $ADK_DISM "OK"
} else {
    Write-Check "ADK DISM" "WARN" "Dùng system DISM" "WARN"
}

# Deployment Tools env
$deployEnv = "$ADK_Root\Deployment Tools\DandISetEnv.bat"
if (Test-Path $deployEnv) {
    Write-Check "Deployment Tools Env" "OK" "DandISetEnv.bat found" "OK"
} else {
    Write-Check "Deployment Tools Env" "WARN" "DandISetEnv.bat not found" "WARN"
}

# ─────────────────────────────────────────────
# SECTION 4: WinPE Add-on
# ─────────────────────────────────────────────
Write-Header "4. WINPE ADD-ON"

if (Test-Path $WinPE_Root) {
    Write-Check "WinPE Add-on Root" "OK" $WinPE_Root "OK"
} else {
    Write-Check "WinPE Add-on Root" "FAIL" "Không tìm thấy — cài WinPE Add-on sau ADK" "FAIL"
}

if (Test-Path $WinPE_WIM) {
    $wimSize = [math]::Round((Get-Item $WinPE_WIM).Length / 1MB, 1)
    Write-Check "winpe.wim" "OK" "$wimSize MB" "OK"
} else {
    Write-Check "winpe.wim" "FAIL" "Không tìm thấy tại $WinPE_WIM" "FAIL"
}

if (Test-Path $WinPE_OCs) {
    $ocCount = (Get-ChildItem $WinPE_OCs -Filter "*.cab").Count
    Write-Check "WinPE OCs (Optional Components)" "OK" "$ocCount packages found" "OK"

    # Check critical OCs
    $criticalOCs = @("WinPE-WMI", "WinPE-NetFx", "WinPE-Scripting", "WinPE-PowerShell", "WinPE-StorageWMI", "WinPE-DismCmdlets")
    foreach ($oc in $criticalOCs) {
        $ocFile = "$WinPE_OCs\$oc.cab"
        if (Test-Path $ocFile) {
            Write-Check "  OC: $oc" "OK" "" "OK"
        } else {
            Write-Check "  OC: $oc" "FAIL" "Thiếu package cần thiết" "FAIL"
        }
    }
} else {
    Write-Check "WinPE OCs" "FAIL" "Không tìm thấy $WinPE_OCs" "FAIL"
}

if (Test-Path $WinPE_Media) {
    Write-Check "WinPE Media (boot files)" "OK" $WinPE_Media "OK"
} else {
    Write-Check "WinPE Media" "FAIL" "Không tìm thấy $WinPE_Media" "FAIL"
}

# ─────────────────────────────────────────────
# SECTION 5: Build Tools
# ─────────────────────────────────────────────
Write-Header "5. BUILD TOOLS"

$sysDism = Get-Command "dism.exe" -ErrorAction SilentlyContinue
Write-Check "dism.exe (system)" ($sysDism ? "OK" : "WARN") ($sysDism ? $sysDism.Source : "Not in PATH") ($sysDism ? "OK" : "WARN")

$sysBcdboot = Get-Command "bcdboot.exe" -ErrorAction SilentlyContinue
Write-Check "bcdboot.exe" ($sysBcdboot ? "OK" : "WARN") ($sysBcdboot ? $sysBcdboot.Source : "Not found") ($sysBcdboot ? "OK" : "WARN")

$sysDiskpart = Get-Command "diskpart.exe" -ErrorAction SilentlyContinue
Write-Check "diskpart.exe" ($sysDiskpart ? "OK" : "FAIL") ($sysDiskpart ? $sysDiskpart.Source : "Not found") ($sysDiskpart ? "OK" : "FAIL")

$sysXcopy = Get-Command "xcopy.exe" -ErrorAction SilentlyContinue
Write-Check "xcopy.exe" ($sysXcopy ? "OK" : "WARN") "" ($sysXcopy ? "OK" : "WARN")

# ─────────────────────────────────────────────
# SECTION 6: Git
# ─────────────────────────────────────────────
Write-Header "6. GIT"

$git = Get-Command "git" -ErrorAction SilentlyContinue
if ($git) {
    $gitVer = (git --version 2>&1).ToString()
    Write-Check "Git" "OK" $gitVer "OK"
} else {
    Write-Check "Git" "WARN" "Không tìm thấy — không bắt buộc" "WARN"
}

# ─────────────────────────────────────────────
# SECTION 7: Disk Space
# ─────────────────────────────────────────────
Write-Header "7. DISK SPACE"

$drives = Get-PSDrive -PSProvider FileSystem | Where-Object { $_.Used -ne $null }
foreach ($drive in $drives) {
    $freeGB = [math]::Round($drive.Free / 1GB, 1)
    $usedGB = [math]::Round($drive.Used / 1GB, 1)
    $totalGB = [math]::Round(($drive.Free + $drive.Used) / 1GB, 1)
    $level = if ($freeGB -ge 10) { "OK" } elseif ($freeGB -ge 5) { "WARN" } else { "FAIL" }
    Write-Check "Drive $($drive.Name):" $level "$freeGB GB free / $totalGB GB total" $level
}

# ─────────────────────────────────────────────
# SECTION 8: VMware
# ─────────────────────────────────────────────
Write-Header "8. VIRTUALIZATION (Testing)"

$vmwarePaths = @(
    "C:\Program Files (x86)\VMware\VMware Workstation\vmware.exe",
    "C:\Program Files\VMware\VMware Workstation\vmware.exe",
    "C:\Program Files (x86)\VMware\VMware Player\vmplayer.exe",
    "C:\Program Files\VMware\VMware Player\vmplayer.exe"
)
$vmFound = $false
foreach ($v in $vmwarePaths) {
    if (Test-Path $v) {
        $vmVer = (Get-Item $v).VersionInfo.ProductVersion
        Write-Check "VMware" "OK" "v$vmVer at $v" "OK"
        $vmFound = $true
        break
    }
}
if (-not $vmFound) {
    Write-Check "VMware" "WARN" "Không tìm thấy — cần để test ISO" "WARN"
}

# ─────────────────────────────────────────────
# SECTION 9: Project Structure
# ─────────────────────────────────────────────
Write-Header "9. PROJECT STRUCTURE"

$requiredDirs = @("build", "source", "app", "scripts", "drivers", "config", "assets", "tests", "docs", "output", "tools")
foreach ($d in $requiredDirs) {
    $fullPath = Join-Path $ProjectRoot $d
    if (Test-Path $fullPath) {
        Write-Check "  /$d" "OK" "" "OK"
    } else {
        Write-Check "  /$d" "FAIL" "Thư mục chưa tồn tại" "FAIL"
    }
}

$requiredFiles = @(
    "config\winpe-config.json",
    "config\tools-manifest.json",
    "VERSION"
)
foreach ($f in $requiredFiles) {
    $fullPath = Join-Path $ProjectRoot $f
    if (Test-Path $fullPath) {
        Write-Check "  $f" "OK" "" "OK"
    } else {
        Write-Check "  $f" "FAIL" "File chưa tồn tại" "FAIL"
    }
}

# ─────────────────────────────────────────────
# SUMMARY
# ─────────────────────────────────────────────
Write-Header "SUMMARY"

$okCount   = ($Results | Where-Object { $_.Status -eq "OK" }).Count
$warnCount = $Warnings.Count
$failCount = $Errors.Count

Write-Host ""
Write-Host "  ✅ OK    : $okCount" -ForegroundColor Green
Write-Host "  ⚠️  WARN  : $warnCount" -ForegroundColor Yellow
Write-Host "  ❌ FAIL  : $failCount" -ForegroundColor Red
Write-Host ""

if ($failCount -eq 0) {
    Write-Host "  🚀 MÔI TRƯỜNG SẴN SÀNG — CÓ THỂ BUILD WINPE ISO" -ForegroundColor Green
} elseif ($failCount -le 2) {
    Write-Host "  ⚠️  CÓ $failCount LỖI CẦN SỬA TRƯỚC KHI BUILD" -ForegroundColor Yellow
} else {
    Write-Host "  ❌ MÔI TRƯỜNG CHƯA SẴN SÀNG — CÓ $failCount LỖI" -ForegroundColor Red
}

if ($Errors.Count -gt 0) {
    Write-Host ""
    Write-Host "  Lỗi cần xử lý:" -ForegroundColor Red
    foreach ($e in $Errors) { Write-Host "    → $e" -ForegroundColor Red }
}

if ($Warnings.Count -gt 0) {
    Write-Host ""
    Write-Host "  Cảnh báo:" -ForegroundColor Yellow
    foreach ($w in $Warnings) { Write-Host "    → $w" -ForegroundColor Yellow }
}

if ($ExportReport) {
    $reportPath = Join-Path $ProjectRoot "output\environment-report.txt"
    $Results | Format-Table -AutoSize | Out-File $reportPath -Encoding UTF8
    Write-Host ""
    Write-Host "  Report saved: $reportPath" -ForegroundColor Cyan
}

Write-Host ""
