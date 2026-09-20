using System;
using System.Drawing;
using System.Management;
using System.Windows.Forms;
using WinPETool.Core;
using WinPETool.UI;

namespace WinPETool.UI.Pages
{
    /// <summary>
    /// Dashboard — trang chủ hiển thị hardware summary + Windows detections + quick actions
    /// </summary>
    public class DashboardPage : Panel
    {
        private readonly AppConfig _config;
        private FlowLayoutPanel _cardsPanel;
        private Panel _winPanel;
        private RichTextBox _winList;

        public DashboardPage(AppConfig config)
        {
            _config        = config;
            BackColor      = Theme.BgDark;
            Padding        = new Padding(20);
            AutoScroll     = true;

            BuildUI();
            LoadDataAsync();
        }

        private void BuildUI()
        {
            // ─── Header ────────────────────────────────
            var header = new Label
            {
                Text      = "🏠  Dashboard",
                Font      = Theme.FontTitle,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(20, 20),
            };
            Controls.Add(header);

            var sub = new Label
            {
                Text      = "Tổng quan hệ thống — WinPE Nghitr Dev",
                Font      = Theme.FontBody,
                ForeColor = Theme.TextSecondary,
                AutoSize  = true,
                Location  = new Point(20, 50),
            };
            Controls.Add(sub);

            // ─── Hardware cards ────────────────────────
            _cardsPanel = new FlowLayoutPanel
            {
                Location     = new Point(20, 90),
                Size         = new Size(900, 220),
                Anchor       = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize     = true,
                BackColor    = Color.Transparent,
            };
            Controls.Add(_cardsPanel);

            // Placeholder cards while loading
            AddCard("💻 CPU", "Đang đọc...", Theme.CatHardware);
            AddCard("🧠 RAM", "Đang đọc...", Theme.CatBackup);
            AddCard("💿 Disk", "Đang đọc...", Theme.CatDisk);
            AddCard("🌐 Network", "Đang đọc...", Theme.CatNetwork);
            AddCard("🖥️  OS WinPE", GetWinPEInfo(), Theme.CatSystem);

            // ─── Windows installations ─────────────────
            var winHeader = new Label
            {
                Text      = "🔍  Phát hiện Windows Installations",
                Font      = Theme.FontHeader,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(20, 320),
            };
            Controls.Add(winHeader);

            var scanBtn = new Button
            {
                Text      = "🔄 Quét lại",
                Font      = Theme.FontSmall,
                ForeColor = Theme.Accent,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                AutoSize  = true,
                Location  = new Point(280, 316),
                Cursor    = Cursors.Hand,
            };
            scanBtn.FlatAppearance.BorderSize = 0;
            scanBtn.Click += (s, e) => ScanWindowsInstallations();
            Controls.Add(scanBtn);

            _winPanel = new Panel
            {
                Location  = new Point(20, 350),
                Size      = new Size(880, 200),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Theme.BgCard,
                Padding   = new Padding(12),
            };
            _winList = new RichTextBox
            {
                Dock        = DockStyle.Fill,
                BackColor   = Theme.BgCard,
                ForeColor   = Theme.TextPrimary,
                Font        = Theme.FontMono,
                ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                Text        = "  ⏳ Đang quét Windows installations...",
            };
            _winPanel.Controls.Add(_winList);
            Controls.Add(_winPanel);

            // ─── Quick actions ─────────────────────────
            var qaHeader = new Label
            {
                Text      = "⚡  Thao tác nhanh",
                Font      = Theme.FontHeader,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(20, 570),
            };
            Controls.Add(qaHeader);

            var qaFlow = new FlowLayoutPanel
            {
                Location  = new Point(20, 600),
                AutoSize  = true,
                BackColor = Color.Transparent,
            };

            qaFlow.Controls.Add(MakeQuickBtn("💻 CMD",      () => ProcessRunner.Launch("cmd.exe")));
            qaFlow.Controls.Add(MakeQuickBtn("📜 PowerShell", () => ProcessRunner.Launch("powershell.exe", "-NoExit")));
            qaFlow.Controls.Add(MakeQuickBtn("📝 Notepad",  () => ProcessRunner.Launch("notepad.exe")));
            qaFlow.Controls.Add(MakeQuickBtn("📊 Task Mgr", () => ProcessRunner.Launch("taskmgr.exe")));
            qaFlow.Controls.Add(MakeQuickBtn("🔧 DiskPart", () => ProcessRunner.Launch("cmd.exe", "/k diskpart")));
            qaFlow.Controls.Add(MakeQuickBtn("🗂 Reg Edit", () => ProcessRunner.Launch("regedit.exe")));

            Controls.Add(qaFlow);
        }

