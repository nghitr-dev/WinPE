# WinPE Nghitr Dev

> **WinPE ISO tùy chỉnh** — Công cụ cứu hộ và bảo trì Windows  
> Thiết kế bởi **Nghitr.dev** | v1.0.0

[![Build Status](https://img.shields.io/badge/build-in_development-yellow)]()
[![WinPE](https://img.shields.io/badge/WinPE-amd64-blue)]()
[![.NET](https://img.shields.io/badge/.NET-Framework_4.8-purple)]()
[![License](https://img.shields.io/badge/license-MIT-green)]()

---

## 📋 Tổng quan

**WinPE_Nghitr-dev** là một Windows PE ISO do tôi tự xây dựng, bao gồm:

- ✅ GUI riêng (WinForms .NET 4.8 — dark theme)
- ✅ Bộ công cụ cứu hộ Windows
- ✅ Phát hiện Windows installations (không giả định C:)
- ✅ Quản lý disk/partition
- ✅ File Manager offline
- ✅ Hardware diagnostics
- ✅ Driver management
- ✅ Backup & Restore
- ✅ Network tools
- ✅ Log system
- ✅ Build system tự động (PowerShell)
- ✅ UEFI + Legacy BIOS boot

---

## 🗂 Cấu trúc project

```
WINPE/
├── build/                    # Build scripts
│   ├── Build-WinPE.ps1       # Main build (cần Admin)
│   ├── Clean-Build.ps1       # Dọn working directory
│   ├── Mount-WinPE.ps1       # Mount boot.wim thủ công
│   ├── Unmount-WinPE.ps1     # Unmount + commit
│   ├── Add-Driver.ps1        # Inject driver
│   ├── Create-ISO.ps1        # Tạo ISO standalone
│   └── Get-Environment.ps1   # Kiểm tra môi trường
├── app/                      # GUI Application (C# WinForms)
│   └── WinPE-Tool/
│       ├── core/             # Logger, Config, ProcessRunner, WindowsFinder
│       ├── ui/               # Theme, Pages, Controls
│       └── modules/          # Feature modules
├── scripts/                  # PowerShell scripts chạy trong WinPE
│   ├── startup/              # startnet.cmd, Start-GUI.ps1
│   ├── recovery/             # BCD, SFC, DISM scripts
│   ├── disk/                 # Disk/partition scripts
│   ├── network/              # Network scripts
│   ├── hardware/             # Hardware info scripts
│   ├── backup/               # Backup/restore scripts
│   └── drivers/              # Driver export/import scripts
├── drivers/                  # Driver repository
│   ├── storage/              # NVMe, SATA
│   ├── network/              # LAN, Wi-Fi
│   ├── usb/
│   ├── chipset/
│   ├── gpu/
│   └── misc/
├── config/
│   ├── winpe-config.json     # Cấu hình chính
│   └── tools-manifest.json   # Danh sách tools/plugins
├── assets/                   # Wallpaper, logo, icons
├── tools/                    # Optional portable tools
├── tests/                    # Test scripts
├── docs/                     # Documentation
└── output/                   # ISO output
```

---

## ⚙️ Requirements

### Bắt buộc
| Requirement | Version | Notes |
|------------|---------|-------|
| Windows 10/11 | 64-bit | Máy build |
| PowerShell | 5.1+ hoặc 7.x | Build scripts |
| **Windows ADK** | 10.1.26100+ | Deployment Tools |
| **WinPE Add-on** | Cùng version ADK | WinPE base + OCs |
| .NET Framework | 4.8 | GUI build |
| Git | Any | Version control |

### Download ADK
1. **Windows ADK**: https://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install
   - Chọn: ✅ **Deployment Tools** only
2. **WinPE Add-on**: Tải từ cùng trang, cài sau ADK

### Để test ISO
- VMware Workstation / Player, hoặc
- Hyper-V (Windows 11 Pro)

---

## 🚀 Hướng dẫn Build

### Bước 1: Kiểm tra môi trường
```powershell
# Chạy PowerShell (không cần Admin)
.\build\Get-Environment.ps1
```
Phải thấy tất cả ✅ trước khi build.

### Bước 2: Build GUI App
```powershell
# Cần .NET Framework 4.8 SDK
cd app\WinPE-Tool
dotnet build -c Release
# hoặc dùng Visual Studio
```

### Bước 3: Build WinPE ISO
```powershell
# Chạy PowerShell với quyền Administrator
.\build\Build-WinPE.ps1

# Options:
.\build\Build-WinPE.ps1 -Clean          # Xóa working dir trước
.\build\Build-WinPE.ps1 -SkipDrivers   # Bỏ qua driver injection
.\build\Build-WinPE.ps1 -SkipApp       # Bỏ qua GUI app
.\build\Build-WinPE.ps1 -NoISO         # Build WIM nhưng không tạo ISO
```

### Bước 4: Kết quả
```
output/
  WinPE_Nghitr-dev-v1.0.0.iso   ← Boot trực tiếp bằng VMware/USB
  build.log                      ← Build log
```

---

## 💿 Thêm Drivers

Đặt file `.inf` (và các file đi kèm) vào thư mục phù hợp:

```
drivers/
  storage/     ← NVMe Intel/Samsung, SATA driver
  network/     ← LAN Realtek, Intel, Wi-Fi driver
  usb/         ← USB 3.x driver
  chipset/     ← Intel/AMD chipset
  gpu/         ← Display adapter
  misc/        ← Khác
```

Build script sẽ tự động inject tất cả `.inf` tìm thấy.

---

## 🔧 Thêm Tools

1. Copy portable tool vào `tools/<tên-tool>/`
2. Thêm entry vào `config/tools-manifest.json`:

```json
{
  "id": "mytool",
  "name": "My Tool",
  "nameVi": "Công cụ của tôi",
  "category": "SystemTools",
  "executable": "tools\\mytool\\mytool.exe",
  "enabled": true
}
```

---

## ⚙️ Cấu hình

Chỉnh `config/winpe-config.json`:

```json
{
  "project": {
    "name": "WinPE_Nghitr-dev",
    "version": "1.0.0"
  },
  "branding": {
    "windowTitle": "WinPE Nghitr Dev",
    "primaryColor": "#1A2744",
    "accentColor": "#00AAFF"
  }
}
```

---

## 🧪 Test Checklist

```
[ ] Get-Environment.ps1 — tất cả PASS
[ ] Build-WinPE.ps1 — exit 0
[ ] ISO tạo ra trong output/
[ ] ISO size > 200MB
[ ] VMware boot thành công
[ ] GUI khởi động
[ ] Dashboard hiển thị CPU/RAM
[ ] Windows detection hoạt động
[ ] CMD/PowerShell mở được
[ ] Disk info hiển thị
[ ] Network info hiển thị
[ ] Log ghi ra file
[ ] USB boot (nếu có hardware)
```

---

## ⚠️ Safety Warnings

- **Các thao tác phá hủy dữ liệu** (Format, Delete partition, Reset password) đều có confirmation dialog
- **Không tự động sửa** Windows installation — người dùng phải chủ động chọn
- **Driver injection**: Chỉ dùng driver do bạn tự cung cấp — không tự tải từ Internet
- **Backup dữ liệu quan trọng** trước khi dùng bất kỳ recovery tool nào

---

## 🗺 Roadmap

### v1.0.0 (Current)
- [x] Project structure
- [x] Build system (Build-WinPE.ps1)
- [x] GUI framework (MainForm + Theme + Dashboard)
- [x] Logger, Config, ProcessRunner
- [x] Windows detection
- [ ] Windows Recovery module
- [ ] Disk Management module
- [ ] File Manager module
- [ ] Hardware Diagnostics module
- [ ] Network module
- [ ] Driver module
- [ ] Backup module
- [ ] Full ISO build + test

### v1.5.0
- [ ] Advanced diagnostics
- [ ] Remote support (TeamViewer portable slot)
- [ ] Malware scan (optional tools slot)
- [ ] Advanced backup/restore
- [ ] Plugin system

### v2.0.0
- [ ] Linux EXT3/EXT4 read support
- [ ] macOS APFS/HFS+ read-only
- [ ] PXE boot support
- [ ] Network deployment
- [ ] Automated repair workflows

---

## 📝 Changelog

### v1.0.0 (2026-09-20)
- Initial project structure
- Build system (Build-WinPE.ps1, Clean-Build.ps1, Mount-WinPE.ps1, etc.)
- GUI framework: MainForm, Theme, Dashboard, PageFactory
- Core: Logger, AppConfig, ProcessRunner, WindowsFinder
- Config system (winpe-config.json, tools-manifest.json)
- Startup scripts (startnet.cmd, Start-GUI.ps1)
- WinPE packages configuration

---

## 📄 License

MIT License — Personal use.  
Không tích hợp phần mềm crack, keygen hoặc vượt license.

---

*Built from scratch by Nghitr — not based on any existing WinPE project.*
