<#
.SYNOPSIS
    Tạo ISO từ media directory (standalone, không cần build full)
.DESCRIPTION
    Dùng sau khi đã có source\media hoàn chỉnh
    Gọi oscdimg để tạo UEFI+BIOS dual bootable ISO
.PARAMETER OutputDir
    Thư mục output
.PARAMETER IsoName
    Tên file ISO
#>
#Requires -RunAsAdministrator

param(
    [string]$OutputDir = "",
    [string]$IsoName   = ""
)

$ProjectRoot = Split-Path $PSScriptRoot -Parent
$ConfigFile  = Join-Path $ProjectRoot "config\winpe-config.json"
$MediaDir    = Join-Path $ProjectRoot "source\media"

function Find-ADK {
    $regKey = "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows Kits\Installed Roots"
    $reg = Get-ItemProperty $regKey -ErrorAction SilentlyContinue
    if ($reg -and $reg.KitsRoot10 -and (Test-Path $reg.KitsRoot10)) {
        $adkBase = $reg.KitsRoot10.TrimEnd('\')
        return "$adkBase\Assessment and Deployment Kit"
    }
    $candidates = @(
        "C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit",
        "C:\Program Files\Windows Kits\10\Assessment and Deployment Kit"
    )
    foreach ($c in $candidates) {
        if (Test-Path $c) { return $c }
    }
    return $null
}

$ADK_Root = Find-ADK
if (-not $ADK_Root) {
    Write-Host "❌ Windows ADK not found. Please install ADK Deployment Tools." -ForegroundColor Red
    exit 1
}
$OscdImg = "$ADK_Root\Deployment Tools\amd64\Oscdimg\oscdimg.exe"

Write-Host ""
Write-Host "=== Create ISO ===" -ForegroundColor Cyan

# Load config
$Config = Get-Content $ConfigFile -Raw -Encoding UTF8 | ConvertFrom-Json
if (-not $OutputDir) { $OutputDir = Join-Path $ProjectRoot $Config.build.outputDir }
if (-not $IsoName)   { $IsoName   = $Config.build.isoName }
$IsoLabel = $Config.build.isoLabel

$null = New-Item -ItemType Directory -Path $OutputDir -Force
$IsoPath = Join-Path $OutputDir $IsoName

if (-not (Test-Path $MediaDir)) {
    Write-Host "❌ Media directory not found: $MediaDir" -ForegroundColor Red
    Write-Host "   Chạy Build-WinPE.ps1 trước." -ForegroundColor Yellow
    exit 1
}
if (-not (Test-Path $OscdImg)) {
    Write-Host "❌ oscdimg not found: $OscdImg" -ForegroundColor Red
    exit 1
}

# Locate boot bins from ADK Oscdimg directory or MediaDir
$oscdimgDir = Split-Path $OscdImg -Parent
$pcBootCandidate  = Join-Path $oscdimgDir "etfsboot.com"
$efiBootCandidate = Join-Path $oscdimgDir "efisys.bin"
if (-not (Test-Path $efiBootCandidate)) {
    $efiBootCandidate = Join-Path $oscdimgDir "efisys_noprompt.bin"
}
if (-not (Test-Path $pcBootCandidate)) {
    $pcBootCandidate = "$MediaDir\boot\etfsboot.com"
}
if (-not (Test-Path $efiBootCandidate)) {
    $efiBootCandidate = "$MediaDir\efi\microsoft\boot\efisys.bin"
}

if (-not (Test-Path $pcBootCandidate))  { Write-Host "❌ PC boot file missing: $pcBootCandidate" -ForegroundColor Red; exit 1 }
if (-not (Test-Path $efiBootCandidate)) { Write-Host "❌ EFI boot file missing: $efiBootCandidate" -ForegroundColor Red; exit 1 }

# oscdimg's -bootdata switch cannot handle embedded quotes with spaces.
# Stage boot files to a root-level temporary path without spaces.
$driveRoot = [System.IO.Path]::GetPathRoot($ProjectRoot)
if (-not $driveRoot) { $driveRoot = "$($env:SystemDrive)\" }
$bootBinsDir = Join-Path $driveRoot "WinPE_BootBins"

try {
    $null = New-Item -ItemType Directory -Path $bootBinsDir -Force
    Copy-Item $pcBootCandidate "$bootBinsDir\etfsboot.com" -Force
    Copy-Item $efiBootCandidate "$bootBinsDir\efisys.bin" -Force

    $pcBoot = "$bootBinsDir\etfsboot.com"
    $efiBoot = "$bootBinsDir\efisys.bin"

    Write-Host "Creating: $IsoName" -ForegroundColor White
    Write-Host "Label:    $IsoLabel" -ForegroundColor White
    Write-Host "From:     $MediaDir" -ForegroundColor White
    Write-Host "Target:   $IsoPath" -ForegroundColor White
    Write-Host ""

    # Dual boot: BIOS (etfsboot) + UEFI (efisys)
    $bootData = "2#p0,e,b$pcBoot#pEF,e,b$efiBoot"

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $OscdImg
    $psi.Arguments = "-m -o -u2 -udfver102 -l$IsoLabel -bootdata:$bootData `"$MediaDir`" `"$IsoPath`""
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true

    $p = [System.Diagnostics.Process]::Start($psi)
    $stdout = $p.StandardOutput.ReadToEnd()
    $stderr = $p.StandardError.ReadToEnd()
    $p.WaitForExit()

    if ($p.ExitCode -eq 0 -and (Test-Path $IsoPath)) {
        $sz = [math]::Round((Get-Item $IsoPath).Length/1MB, 0)
        Write-Host $stdout
        Write-Host "✅ ISO created: $IsoPath ($sz MB)" -ForegroundColor Green
    } else {
        Write-Host "❌ oscdimg failed (exit $($p.ExitCode))" -ForegroundColor Red
        if ($stdout) { Write-Host $stdout -ForegroundColor Yellow }
        if ($stderr) { Write-Host $stderr -ForegroundColor Red }
        exit 1
    }
} finally {
    if (Test-Path $bootBinsDir) {
        Remove-Item $bootBinsDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
