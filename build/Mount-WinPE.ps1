<#
.SYNOPSIS
    Mount boot.wim để chỉnh sửa thủ công
.DESCRIPTION
    Mount WIM từ source\media\sources\boot.wim vào source\mount
    Sau khi chỉnh xong, chạy Unmount-WinPE.ps1 để commit
.PARAMETER Discard
    Mount để xem, không commit thay đổi khi unmount
#>
#Requires -RunAsAdministrator

param([switch]$Discard)

$ProjectRoot = Split-Path $PSScriptRoot -Parent
$WimPath     = Join-Path $ProjectRoot "source\media\sources\boot.wim"
$MountDir    = Join-Path $ProjectRoot "source\mount"

Write-Host ""
Write-Host "=== Mount WinPE WIM ===" -ForegroundColor Cyan

if (-not (Test-Path $WimPath)) {
    Write-Host "❌ boot.wim not found: $WimPath" -ForegroundColor Red
    Write-Host "   Chạy Build-WinPE.ps1 trước để copy WIM từ ADK." -ForegroundColor Yellow
    exit 1
}

# Check if already mounted
$mounted = Get-WindowsImage -Mounted -ErrorAction SilentlyContinue
if ($mounted | Where-Object { $_.MountPath -eq $MountDir }) {
    Write-Host "⚠️  WIM already mounted at $MountDir" -ForegroundColor Yellow
    exit 0
}

$null = New-Item -ItemType Directory -Path $MountDir -Force

Write-Host "Mounting: $WimPath" -ForegroundColor White
Write-Host "     → $MountDir" -ForegroundColor White

try {
    Mount-WindowsImage -ImagePath $WimPath -Index 1 -Path $MountDir
    Write-Host ""
    Write-Host "✅ WIM mounted at: $MountDir" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Thư mục WinPE: $MountDir\WinPE" -ForegroundColor Cyan
    Write-Host "  Khi xong: chạy .\Unmount-WinPE.ps1 $(if($Discard){'-Discard'})" -ForegroundColor DarkGray
} catch {
    Write-Host "❌ Mount failed: $_" -ForegroundColor Red
    exit 1
}
