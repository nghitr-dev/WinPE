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
                "recovery"    => new PlaceholderPage("🔧 Phục hồi Windows", "Module đang phát triển...", Theme.CatRecovery),
                "bcd"         => new PlaceholderPage("💾 Sửa BCD / Bootloader", "Module đang phát triển...", Theme.CatRecovery),
                "disk"        => new PlaceholderPage("💿 Quản lý ổ đĩa", "Module đang phát triển...", Theme.CatDisk),
                "filemanager" => new PlaceholderPage("📁 File Manager", "Module đang phát triển...", Theme.CatFiles),
                "backup"      => new PlaceholderPage("📦 Backup & Restore", "Module đang phát triển...", Theme.CatBackup),
                "hardware"    => new PlaceholderPage("🖥️ Phần cứng", "Module đang phát triển...", Theme.CatHardware),
                "drivers"     => new PlaceholderPage("🔌 Driver", "Module đang phát triển...", Theme.CatDrivers),
                "network"     => new PlaceholderPage("🌐 Mạng", "Module đang phát triển...", Theme.CatNetwork),
                "security"    => new PlaceholderPage("🛡️ Bảo mật", "Module đang phát triển...", Theme.CatSecurity),
                "account"     => new PlaceholderPage("👤 Tài khoản", "Module đang phát triển...", Theme.CatSecurity),
                "systemtools" => new PlaceholderPage("🔩 Công cụ hệ thống", "Module đang phát triển...", Theme.CatSystem),
                "remote"      => new PlaceholderPage("📡 Hỗ trợ từ xa", "Module đang phát triển...", Theme.CatNetwork),
                "logs"        => new PlaceholderPage("📋 Logs", "Module đang phát triển...", Theme.CatSettings),
                "settings"    => new PlaceholderPage("⚙️ Cài đặt", "Module đang phát triển...", Theme.CatSettings),
                _             => new DashboardPage(config),
            };
        }
    }
}
