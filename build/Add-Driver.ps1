<#
.SYNOPSIS
    Inject driver vào WinPE WIM đang mount
.PARAMETER DriverPath
    Đường dẫn đến file .inf hoặc thư mục chứa driver
.PARAMETER Category
    Category để thêm vào drivers\ (storage/network/usb/chipset/gpu/misc)
.EXAMPLE
    .\Add-Driver.ps1 -DriverPath "C:\Downloads\nvme-driver.inf"
    .\Add-Driver.ps1 -DriverPath "C:\Downloads\intel-network\" -Category network
#>
#Requires -RunAsAdministrator

param(
    [Parameter(Mandatory=$true)]
    [string]$DriverPath,
    [ValidateSet("storage","network","usb","chipset","gpu","misc")]
    [string]$Category = "misc",
    [switch]$AlsoBackup
)

$ProjectRoot = Split-Path $PSScriptRoot -Parent
$MountDir    = Join-Path $ProjectRoot "source\mount"
$DriversDir  = Join-Path $ProjectRoot "drivers\$Category"

Write-Host ""
Write-Host "=== Add Driver ===" -ForegroundColor Cyan

if (-not (Test-Path $DriverPath)) {
    Write-Host "❌ Driver not found: $DriverPath" -ForegroundColor Red
    exit 1
}

# Check WIM is mounted
$mounted = Get-WindowsImage -Mounted -ErrorAction SilentlyContinue
if (-not ($mounted | Where-Object { $_.MountPath -eq $MountDir })) {
    Write-Host "❌ WIM not mounted. Chạy Mount-WinPE.ps1 trước." -ForegroundColor Red
    exit 1
}

if ($AlsoBackup) {
    $null = New-Item -ItemType Directory -Path $DriversDir -Force
    Copy-Item $DriverPath $DriversDir -Recurse -Force
    Write-Host "✅ Driver backed up to: $DriversDir" -ForegroundColor Green
}

Write-Host "Injecting driver: $DriverPath" -ForegroundColor White
try {
    Add-WindowsDriver -Path $MountDir -Driver $DriverPath -Recurse -ForceUnsigned
    Write-Host "✅ Driver injected successfully." -ForegroundColor Green
    Write-Host "   Chạy Unmount-WinPE.ps1 để save." -ForegroundColor DarkGray
} catch {
    Write-Host "❌ Driver injection failed: $_" -ForegroundColor Red
    exit 1
}
