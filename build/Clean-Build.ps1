<#
.SYNOPSIS
    Dọn sạch build artifacts của WinPE_Nghitr-dev
.DESCRIPTION
    Xóa: working dir, mount dir, media dir, ISO cũ
    KHÔNG xóa: drivers, app source, config, assets
.PARAMETER All
    Xóa cả output ISO cũ
.PARAMETER Force
    Không hỏi confirm
#>
#Requires -RunAsAdministrator

param([switch]$All, [switch]$Force)

$ProjectRoot = Split-Path $PSScriptRoot -Parent
$WorkDir     = Join-Path $ProjectRoot "source\working"
$MountDir    = Join-Path $ProjectRoot "source\mount"
$MediaDir    = Join-Path $ProjectRoot "source\media"

Write-Host ""
Write-Host "=== Clean Build — WinPE_Nghitr-dev ===" -ForegroundColor Cyan

if (-not $Force) {
    $confirm = Read-Host "Xóa build artifacts? (y/N)"
    if ($confirm -notmatch '^[Yy]') { Write-Host "Cancelled."; exit 0 }
}

# Check if WIM is mounted
$mounted = Get-WindowsImage -Mounted -ErrorAction SilentlyContinue
if ($mounted | Where-Object { $_.MountPath -eq $MountDir }) {
    Write-Host "⚠️  WIM still mounted at $MountDir — discarding..." -ForegroundColor Yellow
    Dismount-WindowsImage -Path $MountDir -Discard -ErrorAction SilentlyContinue
    Write-Host "✅ WIM discarded." -ForegroundColor Green
}

$toDelete = @($WorkDir, $MountDir, $MediaDir)
if ($All) {
    Write-Host "⚠️  --All: also removing output ISOs" -ForegroundColor Yellow
    $toDelete += Get-ChildItem (Join-Path $ProjectRoot "output") -Filter "*.iso" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
}

foreach ($path in $toDelete) {
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "✅ Removed: $path" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "✅ Clean complete." -ForegroundColor Green
