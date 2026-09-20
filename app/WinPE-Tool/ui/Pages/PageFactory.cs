using System.Windows.Forms;
using WinPETool.Core;
using WinPETool.UI;

namespace WinPETool.UI.Pages
{
    /// <summary>
    /// Factory tạo page panels theo page ID
    /// Thêm page mới: chỉ cần thêm case vào switch
    /// </summary>
    public static class PageFactory
    {
        public static Panel Create(string pageId, AppConfig config)
        {
            return pageId switch
            {
                "dashboard"   => new DashboardPage(config),
                "recovery"    => new RecoveryPage(config),
                "bcd"         => new RecoveryPage(config),          // BCD tab within Recovery
                "disk"        => new DiskPage(config),
                "filemanager" => new PlaceholderPage("📁 File Manager", "Coming in v1.1 — sẽ cho phép browse/copy/move files offline.", Theme.CatFiles),
                "backup"      => new PlaceholderPage("📦 Backup & Restore", "Coming in v1.1", Theme.CatBackup),
                "hardware"    => new HardwarePage(config),
                "drivers"     => new PlaceholderPage("🔌 Driver Management", "Coming in v1.1 — export/import/inject drivers.", Theme.CatDrivers),
                "network"     => new NetworkPage(config),
                "security"    => new PlaceholderPage("🛡️ Bảo mật / Quét virus", "Optional tools slot — thêm portable scanner vào tools/.", Theme.CatSecurity),
                "account"     => new PlaceholderPage("👤 Tài khoản Windows", "Coming in v1.1 — local account management.", Theme.CatSecurity),
                "systemtools" => new SystemToolsPage(config),
                "remote"      => new PlaceholderPage("📡 Hỗ trợ từ xa", "Coming in v1.5 — TeamViewer/AnyDesk portable slot.", Theme.CatNetwork),
                "logs"        => new LogsPage(config),
                "settings"    => new PlaceholderPage("⚙️ Cài đặt", "Coming soon.", Theme.CatSettings),
                _             => new DashboardPage(config),
            };
        }
    }
}
