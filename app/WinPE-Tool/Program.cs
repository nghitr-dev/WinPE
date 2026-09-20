using System;
using System.Windows.Forms;

namespace WinPETool
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string startPage = "dashboard";
            if (args != null && args.Length > 0)
            {
                startPage = args[0].TrimStart('-', '/').ToLowerInvariant();
            }

            // Global unhandled exception handler — không để crash toàn bộ app
            Application.ThreadException += (sender, e) =>
            {
                var result = MessageBox.Show(
                    $"Đã xảy ra lỗi không mong đợi:\n\n{e.Exception.Message}\n\n" +
                    "Bạn muốn tiếp tục chạy ứng dụng?",
                    "WinPE Nghitr Dev — Lỗi",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Error
                );
                if (result == DialogResult.No)
                    Application.Exit();
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show(
                    $"Lỗi nghiêm trọng:\n\n{ex?.Message ?? e.ExceptionObject.ToString()}",
                    "WinPE Nghitr Dev — Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            };

            // Load config và launch
            try
            {
                var config = AppConfig.Load();
                Application.Run(new MainForm(config, startPage));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Không thể khởi động ứng dụng:\n\n{ex.Message}",
                    "WinPE Nghitr Dev",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}
