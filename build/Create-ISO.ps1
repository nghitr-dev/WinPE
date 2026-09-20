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
$ADK_Root    = "C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit"
$OscdImg     = "$ADK_Root\Deployment Tools\amd64\Oscdimg\oscdimg.exe"

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

$efiBoot = "$MediaDir\efi\microsoft\boot\efisys.bin"
$pcBoot  = "$MediaDir\boot\etfsboot.com"

if (-not (Test-Path $efiBoot)) { Write-Host "❌ EFI boot file missing: $efiBoot" -ForegroundColor Red; exit 1 }
if (-not (Test-Path $pcBoot))  { Write-Host "❌ PC boot file missing: $pcBoot" -ForegroundColor Red; exit 1 }

Write-Host "Creating: $IsoName" -ForegroundColor White
Write-Host "Label:    $IsoLabel" -ForegroundColor White
Write-Host "From:     $MediaDir" -ForegroundColor White
Write-Host ""

# Dual boot: BIOS (etfsboot) + UEFI (efisys)
$bootData = "2#p0,e,b`"$pcBoot`"#pEF,e,b`"$efiBoot`""

$args = @("-m", "-o", "-u2", "-udfver102", "-l$IsoLabel", "-bootdata:$bootData", "`"$MediaDir`"", "`"$IsoPath`"")

$proc = Start-Process -FilePath $OscdImg -ArgumentList $args -Wait -PassThru -NoNewWindow
if ($proc.ExitCode -eq 0) {
    $sz = [math]::Round((Get-Item $IsoPath).Length/1MB, 0)
    Write-Host "✅ ISO created: $IsoPath ($sz MB)" -ForegroundColor Green
} else {
    Write-Host "❌ oscdimg failed (exit $($proc.ExitCode))" -ForegroundColor Red
    exit 1
}
