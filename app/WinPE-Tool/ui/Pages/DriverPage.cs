using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WinPETool.Core;
using WinPETool.UI;

namespace WinPETool.UI.Pages
{
    /// <summary>
    /// Driver Management Page
    /// Cho phép Export drivers từ Windows offline hoặc WinPE, Inject driver vào Windows offline, và nạp driver live (drvload)
    /// </summary>
    public class DriverPage : Panel
    {
        private readonly AppConfig _config;
        private ComboBox _targetWindowsCombo;
        private RichTextBox _outputBox;
        private ProgressBar _progressBar;

        public DriverPage(AppConfig config)
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
            var bar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Theme.CatDrivers };
            var lbl = new Label
            {
                Text = "🔌  Quản lý Driver (Export / Inject / Load Live)",
                Font = Theme.FontTitle,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(14, 10)
            };
            header.Controls.Add(bar);
            header.Controls.Add(lbl);
            Controls.Add(header);

            // Target selector panel
            var targetPanel = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Theme.BgToolbar, Padding = new Padding(12, 10, 12, 10) };
            var lblTarget = new Label { Text = "Hệ điều hành đích:", ForeColor = Theme.TextSecondary, Font = Theme.FontBody, AutoSize = true, Location = new Point(12, 14) };
            _targetWindowsCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontBody,
                Width = 260,
                Location = new Point(150, 10)
            };
            var btnRefresh = MakeBtn("🔄 Quét lại", () => RefreshWindowsTargets(), Theme.BgCard);
            btnRefresh.Location = new Point(420, 9);

            targetPanel.Controls.AddRange(new Control[] { lblTarget, _targetWindowsCombo, btnRefresh });
            Controls.Add(targetPanel);

            // Cards flow layout
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 12, 0, 12),
                BackColor = Theme.BgDark
            };

            // Card 1: Export Driver from Offline Windows
            var cardExportOffline = MakeCard(
                "Export Driver từ Windows",
                "Sao lưu toàn bộ driver bên thứ ba (NVMe, WiFi, GPU...) từ bản Windows offline ra thư mục USB.",
                "Export Drivers",
                () => ExportOfflineDrivers()
            );

            // Card 2: Inject Driver into Offline Windows
            var cardInject = MakeCard(
                "Cài / Inject Driver vào Windows",
                "Thêm driver (.inf) vào Windows offline (giải quyết lỗi thiếu driver bàn phím/touchpad/NVMe).",
                "Inject Driver (.inf)",
                () => InjectDriverOffline()
            );

            // Card 3: Load Driver Live into WinPE (drvload)
            var cardDrvLoad = MakeCard(
                "Nạp Driver trực tiếp vào WinPE (Live)",
                "Dùng drvload.exe nạp ngay driver (.inf) vào WinPE mà không cần khởi động lại (dành cho LAN/WiFi/Storage).",
                "DrvLoad (.inf)",
                () => DrvLoadLive()
            );

            // Card 4: Export WinPE Drivers
            var cardExportWinPE = MakeCard(
                "Export Driver của WinPE",
                "Trích xuất tất cả driver đang nạp trong môi trường WinPE hiện tại ra thư mục.",
                "Export WinPE Drivers",
                () => ExportWinPEDrivers()
            );

            flow.Controls.AddRange(new Control[] { cardExportOffline, cardInject, cardDrvLoad, cardExportWinPE });
            Controls.Add(flow);

            // Output / Log section
            var outputHeader = new Label
            {
                Text = "Nhật ký thực thi:",
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

            // Fix z-order for docking
            header.SendToBack();
            targetPanel.SendToBack();
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

            var topBar = new Panel { Dock = DockStyle.Top, Height = 3, BackColor = Theme.CatDrivers };
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

        private string GetSelectedWindowsDir()
        {
            var selected = _targetWindowsCombo.SelectedItem as string;
            if (string.IsNullOrEmpty(selected)) return @"C:\Windows";

            var installs = WindowsFinder.FindInstallations();
            foreach (var inst in installs)
            {
                if (selected.Contains(inst.SystemDrive))
                    return inst.WindowsDirectory;
            }

            return @"C:\Windows";
        }

        private void ExportOfflineDrivers()
        {
            var winDir = GetSelectedWindowsDir();
            if (!Directory.Exists(winDir))
            {
                MessageBox.Show($"Thư mục Windows không tồn tại:\n{winDir}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var fbd = new FolderBrowserDialog { Description = "Chọn thư mục lưu driver xuất ra (nên chọn ổ USB hoặc ổ D:)" };
            if (fbd.ShowDialog() != DialogResult.OK) return;

            var dest = fbd.SelectedPath;
            AppendLog($"\n[BẮT ĐẦU] Xuất driver từ '{winDir}' sang '{dest}'...", Theme.TextAccent);
            SetBusy(true);

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                var cmd = $"dism.exe /Image:\"{winDir}\" /Export-Driver /Destination:\"{dest}\"";
                var result = ProcessRunner.Run("cmd.exe", $"/c {cmd}");

                Invoke(new Action(() =>
                {
                    SetBusy(false);
                    if (result.Success)
                    {
                        AppendLog(result.Output, Theme.TextSuccess);
                        AppendLog($"✅ Xuất driver hoàn tất! Đã lưu tại: {dest}", Theme.TextSuccess);
                        MessageBox.Show($"Xuất driver thành công!\nLưu tại: {dest}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        AppendLog(result.Output, Theme.TextError);
                        AppendLog($"❌ Lỗi xuất driver (Exit code: {result.ExitCode})", Theme.TextError);
                    }
                }));
            });
        }

        private void InjectDriverOffline()
        {
            var winDir = GetSelectedWindowsDir();
            if (!Directory.Exists(winDir))
            {
                MessageBox.Show($"Thư mục Windows không tồn tại:\n{winDir}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var ofd = new OpenFileDialog
            {
                Title = "Chọn file INF của driver cần thêm vào Windows",
                Filter = "Driver INF (*.inf)|*.inf|Tất cả file (*.*)|*.*"
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            var driverFile = ofd.FileName;
            var driverDir = Path.GetDirectoryName(driverFile);

            AppendLog($"\n[BẮT ĐẦU] Cài đặt driver '{driverFile}' vào '{winDir}'...", Theme.TextAccent);
            SetBusy(true);

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                var cmd = $"dism.exe /Image:\"{winDir}\" /Add-Driver /Driver:\"{driverDir}\" /Recurse /ForceUnsigned";
                var result = ProcessRunner.Run("cmd.exe", $"/c {cmd}");

                Invoke(new Action(() =>
                {
                    SetBusy(false);
                    if (result.Success)
                    {
                        AppendLog(result.Output, Theme.TextSuccess);
                        AppendLog("✅ Thêm driver vào Windows thành công!", Theme.TextSuccess);
                        MessageBox.Show("Thêm driver vào Windows thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        AppendLog(result.Output, Theme.TextError);
                        AppendLog($"❌ Lỗi thêm driver (Exit code: {result.ExitCode})", Theme.TextError);
                    }
                }));
            });
        }

        private void DrvLoadLive()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Chọn file INF cần nạp ngay vào WinPE",
                Filter = "Driver INF (*.inf)|*.inf|Tất cả file (*.*)|*.*"
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            var driverFile = ofd.FileName;
            AppendLog($"\n[BẮT ĐẦU] Nạp driver trực tiếp vào WinPE: {driverFile}...", Theme.TextAccent);
            SetBusy(true);

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                var result = ProcessRunner.Run("drvload.exe", $"\"{driverFile}\"");

                Invoke(new Action(() =>
                {
                    SetBusy(false);
                    if (result.Success)
                    {
                        AppendLog(result.Output, Theme.TextSuccess);
                        AppendLog("✅ Nạp driver live thành công!", Theme.TextSuccess);
                        MessageBox.Show("Nạp driver trực tiếp vào WinPE thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        AppendLog(result.Output, Theme.TextError);
                        AppendLog($"❌ Lỗi drvload: {result.Error}", Theme.TextError);
                    }
                }));
            });
        }

        private void ExportWinPEDrivers()
        {
            using var fbd = new FolderBrowserDialog { Description = "Chọn thư mục lưu driver WinPE xuất ra" };
            if (fbd.ShowDialog() != DialogResult.OK) return;

            var dest = fbd.SelectedPath;
            AppendLog($"\n[BẮT ĐẦU] Xuất driver từ WinPE (Online) sang '{dest}'...", Theme.TextAccent);
            SetBusy(true);

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                var cmd = $"dism.exe /Online /Export-Driver /Destination:\"{dest}\"";
                var result = ProcessRunner.Run("cmd.exe", $"/c {cmd}");

                Invoke(new Action(() =>
                {
                    SetBusy(false);
                    if (result.Success)
                    {
                        AppendLog(result.Output, Theme.TextSuccess);
                        AppendLog($"✅ Xuất driver WinPE hoàn tất: {dest}", Theme.TextSuccess);
                        MessageBox.Show("Xuất driver WinPE thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        AppendLog(result.Output, Theme.TextError);
                        AppendLog($"❌ Lỗi: {result.Error}", Theme.TextError);
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
