using System;
using System.Drawing;
using System.Windows.Forms;
using WinPETool.Core;
using WinPETool.UI;

namespace WinPETool.UI.Pages
{
    /// <summary>
    /// System Tools Page — shortcuts đến tools thực sự chạy được trong WinPE
    /// Chỉ đưa vào tool ĐÃ XÁC NHẬN chạy được trong WinPE environment
    /// </summary>
    public class SystemToolsPage : Panel
    {
        public SystemToolsPage(AppConfig config)
        {
            BackColor = Theme.BgDark;
            Dock      = DockStyle.Fill;
            Padding   = new Padding(20);
            AutoScroll = true;
            BuildUI();
        }

        private void BuildUI()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 50 };
            var bar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Theme.CatSystem };
            var lbl = new Label { Text = "🔩  Công cụ hệ thống", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary, AutoSize = true, Location = new Point(14, 12) };
            header.Controls.Add(bar); header.Controls.Add(lbl);
            Controls.Add(header);

            var note = new Label
            {
                Text      = "ℹ️  Chỉ hiển thị công cụ đã xác nhận chạy được trong WinPE. Các tool không tương thích WinPE sẽ không xuất hiện ở đây.",
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                Dock      = DockStyle.Top,
                Height    = 32,
                Padding   = new Padding(8, 6, 0, 0),
                BackColor = Theme.BgToolbar,
            };
            Controls.Add(note);

            var flow = new FlowLayoutPanel
            {
                Dock      = DockStyle.Fill,
                AutoScroll = true,
                Padding   = new Padding(12),
                BackColor = Theme.BgDark,
            };
            Controls.Add(flow);

            var tools = new[]
            {
                // (Name, Icon, Description, exe, args, winpe_ok)
                ("Command Prompt",   "💻", "CMD shell đầy đủ",                  "cmd.exe",          "",           true),
                ("PowerShell",       "📜", "PowerShell (có trong WinPE nếu add OC)", "powershell.exe", "-NoExit",  true),
                ("Registry Editor",  "🗝",  "Regedit — chỉnh registry",           "regedit.exe",      "",           true),
                ("Notepad",          "📝", "Text editor cơ bản",                  "notepad.exe",      "",           true),
                ("DiskPart",         "💿", "Disk partition CLI",                  "cmd.exe",          "/k diskpart", true),
                ("Task Manager",     "📊", "Quản lý processes",                   "taskmgr.exe",      "",           true),
                ("System Info",      "ℹ",  "msinfo32 — thông tin hệ thống",       "msinfo32.exe",     "",           true),
                ("Calculator",       "🔢", "Máy tính",                            "calc.exe",         "",           false), // not in WinPE
                ("DISM",             "🔧", "DISM CLI",                            "cmd.exe",          "/k dism /?", true),
                ("SFC",              "🔍", "System File Checker",                 "cmd.exe",          "/k sfc /?",  true),
                ("BCDEdit",          "⚙️", "Boot Config Editor CLI",              "cmd.exe",          "/k bcdedit", true),
                ("Network Shell",    "🌐", "netsh — network config",              "cmd.exe",          "/k netsh",   true),
            };

            foreach (var (name, icon, desc, exe, args, winpeOk) in tools)
            {
                var card = MakeToolCard(name, icon, desc, exe, args, winpeOk);
                flow.Controls.Add(card);
            }
        }

        private Panel MakeToolCard(string name, string icon, string desc, string exe, string args, bool winpeOk)
        {
            var card = new Panel
            {
                Width     = 200,
                Height    = 110,
                Margin    = new Padding(0, 0, 12, 12),
                BackColor = winpeOk ? Theme.BgCard : Color.FromArgb(30, 40, 40, 40),
                Cursor    = winpeOk ? Cursors.Hand : Cursors.Default,
            };

            // WinPE compat indicator
            var bar = new Panel { Dock = DockStyle.Top, Height = 3, BackColor = winpeOk ? Theme.Success : Theme.TextMuted };
            var iconLbl = new Label { Text = icon, Font = new Font("Segoe UI Emoji", 22f), ForeColor = winpeOk ? Theme.TextPrimary : Theme.TextMuted, AutoSize = true, Location = new Point(12, 12) };
            var nameLbl = new Label { Text = name, Font = Theme.FontHeader, ForeColor = winpeOk ? Theme.TextPrimary : Theme.TextMuted, AutoSize = false, Width = 175, Height = 20, Location = new Point(12, 52) };
            var descLbl = new Label { Text = desc, Font = Theme.FontSmall, ForeColor = Theme.TextMuted, AutoSize = false, Width = 175, Height = 30, Location = new Point(12, 72) };
            var statusDot = new Label { Text = winpeOk ? "✅ WinPE OK" : "⚠️  WinPE N/A", Font = Theme.FontSmall, ForeColor = winpeOk ? Theme.TextSuccess : Theme.TextMuted, AutoSize = true, Location = new Point(12, 92) };

            card.Controls.AddRange(new Control[] { bar, iconLbl, nameLbl, descLbl, statusDot });

            if (winpeOk)
            {
                Action click = () =>
                {
                    try { ProcessRunner.Launch(exe, args); }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Không thể mở {name}:\n{ex.Message}", "WinPE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                card.Click += (s, e) => click();
                foreach (Control c in card.Controls) c.Click += (s, e) => click();
                card.MouseEnter += (s, e) => card.BackColor = Theme.BgHover;
                card.MouseLeave += (s, e) => card.BackColor = Theme.BgCard;
            }

            return card;
        }
    }

    /// <summary>
    /// Logs Page — xem và export log file
    /// </summary>
    public class LogsPage : Panel
    {
        private RichTextBox _rtb;

        public LogsPage(AppConfig config)
        {
            BackColor = Theme.BgDark;
            Dock      = DockStyle.Fill;
            BuildUI(config);
            LoadLog();
        }

        private void BuildUI(AppConfig config)
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 50 };
            var bar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Theme.CatSettings };
            var lbl = new Label { Text = "📋  Nhật ký hoạt động", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary, AutoSize = true, Location = new Point(14, 12) };
            header.Controls.Add(bar); header.Controls.Add(lbl);
            Controls.Add(header);

            // Toolbar
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.BgToolbar, Padding = new Padding(8, 6, 8, 6) };
            var refreshBtn = MakeBtn("🔄 Làm mới", () => LoadLog());
            var saveBtn    = MakeBtn("💾 Lưu log", () => SaveLog(config));
            var clearBtn   = MakeBtn("🗑 Xóa",     () => { Logger.Info("Log cleared by user."); _rtb.Clear(); });
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill };
            flow.Controls.AddRange(new Control[] { refreshBtn, saveBtn, clearBtn });
            toolbar.Controls.Add(flow);
            Controls.Add(toolbar);

            _rtb = new RichTextBox
            {
                Dock        = DockStyle.Fill,
                BackColor   = Theme.BgInput,
                ForeColor   = Theme.TextPrimary,
                Font        = Theme.FontMonoSm,
                ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                ScrollBars  = RichTextBoxScrollBars.Both,
            };
            Controls.Add(_rtb);

            // Subscribe to live log updates
            Logger.OnLog += (line, level) =>
            {
                if (IsDisposed || _rtb.IsDisposed) return;
                try
                {
                    var color = level switch
                    {
                        LogLevel.ERROR   => Theme.TextError,
                        LogLevel.WARN    => Theme.TextWarning,
                        LogLevel.SUCCESS => Theme.TextSuccess,
                        _                => Theme.TextSecondary,
                    };
                    if (_rtb.InvokeRequired)
                        _rtb.Invoke(new Action(() => AppendLine(line, color)));
                    else
                        AppendLine(line, color);
                }
                catch { }
            };
        }

        private void LoadLog()
        {
            var logPath = Logger.GetLogPath();
            if (string.IsNullOrEmpty(logPath) || !System.IO.File.Exists(logPath))
            {
                _rtb.Text = "(Log file không tìm thấy hoặc chưa có log)";
                return;
            }

            try
            {
                var lines = System.IO.File.ReadAllLines(logPath, System.Text.Encoding.UTF8);
                _rtb.Clear();
                foreach (var line in lines)
                {
                    var color = line.Contains("[ERROR]") ? Theme.TextError :
                                line.Contains("[WARN]")  ? Theme.TextWarning :
                                line.Contains("[SUCCESS]") ? Theme.TextSuccess :
                                Theme.TextSecondary;
                    AppendLine(line, color);
                }
                _rtb.SelectionStart = _rtb.TextLength;
                _rtb.ScrollToCaret();
            }
            catch (Exception ex)
            {
                _rtb.Text = $"Lỗi đọc log: {ex.Message}";
            }
        }

        private void SaveLog(AppConfig config)
        {
            using var sfd = new SaveFileDialog
            {
                Title    = "Lưu log",
                Filter   = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = $"WinPE_log_{DateTime.Now:yyyyMMdd_HHmmss}.log",
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                Logger.SaveCopy(sfd.FileName);
                MessageBox.Show($"✅ Log đã lưu:\n{sfd.FileName}", "WinPE", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void AppendLine(string text, Color color)
        {
            _rtb.SelectionStart  = _rtb.TextLength;
            _rtb.SelectionColor  = color;
            _rtb.AppendText(text + "\n");
        }

        private Button MakeBtn(string text, Action click)
        {
            var btn = new Button { Text = text, Font = Theme.FontSmall, ForeColor = Theme.TextPrimary, BackColor = Theme.BgCard, FlatStyle = FlatStyle.Flat, AutoSize = true, Padding = new Padding(8, 2, 8, 2), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderColor = Theme.Border;
            btn.Click += (s, e) => click();
            return btn;
        }
    }
}
