using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WinPETool.Core;
using WinPETool.UI;

namespace WinPETool.UI.Pages
{
    /// <summary>
    /// Backup & Restore Page
    /// Hỗ trợ: Sao lưu / Phục hồi Windows bằng WIM (DISM) và Sao lưu dữ liệu người dùng (Desktop, Docs, Downloads qua Robocopy)
    /// </summary>
    public class BackupPage : Panel
    {
        private readonly AppConfig _config;
        private RichTextBox _outputBox;
        private ProgressBar _progressBar;

        public BackupPage(AppConfig config)
        {
            _config = config;
            BackColor = Theme.BgDark;
            Dock = DockStyle.Fill;
            Padding = new Padding(16);
            AutoScroll = true;

            BuildUI();
        }

        private void BuildUI()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 48 };
            var bar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Theme.CatBackup };
            var lbl = new Label
            {
                Text = "📦  Sao lưu & Phục hồi (Backup & Restore WIM / Dữ liệu người dùng)",
                Font = Theme.FontTitle,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(14, 10)
            };
            header.Controls.Add(bar);
            header.Controls.Add(lbl);
            Controls.Add(header);

            // Cards flow layout
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 10, 0, 10),
                BackColor = Theme.BgDark
            };

            // Card 1: Capture WIM (Full OS Backup)
            var cardCapture = MakeCard(
                "Sao lưu hệ điều hành (Capture WIM)",
                "Tạo bản sao lưu toàn bộ ổ cài Windows thành file install.wim / backup.wim nén cao.",
                "Tạo file WIM",
                () => CaptureWim()
            );

            // Card 2: Apply WIM (Restore OS)
            var cardApply = MakeCard(
                "Phục hồi hệ điều hành (Apply WIM)",
                "Bung file WIM/ESD/SWM vào phân vùng đích để cài hoặc khôi phục Windows.",
                "Bung file WIM",
                () => ApplyWim()
            );

            // Card 3: Backup User Data (Robocopy)
            var cardUserData = MakeCard(
                "Sao lưu dữ liệu cá nhân (Desktop/Docs)",
                "Tự động quét và sao chép Desktop, Documents, Downloads của người dùng ra ổ cứng ngoài.",
                "Sao lưu tài liệu",
                () => BackupUserData()
            );

            // Card 4: DISM Check Image Health
            var cardCheckWim = MakeCard(
                "Kiểm tra thông tin file WIM",
                "Xem danh sách editions, kích thước, kiến trúc và build number bên trong file .wim",
                "Xem thông tin WIM",
                () => CheckWimInfo()
            );

            flow.Controls.AddRange(new Control[] { cardCapture, cardApply, cardUserData, cardCheckWim });
            Controls.Add(flow);

            // Output / Log section
            var outputHeader = new Label
            {
                Text = "Tiến trình sao lưu / phục hồi:",
                Font = Theme.FontHeader,
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(0, 8, 0, 0)
            };
            Controls.Add(outputHeader);

            _progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 6,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 0
            };
            Controls.Add(_progressBar);

            _outputBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontMonoSm,
                ReadOnly = true,
                BorderStyle = BorderStyle.None
            };
            Controls.Add(_outputBox);

            header.SendToBack();
            flow.SendToBack();
            outputHeader.SendToBack();
            _progressBar.SendToBack();
            _outputBox.BringToFront();
        }

        private Panel MakeCard(string title, string desc, string btnText, Action onClick)
        {
            var card = new Panel
            {
                Width = 280,
                Height = 160,
                BackColor = Theme.BgCard,
                Margin = new Padding(0, 0, 12, 12),
                Padding = new Padding(12)
            };

            var topBar = new Panel { Dock = DockStyle.Top, Height = 3, BackColor = Theme.CatBackup };
            var lblTitle = new Label
            {
                Text = title,
                Font = Theme.FontHeader,
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 24
            };
            var lblDesc = new Label
            {
                Text = desc,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Fill
            };
            var btn = new Button
            {
                Text = btnText,
                Font = Theme.FontButton,
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.AccentDark,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Bottom,
                Height = 32,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Theme.Accent;
            btn.Click += (s, e) => onClick();

            card.Controls.AddRange(new Control[] { lblDesc, btn, lblTitle, topBar });
            return card;
        }

        private void CaptureWim()
        {
            using var fbd = new FolderBrowserDialog { Description = "Chọn ổ đĩa chứa Windows cần sao lưu (thường là C:\\)" };
            if (fbd.ShowDialog() != DialogResult.OK) return;
            var srcDir = fbd.SelectedPath;

            using var sfd = new SaveFileDialog
            {
                Title = "Chọn nơi lưu file backup .wim",
                Filter = "WIM File (*.wim)|*.wim",
                FileName = $"Windows_Backup_{DateTime.Now:yyyyMMdd}.wim"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            var destWim = sfd.FileName;

            AppendLog($"\n[BẮT ĐẦU] Tạo file WIM từ '{srcDir}' sang '{destWim}'...", Theme.TextAccent);
            SetBusy(true);

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                var cmd = $"dism.exe /Capture-Image /ImageFile:\"{destWim}\" /CaptureDir:\"{srcDir}\" /Name:\"Windows Backup\" /Compress:max";
                var result = ProcessRunner.Run("cmd.exe", $"/c {cmd}");

                Invoke(new Action(() =>
                {
                    SetBusy(false);
                    if (result.Success)
                    {
                        AppendLog(result.Output, Theme.TextSuccess);
                        AppendLog($"✅ Tạo file backup WIM thành công: {destWim}", Theme.TextSuccess);
                        MessageBox.Show($"Sao lưu WIM thành công!\n{destWim}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        AppendLog(result.Output, Theme.TextError);
                        AppendLog($"❌ Lỗi tạo WIM (Exit: {result.ExitCode})", Theme.TextError);
                    }
                }));
            });
        }

        private void ApplyWim()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Chọn file WIM cần bung",
                Filter = "WIM File (*.wim;*.esd;*.swm)|*.wim;*.esd;*.swm|Tất cả file (*.*)|*.*"
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            var wimFile = ofd.FileName;

            using var fbd = new FolderBrowserDialog { Description = "Chọn phân vùng đích để bung Windows vào (CẨN THẬN: Dữ liệu phân vùng này sẽ bị ghi đè)" };
            if (fbd.ShowDialog() != DialogResult.OK) return;
            var destDir = fbd.SelectedPath;

            if (MessageBox.Show($"Bạn có chắc chắn muốn bung file:\n{wimFile}\nvào phân vùng:\n{destDir}?", "Xác nhận khôi phục", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            AppendLog($"\n[BẮT ĐẦU] Bung WIM '{wimFile}' vào '{destDir}'...", Theme.TextAccent);
            SetBusy(true);

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                var cmd = $"dism.exe /Apply-Image /ImageFile:\"{wimFile}\" /Index:1 /ApplyDir:\"{destDir}\"";
                var result = ProcessRunner.Run("cmd.exe", $"/c {cmd}");

                Invoke(new Action(() =>
                {
                    SetBusy(false);
                    if (result.Success)
                    {
                        AppendLog(result.Output, Theme.TextSuccess);
                        AppendLog($"✅ Phục hồi WIM vào {destDir} thành công!", Theme.TextSuccess);
                        MessageBox.Show($"Phục hồi WIM thành công!\nPhân vùng: {destDir}\n\nLưu ý: Nếu cần, hãy dùng trang 'Cứu hộ Windows' để chạy BCD Repair (bcdboot {destDir}\\Windows).", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        AppendLog(result.Output, Theme.TextError);
                        AppendLog($"❌ Lỗi bung WIM: {result.Error}", Theme.TextError);
                    }
                }));
            });
        }

        private void BackupUserData()
        {
            var installs = WindowsFinder.FindInstallations();
            string userDir = @"C:\Users";
            if (installs.Count > 0)
                userDir = Path.Combine(installs[0].SystemDrive, "Users");

            using var fbd = new FolderBrowserDialog { Description = "Chọn thư mục lưu dữ liệu người dùng (ổ USB hoặc ổ cứng gắn ngoài)" };
            if (fbd.ShowDialog() != DialogResult.OK) return;
            var destDir = fbd.SelectedPath;

            AppendLog($"\n[BẮT ĐẦU] Sao lưu dữ liệu người dùng từ '{userDir}' sang '{destDir}' bằng Robocopy...", Theme.TextAccent);
            SetBusy(true);

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                // Robocopy desktop, docs, downloads, pictures
                var target = Path.Combine(destDir, $"UserBackup_{DateTime.Now:yyyyMMdd_HHmmss}");
                var cmd = $"robocopy.exe \"{userDir}\" \"{target}\" /E /R:1 /W:1 /XD \"AppData\" \"Local Settings\" \"Application Data\" /XF \"*.tmp\" \"*.sys\" \"ntuser.dat*\"";
                var result = ProcessRunner.Run("cmd.exe", $"/c {cmd}");

                Invoke(new Action(() =>
                {
                    SetBusy(false);
                    // Robocopy returns codes 0-7 for success/files copied
                    if (result.ExitCode <= 7)
                    {
                        AppendLog(result.Output, Theme.TextSuccess);
                        AppendLog($"✅ Sao lưu dữ liệu cá nhân thành công tại:\n{target}", Theme.TextSuccess);
                        MessageBox.Show($"Sao lưu dữ liệu người dùng thành công!\n{target}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        AppendLog(result.Output, Theme.TextError);
                        AppendLog($"⚠️ Robocopy hoàn thành với mã: {result.ExitCode}", Theme.TextWarning);
                    }
                }));
            });
        }

        private void CheckWimInfo()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Chọn file WIM cần kiểm tra",
                Filter = "WIM File (*.wim;*.esd)|*.wim;*.esd|Tất cả (*.*)|*.*"
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            var wimFile = ofd.FileName;

            AppendLog($"\n[KIỂM TRA] Đọc thông tin file: {wimFile}...", Theme.TextAccent);
            SetBusy(true);

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                var cmd = $"dism.exe /Get-ImageInfo /ImageFile:\"{wimFile}\"";
                var result = ProcessRunner.Run("cmd.exe", $"/c {cmd}");

                Invoke(new Action(() =>
                {
                    SetBusy(false);
                    if (result.Success)
                    {
                        AppendLog(result.Output, Theme.TextSuccess);
                    }
                    else
                    {
                        AppendLog(result.Output, Theme.TextError);
                    }
                }));
            });
        }

        private void SetBusy(bool busy)
        {
            _progressBar.MarqueeAnimationSpeed = busy ? 30 : 0;
            _progressBar.Style = busy ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
        }

        private void AppendLog(string text, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;
            _outputBox.SelectionStart = _outputBox.TextLength;
            _outputBox.SelectionColor = color;
            _outputBox.AppendText(text + Environment.NewLine);
            _outputBox.ScrollToCaret();
        }
    }
}
