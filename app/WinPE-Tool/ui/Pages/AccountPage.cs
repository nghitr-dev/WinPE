using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WinPETool.Core;
using WinPETool.UI;

namespace WinPETool.UI.Pages
{
    /// <summary>
    /// Windows Account & Password Recovery Page
    /// Hỗ trợ:
    /// 1. Thay thế Utilman.exe / Sethc.exe bằng CMD để mở CMD quyền SYSTEM tại màn hình Login Windows
    /// 2. Khôi phục lại Utilman.exe / Sethc.exe nguyên bản
    /// 3. Hướng dẫn lệnh net user để đổi mật khẩu hoặc kích hoạt tài khoản Administrator
    /// </summary>
    public class AccountPage : Panel
    {
        private readonly AppConfig _config;
        private ComboBox _targetWindowsCombo;
        private RichTextBox _outputBox;

        public AccountPage(AppConfig config)
        {
            _config = config;
            BackColor = Theme.BgDark;
            Dock = DockStyle.Fill;
            Padding = new Padding(16);
            AutoScroll = true;

            BuildUI();
            RefreshWindowsTargets();
        }

        private void BuildUI()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 48 };
            var bar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Theme.CatSecurity };
            var lbl = new Label
            {
                Text = "👤  Khôi phục tài khoản & Mật khẩu Windows (Account Recovery)",
                Font = Theme.FontTitle,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(14, 10)
            };
            header.Controls.Add(bar);
            header.Controls.Add(lbl);
            Controls.Add(header);

            // Windows target selector
            var targetPanel = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Theme.BgToolbar, Padding = new Padding(12, 10, 12, 10) };
            var lblTarget = new Label { Text = "Bản Windows mục tiêu:", ForeColor = Theme.TextSecondary, Font = Theme.FontBody, AutoSize = true, Location = new Point(12, 14) };
            _targetWindowsCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontBody,
                Width = 280,
                Location = new Point(170, 10)
            };
            var btnRefresh = MakeBtn("🔄 Quét lại", () => RefreshWindowsTargets(), Theme.BgCard);
            btnRefresh.Location = new Point(460, 9);

            targetPanel.Controls.AddRange(new Control[] { lblTarget, _targetWindowsCombo, btnRefresh });
            Controls.Add(targetPanel);

            // Info note
            var note = new Label
            {
                Text = "ℹ️  Tính năng này can thiệp vào file hệ thống Windows offline. Khi khởi động lại vào Windows thật, bấm icon Ease of Access hoặc phím Shift 5 lần tại màn hình đăng nhập để mở Command Prompt quản trị.",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(8, 6, 0, 0),
                BackColor = Theme.BgToolbar
            };
            Controls.Add(note);

            // Cards flow layout
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 12, 0, 12),
                BackColor = Theme.BgDark
            };

            // Card 1: Enable Utilman Backdoor
            var cardUtilman = MakeCard(
                "Kích hoạt CMD tại màn hình khóa (Utilman)",
                "Đổi tên utilman.exe thành utilman.bak và copy cmd.exe thế chỗ. Tại màn hình khóa Windows, bấm icon Accessibility sẽ hiện CMD SYSTEM.",
                "Kích hoạt (Utilman)",
                () => ApplyUtilmanBypass()
            );

            // Card 2: Restore Utilman
            var cardRestoreUtilman = MakeCard(
                "Khôi phục Utilman.exe gốc",
                "Phục hồi lại file utilman.exe gốc của Windows sau khi đã đổi mật khẩu xong.",
                "Khôi phục Utilman",
                () => RestoreUtilman()
            );

            // Card 3: Enable Sethc Backdoor
            var cardSethc = MakeCard(
                "Kích hoạt CMD bằng phím Shift (Sethc)",
                "Đổi sethc.exe (Sticky Keys) thành CMD. Tại màn hình đăng nhập, bấm phím Shift 5 lần liên tục để mở CMD SYSTEM.",
                "Kích hoạt (Sethc)",
                () => ApplySethcBypass()
            );

            // Card 4: Restore Sethc
            var cardRestoreSethc = MakeCard(
                "Khôi phục Sethc.exe gốc",
                "Phục hồi file sethc.exe nguyên bản của Windows sau khi đã xử lý xong.",
                "Khôi phục Sethc",
                () => RestoreSethc()
            );

            flow.Controls.AddRange(new Control[] { cardUtilman, cardRestoreUtilman, cardSethc, cardRestoreSethc });
            Controls.Add(flow);

            // Command helper box
            var helperPanel = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Theme.BgCard, Padding = new Padding(12), Margin = new Padding(0, 0, 0, 12) };
            var helperTitle = new Label { Text = "📖  Các câu lệnh đổi mật khẩu khi mở được CMD tại màn hình khóa:", Font = Theme.FontHeader, ForeColor = Theme.TextPrimary, Dock = DockStyle.Top, Height = 22 };
            var helperText = new Label
            {
                Text = "• Xem danh sách user: net user\n" +
                       "• Đổi mật khẩu mới: net user [TênUser] [MậtKhẩuMới]   (ví dụ: net user Administrator 123456)\n" +
                       "• Kích hoạt tài khoản bị khóa: net user Administrator /active:yes",
                Font = Theme.FontMonoSm,
                ForeColor = Theme.Accent,
                Dock = DockStyle.Fill
            };
            helperPanel.Controls.Add(helperText);
            helperPanel.Controls.Add(helperTitle);
            Controls.Add(helperPanel);

            // Output box
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
            targetPanel.SendToBack();
            note.SendToBack();
            flow.SendToBack();
            helperPanel.SendToBack();
            _outputBox.BringToFront();
        }

        private Panel MakeCard(string title, string desc, string btnText, Action onClick)
        {
            var card = new Panel
            {
                Width = 280,
                Height = 165,
                BackColor = Theme.BgCard,
                Margin = new Padding(0, 0, 12, 12),
                Padding = new Padding(12)
            };

            var topBar = new Panel { Dock = DockStyle.Top, Height = 3, BackColor = Theme.CatSecurity };
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

        private void RefreshWindowsTargets()
        {
            _targetWindowsCombo.Items.Clear();
            var installs = WindowsFinder.FindInstallations();
            foreach (var inst in installs)
            {
                _targetWindowsCombo.Items.Add($"{inst.SystemDrive} ({inst.Edition}) - {inst.WindowsDirectory}");
            }
            if (_targetWindowsCombo.Items.Count > 0)
            {
                _targetWindowsCombo.SelectedIndex = 0;
            }
            else
            {
                _targetWindowsCombo.Items.Add(@"C:\Windows (Mặc định)");
                _targetWindowsCombo.SelectedIndex = 0;
            }
        }

        private string GetSelectedSystem32()
        {
            var selected = _targetWindowsCombo.SelectedItem as string;
            string winDir = @"C:\Windows";

            var installs = WindowsFinder.FindInstallations();
            foreach (var inst in installs)
            {
                if (selected != null && selected.Contains(inst.SystemDrive))
                {
                    winDir = inst.WindowsDirectory;
                    break;
                }
            }

            return Path.Combine(winDir, "System32");
        }

        private void ApplyUtilmanBypass()
        {
            var s32 = GetSelectedSystem32();
            var utilman = Path.Combine(s32, "utilman.exe");
            var utilmanBak = Path.Combine(s32, "utilman.exe.bak");
            var cmd = Path.Combine(s32, "cmd.exe");

            if (!File.Exists(cmd))
            {
                MessageBox.Show($"Không tìm thấy cmd.exe tại:\n{cmd}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                if (!File.Exists(utilmanBak) && File.Exists(utilman))
                {
                    File.Move(utilman, utilmanBak);
                    AppendLog($"Đã đổi tên: {utilman} -> utilman.exe.bak", Theme.TextSecondary);
                }

                File.Copy(cmd, utilman, true);
                AppendLog("✅ Đã thế chỗ utilman.exe bằng cmd.exe thành công!", Theme.TextSuccess);
                AppendLog("➡ Hướng dẫn: Khởi động lại máy vào Windows. Tại màn hình khóa, bấm biểu tượng Ease of Access ở góc dưới phải để mở CMD quyền SYSTEM.", Theme.TextAccent);
                MessageBox.Show("Đã kích hoạt thành công!\n\nKhởi động lại máy vào Windows thật, tại màn hình khóa bấm biểu tượng Trợ năng (Accessibility / Ease of Access) để mở Command Prompt quyền SYSTEM.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi: {ex.Message}", Theme.TextError);
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestoreUtilman()
        {
            var s32 = GetSelectedSystem32();
            var utilman = Path.Combine(s32, "utilman.exe");
            var utilmanBak = Path.Combine(s32, "utilman.exe.bak");

            if (!File.Exists(utilmanBak))
            {
                MessageBox.Show($"Không tìm thấy file sao lưu gốc:\n{utilmanBak}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (File.Exists(utilman)) File.Delete(utilman);
                File.Move(utilmanBak, utilman);
                AppendLog("✅ Đã khôi phục utilman.exe nguyên bản thành công!", Theme.TextSuccess);
                MessageBox.Show("Đã khôi phục utilman.exe gốc thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi khôi phục: {ex.Message}", Theme.TextError);
            }
        }

        private void ApplySethcBypass()
        {
            var s32 = GetSelectedSystem32();
            var sethc = Path.Combine(s32, "sethc.exe");
            var sethcBak = Path.Combine(s32, "sethc.exe.bak");
            var cmd = Path.Combine(s32, "cmd.exe");

            if (!File.Exists(cmd))
            {
                MessageBox.Show($"Không tìm thấy cmd.exe tại:\n{cmd}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                if (!File.Exists(sethcBak) && File.Exists(sethc))
                {
                    File.Move(sethc, sethcBak);
                    AppendLog($"Đã đổi tên: {sethc} -> sethc.exe.bak", Theme.TextSecondary);
                }

                File.Copy(cmd, sethc, true);
                AppendLog("✅ Đã thế chỗ sethc.exe bằng cmd.exe thành công!", Theme.TextSuccess);
                AppendLog("➡ Hướng dẫn: Khởi động lại máy vào Windows. Tại màn hình đăng nhập, bấm phím SHIFT liên tục 5 lần để mở CMD quyền SYSTEM.", Theme.TextAccent);
                MessageBox.Show("Đã kích hoạt thành công!\n\nKhởi động lại máy vào Windows thật, tại màn hình đăng nhập bấm phím Shift 5 lần liên tục để mở CMD.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi: {ex.Message}", Theme.TextError);
            }
        }

        private void RestoreSethc()
        {
            var s32 = GetSelectedSystem32();
            var sethc = Path.Combine(s32, "sethc.exe");
            var sethcBak = Path.Combine(s32, "sethc.exe.bak");

            if (!File.Exists(sethcBak))
            {
                MessageBox.Show($"Không tìm thấy file sao lưu gốc:\n{sethcBak}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (File.Exists(sethc)) File.Delete(sethc);
                File.Move(sethcBak, sethc);
                AppendLog("✅ Đã khôi phục sethc.exe nguyên bản thành công!", Theme.TextSuccess);
                MessageBox.Show("Đã khôi phục sethc.exe gốc thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi khôi phục: {ex.Message}", Theme.TextError);
            }
        }

        private void AppendLog(string text, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;
            _outputBox.SelectionStart = _outputBox.TextLength;
            _outputBox.SelectionColor = color;
            _outputBox.AppendText(text + Environment.NewLine);
            _outputBox.ScrollToCaret();
        }

        private Button MakeBtn(string text, Action click, Color bg)
        {
            var btn = new Button
            {
                Text = text,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextPrimary,
                BackColor = bg,
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                Padding = new Padding(6, 2, 6, 2),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Theme.Border;
            btn.Click += (s, e) => click();
            return btn;
        }
    }
}
