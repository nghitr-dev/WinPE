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
                "filemanager" => new FileManagerPage(config),
                "backup"      => new BackupPage(config),
                "hardware"    => new HardwarePage(config),
                "drivers"     => new DriverPage(config),
                "network"     => new NetworkPage(config),
                "security"    => new PlaceholderPage("🛡️ Bảo mật / Quét virus", "Optional tools slot — thêm portable antivirus scanner vào tools/.", Theme.CatSecurity),
                "account"     => new AccountPage(config),
                "systemtools" => new SystemToolsPage(config),
                "remote"      => new PlaceholderPage("📡 Hỗ trợ từ xa", "Thêm TeamViewer / AnyDesk portable vào thư mục tools/ để kích hoạt.", Theme.CatNetwork),
                "logs"        => new LogsPage(config),
                "settings"    => new PlaceholderPage("⚙️ Cài đặt", "Cấu hình giao diện và thông số WinPE.", Theme.CatSettings),
                _             => new DashboardPage(config),
            };
        }
    }
}
