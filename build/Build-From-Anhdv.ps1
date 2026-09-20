# =============================================================================
# WinPE Nghitr Dev - Build From Anhdv Boot
# Tu dong tuy bien va Rebrand Anhdv Boot 26.2 thanh WinPE Nghitr Dev 2026
# =============================================================================

[CmdletBinding()]
param (
    [string]$SourceIso   = "C:\Users\phong\Downloads\Anhdv_Boot_Free_26.2\Anhdv_Boot_Free_26.2.iso",
    [string]$StagingDir  = "D:\WinPE_NghitrDev_Staging",
    [string]$OutputIso   = "",
    [string]$Wallpaper   = "",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path $PSScriptRoot -Parent

if (-not $OutputIso) {
    $OutputIso = Join-Path $ProjectRoot "output\WinPE_Nghitr_Dev_2026.iso"
}
if (-not $Wallpaper) {
    $Wallpaper = Join-Path $ProjectRoot "tools\WinXShell\wallpaper.jpg"
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

function Write-Warn($msg) {
    Write-Host "  [!] $msg" -ForegroundColor Yellow
}

# 1. Kiem tra file ISO nguon
Write-Step "BƯỚC 1: KIỂM TRA FILE ISO NGUỒN ANHDV BOOT"
if (-not (Test-Path $SourceIso)) {
    Write-Host "  [ERROR] Khong tim thay file ISO nguon tai: $SourceIso" -ForegroundColor Red
    exit 1
}
Write-Success "Da tim thay ISO: $SourceIso ($([math]::Round((Get-Item $SourceIso).Length / 1GB, 2)) GB)"

# 2. Kiem tra cong cu ADK oscdimg va 7-Zip
Write-Step "BƯỚC 2: KIỂM TRA CÔNG CỤ ĐÓNG GÓI"
$ADK_Candidates = @(
    "C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe",
    "C:\Program Files\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe"
)
$OscdImg = $ADK_Candidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $OscdImg) {
    Write-Host "  [ERROR] Khong tim thay oscdimg.exe trong Windows ADK." -ForegroundColor Red
    exit 1
}
Write-Success "oscdimg.exe: $OscdImg"

$7zCandidates = @(
    "C:\Users\phong\Downloads\Anhdv_Boot_Free_26.2\Tools\7z.exe",
    "C:\Program Files\7-Zip\7z.exe",
    "7z.exe"
)
$7zExe = $7zCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $7zExe) {
    Write-Host "  [ERROR] Khong tim thay 7z.exe." -ForegroundColor Red
    exit 1
}
Write-Success "7z.exe: $7zExe"

# 3. Trích xuất nội dung ISO vào thư mục Staging
Write-Step "BƯỚC 3: TRÍCH XUẤT NỘI DUNG ISO SANG STAGING"
$needsExtract = $Clean -or (-not (Test-Path "$StagingDir\WIM\w11pe64.wim")) -or (-not (Test-Path "$StagingDir\Apps\ppApps"))

