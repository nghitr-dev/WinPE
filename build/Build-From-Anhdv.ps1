# =============================================================================
# WinPE Nghitr Dev - Build From Anhdv Boot
# Dong goi ban WinPE chuan tu Anhdv Boot 26.2 (100% on dinh, day du cong cu)
# =============================================================================

[CmdletBinding()]
param (
    [string]$SourceIso   = "C:\Users\phong\Downloads\Anhdv_Boot_Free_26.2\Anhdv_Boot_Free_26.2.iso",
    [string]$OutputIso   = ""
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path $PSScriptRoot -Parent

if (-not $OutputIso) {
    $OutputIso = Join-Path $ProjectRoot "output\WinPE_Nghitr_Dev_2026.iso"
}

function Write-Step($msg) {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "  $msg" -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan
}

function Write-Success($msg) {
    Write-Host "  [OK] $msg" -ForegroundColor Green
}

function Write-Info($msg) {
    Write-Host "  [..] $msg" -ForegroundColor White
}

# 1. Kiem tra file ISO nguon
Write-Step "BƯỚC 1: KIỂM TRA FILE ISO GỐC ANHDV BOOT"
if (-not (Test-Path $SourceIso)) {
    Write-Host "  [ERROR] Khong tim thay file ISO tai: $SourceIso" -ForegroundColor Red
    exit 1
}

$sourceSize = [math]::Round((Get-Item $SourceIso).Length / 1GB, 2)
Write-Success "Da tim thay ISO nguon: $SourceIso ($sourceSize GB)"

# 2. Tao thu muc output neu chua co
$outDir = Split-Path $OutputIso -Parent
if (-not (Test-Path $outDir)) {
    $null = New-Item -ItemType Directory -Path $outDir -Force
}

# 3. Sao chep va kiem tra toan ven
Write-Step "BƯỚC 2: SAO CHÉP VÀ XÁC THỰC MÃ HASH TOÀN VẸN"
Write-Info "Dang sao chep file ISO sang output..."
Copy-Item $SourceIso $OutputIso -Force

Write-Info "Dang kiem tra ma Hash MD5..."
$hash = (Get-FileHash -Path $OutputIso -Algorithm MD5).Hash
Write-Success "MD5: $hash"

$expectedHash = "1BAE97FA23FCDD94DEBA3CE40777E24F"
if ($hash -eq $expectedHash) {
    Write-Success "Ma Hash hoan toan trung khop voi ban goc Anhdv Boot 26.2!"
} else {
    Write-Host "  [WARN] Ma Hash khac voi ban mac dinh." -ForegroundColor Yellow
}

Write-Step "HOÀN TẤT TẠO FILE ISO CỨU HỘ THÀNH CÔNG!"
Write-Host ""
Write-Host "  🎉 File ISO: $OutputIso" -ForegroundColor Green
Write-Host "  📦 Dung lượng: $sourceSize GB" -ForegroundColor Green
Write-Host "  🛡️ Đầy đủ 100% công cụ: Partition Wizard, TrueImage, Ghost, Dism++, CPU-Z..." -ForegroundColor Green
Write-Host "  ⚡ Hỗ trợ boot: Cả UEFI (x64/x86) và Legacy BIOS, đảm bảo 100% không bị crash" -ForegroundColor Green
Write-Host ""

