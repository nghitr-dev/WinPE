<#
.SYNOPSIS
    Unmount WinPE WIM và commit thay đổi
.PARAMETER Discard
    Discard thay đổi (không save)
#>
#Requires -RunAsAdministrator

param([switch]$Discard)

$ProjectRoot = Split-Path $PSScriptRoot -Parent
$MountDir    = Join-Path $ProjectRoot "source\mount"

Write-Host ""
Write-Host "=== Unmount WinPE WIM ===" -ForegroundColor Cyan

$mounted = Get-WindowsImage -Mounted -ErrorAction SilentlyContinue
if (-not ($mounted | Where-Object { $_.MountPath -eq $MountDir })) {
    Write-Host "ℹ️  No WIM mounted at $MountDir" -ForegroundColor Yellow
    exit 0
}

if ($Discard) {
    Write-Host "⚠️  Discarding changes..." -ForegroundColor Yellow
    Dismount-WindowsImage -Path $MountDir -Discard
    Write-Host "✅ WIM unmounted (discarded)." -ForegroundColor Green
} else {
    Write-Host "Saving and unmounting (this may take a few minutes)..." -ForegroundColor White
    Dismount-WindowsImage -Path $MountDir -Save
    Write-Host "✅ WIM unmounted and saved." -ForegroundColor Green
}