        private Button MakeQuickBtn(string text, Action onClick)
        {
            var btn = new Button
            {
                Text      = text,
                Font      = Theme.FontBody,
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.BgCard,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(140, 48),
                Margin    = new Padding(0, 0, 8, 8),
                Cursor    = Cursors.Hand,
            };
            btn.FlatAppearance.BorderColor = Theme.Border;
            btn.FlatAppearance.BorderSize  = 1;
            btn.Click += (s, e) =>
            {
                try { onClick(); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể mở: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            btn.MouseEnter += (s, e) => { btn.BackColor = Theme.BgHover; btn.ForeColor = Theme.Accent; };
            btn.MouseLeave += (s, e) => { btn.BackColor = Theme.BgCard; btn.ForeColor = Theme.TextPrimary; };
            return btn;
        }

        private Panel AddCard(string title, string value, Color accentColor)
        {
            var card = new Panel
            {
                Width     = 175,
                Height    = 90,
                Margin    = new Padding(0, 0, 12, 12),
                BackColor = Theme.BgCard,
            };

            // Top accent line
            var accentLine = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 3,
                BackColor = accentColor,
            };

            var titleLbl = new Label
            {
                Text      = title,
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                AutoSize  = false,
                Width     = card.Width - 16,
                Height    = 20,
                Location  = new Point(10, 12),
            };
            titleLbl.Tag = "title";

            var valueLbl = new Label
            {
                Text      = value,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize  = false,
                Width     = card.Width - 16,
                Height    = 50,
                Location  = new Point(10, 34),
                Tag       = "value",
            };

            card.Controls.AddRange(new Control[] { accentLine, titleLbl, valueLbl });
            card.Tag = title;
            _cardsPanel.Controls.Add(card);
            return card;
        }

        private void UpdateCard(string title, string value)
        {
            foreach (Panel card in _cardsPanel.Controls)
            {
                if (card.Tag?.ToString() != title) continue;
                foreach (Control c in card.Controls)
                    if (c.Tag?.ToString() == "value") c.Text = value;
            }
        }

        private string GetWinPEInfo()
        {
            var arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            return $"WinPE {arch}\nNghitr Dev v{_config.Version}";
        }

        private void LoadDataAsync()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try { LoadHardwareInfo(); } catch { }
                try { ScanWindowsInstallations(); } catch { }
            });
        }

        private void LoadHardwareInfo()
        {
            // CPU
            try
            {
                using var cpu = new ManagementObjectSearcher("SELECT Name, NumberOfCores, MaxClockSpeed FROM Win32_Processor");
                foreach (ManagementObject obj in cpu.Get())
                {
                    var name  = obj["Name"]?.ToString()?.Trim() ?? "Unknown";
                    var cores = obj["NumberOfCores"]?.ToString() ?? "?";
                    var mhz   = obj["MaxClockSpeed"]?.ToString() ?? "?";
                    var ghz   = double.TryParse(mhz, out double m) ? $"{m/1000:F1} GHz" : mhz;
                    SafeUpdate(() => UpdateCard("💻 CPU", $"{name}\n{cores} cores @ {ghz}"));
                    break;
                }
            }
            catch { SafeUpdate(() => UpdateCard("💻 CPU", "Lỗi đọc CPU")); }

            // RAM
            try
            {
                using var ram = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in ram.Get())
                {
                    var total = obj["TotalPhysicalMemory"]?.ToString() ?? "0";
                    var gb    = double.TryParse(total, out double b) ? $"{b/1024/1024/1024:F1} GB" : total;
                    SafeUpdate(() => UpdateCard("🧠 RAM", $"Total: {gb}"));
                    break;
                }
            }
            catch { SafeUpdate(() => UpdateCard("🧠 RAM", "Lỗi đọc RAM")); }

            // Disks
            try
            {
                using var disks = new ManagementObjectSearcher("SELECT Model, Size FROM Win32_DiskDrive");
                var diskInfo = "";
                int count = 0;
                foreach (ManagementObject obj in disks.Get())
                {
                    var model = obj["Model"]?.ToString() ?? "Unknown";
                    var sizeB = obj["Size"]?.ToString() ?? "0";
                    var sizeG = double.TryParse(sizeB, out double b) ? $"{b/1024/1024/1024:F0}GB" : "?";
                    diskInfo += $"[{count}] {sizeG}\n";
                    count++;
                    if (count >= 3) break;
                }
                SafeUpdate(() => UpdateCard("💿 Disk", diskInfo.TrimEnd() + $"\n({count} disk(s))"));
            }
            catch { SafeUpdate(() => UpdateCard("💿 Disk", "Lỗi đọc disk")); }

            // Network
            try
            {
                using var net = new ManagementObjectSearcher(
                    "SELECT Description, MACAddress FROM Win32_NetworkAdapter WHERE PhysicalAdapter=True");
                int count = 0;
                var info = "";
                foreach (ManagementObject obj in net.Get())
                {
                    info += obj["Description"]?.ToString()?.Substring(0, Math.Min(20, obj["Description"]?.ToString()?.Length ?? 0)) + "\n";
                    count++;
                    if (count >= 2) break;
                }
                SafeUpdate(() => UpdateCard("🌐 Network", info.TrimEnd() + $"\n({count} adapter(s))"));
            }
            catch { SafeUpdate(() => UpdateCard("🌐 Network", "Lỗi đọc network")); }
        }

        private void ScanWindowsInstallations()
        {
            SafeUpdate(() => _winList.Text = "  ⏳ Đang quét...");

            var installs = WindowsFinder.FindAll();

            SafeUpdate(() =>
            {
                _winList.Clear();
                if (installs.Count == 0)
                {
                    _winList.SelectionColor = Theme.TextWarning;
                    _winList.AppendText("  ⚠️  Không tìm thấy Windows installation nào trên các ổ đĩa.\n");
                    _winList.AppendText("  (WinPE đang chạy từ RAM — đây là bình thường)\n");
                    return;
                }

                _winList.SelectionColor = Theme.TextSuccess;
                _winList.AppendText($"  ✅  Tìm thấy {installs.Count} Windows installation(s):\n\n");

                foreach (var inst in installs)
                {
                    _winList.SelectionColor = Theme.Accent;
                    _winList.AppendText($"  📁  {inst.WindowsPath}\n");
                    _winList.SelectionColor = Theme.TextSecondary;
                    _winList.AppendText($"      Version: {inst.Version}  |  Edition: {inst.Edition}\n");
                    _winList.AppendText($"      BCD: {(inst.HasBCD ? "✅ Found" : "⚠️  Not found")}\n");
                    _winList.AppendText($"      Free space: {inst.FreeSpaceBytes/1024/1024/1024} GB\n\n");
                }
            });
        }

        private void SafeUpdate(Action action)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
                Invoke(action);
            else
                action();
        }
    }

    /// <summary>
    /// Placeholder cho các module chưa implement
    /// </summary>
    public class PlaceholderPage : Panel
    {
        public PlaceholderPage(string title, string subtitle, Color accentColor)
        {
            BackColor = Theme.BgDark;
            Padding   = new Padding(40);

            // Top accent bar
            var bar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 4,
                BackColor = accentColor,
            };

            var titleLbl = new Label
            {
                Text      = title,
                Font      = Theme.FontTitle,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(40, 50),
            };

            var subLbl = new Label
            {
                Text      = subtitle,
                Font      = Theme.FontBody,
                ForeColor = Theme.TextSecondary,
                AutoSize  = true,
                Location  = new Point(40, 90),
            };

            var devNote = new Label
            {
                Text      = "🚧  Module này đang trong quá trình phát triển.\n    Sẽ được bổ sung trong phiên bản tiếp theo.",
                Font      = Theme.FontBody,
                ForeColor = Theme.TextMuted,
                AutoSize  = true,
                Location  = new Point(40, 140),
            };

            Controls.AddRange(new Control[] { bar, titleLbl, subLbl, devNote });
        }
    }
}