if ($needsExtract) {
    if (Test-Path $StagingDir) {
        Write-Info "Dang don sach thu muc staging cu..."
        Remove-Item $StagingDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    Write-Info "Dang trich xuat ISO vao $StagingDir (mat khoang 10-15 giay)..."
    & $7zExe x $SourceIso "-o$StagingDir" -aoa -y | Out-Null
    Write-Success "Trich xuat ISO thanh cong."
} else {
    Write-Success "Su dung du lieu staging co san tai: $StagingDir"
}

# 4. Cap nhat Wallpaper vao w11pe64.wim va w10pe32.wim
Write-Step "BƯỚC 4: TÙY BIẾN HÌNH NỀN DESKTOP (WALLPAPER)"
if (Test-Path $Wallpaper) {
    $tempWpDir = "D:\WinPE_Temp_Wallpaper"
    if (Test-Path $tempWpDir) { Remove-Item $tempWpDir -Recurse -Force }
    $wpDestDir = "$tempWpDir\Windows\Web\Wallpaper\Windows"
    $null = New-Item -ItemType Directory -Path $wpDestDir -Force
    Copy-Item $Wallpaper "$wpDestDir\img0.jpg" -Force

    $wims = @(
        "$StagingDir\WIM\w11pe64.wim",
        "$StagingDir\WIM\w10pe32.wim"
    )
    foreach ($wim in $wims) {
        if (Test-Path $wim) {
            $wimName = Split-Path $wim -Leaf
            Write-Info "Dang cap nhat hinh nen cho $wimName..."
            & $7zExe u $wim "$tempWpDir\Windows" | Out-Null
            Write-Success "Da cap nhat hinh nen moi vao $wimName"
        }
    }
    Remove-Item $tempWpDir -Recurse -Force -ErrorAction SilentlyContinue
} else {
    Write-Warn "Khong tim thay file wallpaper tai: $Wallpaper (bo qua)"
}

# 5. Cap nhat Menu Boot Grub2 & Grub4dos
Write-Step "BƯỚC 5: TÙY BIẾN MENU BOOT (REBRAND NGHITR DEV)"
$mainCfg = "$StagingDir\boot\grub\main.cfg"
if (Test-Path $mainCfg) {
    $cfgContent = Get-Content $mainCfg -Raw
    $cfgContent = $cfgContent -replace 'menuentry "\[1\] > WinPE"', 'menuentry "[1] > WinPE Nghitr Dev 2026 (Rescue Suite)"'
    Set-Content $mainCfg $cfgContent -Encoding UTF8
    Write-Success "Da cap nhat Grub2 menu: $mainCfg"
}

$menuLst = "$StagingDir\boot\grub\menu.lst"
if (Test-Path $menuLst) {
    $lstContent = Get-Content $menuLst -Raw
    $lstContent = $lstContent -replace 'title \[1\] > WinPE', 'title [1] > WinPE Nghitr Dev 2026'
    Set-Content $menuLst $lstContent -Encoding ASCII
    Write-Success "Da cap nhat Grub4dos menu: $menuLst"
}

# 6. Chuan bi Boot Sector Binaries khong chua dau cach
Write-Step "BƯỚC 6: CHUẨN BỊ BOOT SECTORS"
$bootBinDir = "D:\WinPE_BootBins"
$null = New-Item -ItemType Directory -Path $bootBinDir -Force

$etfsboot = "$StagingDir\boot\etfsboot.com"
$efisys   = "$StagingDir\efi\Microsoft\Boot\efisys.bin"

if ((-not (Test-Path $etfsboot)) -or (-not (Test-Path $efisys))) {
    Write-Host "  [ERROR] Khong tim thay boot sector etfsboot.com hoac efisys.bin" -ForegroundColor Red
    exit 1
}

$stagedEtfs = "$bootBinDir\etfsboot.com"
$stagedEfi  = "$bootBinDir\efisys.bin"
Copy-Item $etfsboot $stagedEtfs -Force
Copy-Item $efisys $stagedEfi -Force
Write-Success "Boot sectors da duoc stage tai: $bootBinDir"

# 7. Dong goi file ISO bang oscdimg (UEFI + Legacy BIOS)
Write-Step "BƯỚC 7: ĐÓNG GÓI FILE ISO BẰNG OSCDIMG"
$outputDir = Split-Path $OutputIso -Parent
if (-not (Test-Path $outputDir)) {
    $null = New-Item -ItemType Directory -Path $outputDir -Force
}

$bootData = "2#p0,e,b`"$stagedEtfs`"#pEF,e,b`"$stagedEfi`""
$oscdimgArgs = @(
    "-m",
    "-o",
    "-u2",
    "-udfver102",
    "-bootdata:$bootData",
    "-lWinPE_NghitrDev",
    "`"$StagingDir`"",
    "`"$OutputIso`""
)

Write-Info "Dang tao file ISO WinPE Nghitr Dev 2026..."
$cmdLine = "& `"$OscdImg`" " + ($oscdimgArgs -join " ")
Invoke-Expression $cmdLine

if (Test-Path $OutputIso) {
    $sizeMB = [math]::Round((Get-Item $OutputIso).Length / 1MB, 1)
    $sizeGB = [math]::Round((Get-Item $OutputIso).Length / 1GB, 2)
    Write-Step "HOÀN TẤT TẠO FILE ISO THÀNH CÔNG!"
    Write-Host ""
    Write-Host "  🎉 File ISO: $OutputIso" -ForegroundColor Green
    Write-Host "  📦 Dung lượng: $sizeMB MB ($sizeGB GB)" -ForegroundColor Green
    Write-Host "  🛡️ Đầy đủ phần mềm: Partition Wizard, TrueImage, Ghost, Dism++, CPU-Z..." -ForegroundColor Green
    Write-Host "  ⚡ Hỗ trợ boot: Cả UEFI (x64/x86) và Legacy BIOS" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "  [ERROR] Khong the tao file ISO." -ForegroundColor Red
    exit 1
}
