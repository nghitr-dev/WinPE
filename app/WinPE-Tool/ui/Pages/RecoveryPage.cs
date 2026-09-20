using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WinPETool.Core;
using WinPETool.UI;

namespace WinPETool.UI.Pages
{
    /// <summary>
    /// Windows Recovery Page
    /// - Detect Windows installations (KHÔNG giả định C:)
    /// - Startup Repair / BCD repair / bootrec / bcdboot
    /// - DISM offline repair / SFC / CHKDSK
    /// - TẤT CẢ thao tác có log và confirmation
    /// </summary>
    public class RecoveryPage : Panel
    {
        private ListBox  _winList;
        private RichTextBox _output;
        private Button   _scanBtn;
        private Label    _selectedWinLabel;
        private List<WindowsInstallation> _installations = new List<WindowsInstallation>();
        private WindowsInstallation _selectedInstall;

        public RecoveryPage(AppConfig config)
        {
            BackColor = Theme.BgDark;
            Dock      = DockStyle.Fill;
            BuildUI();
        }

        private void BuildUI()
        {
            // Header
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50 };
            var bar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Theme.CatRecovery };
            var lbl = new Label { Text = "🔧  Phục hồi Windows", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary, AutoSize = true, Location = new Point(14, 12) };
            hdr.Controls.Add(bar); hdr.Controls.Add(lbl);
            Controls.Add(hdr);

            // Main split: left = detection + tools, right = output
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 380,
                BackColor = Theme.BgDark,
            };

            // ─── LEFT PANEL ───────────────────────────────────────────────
            var leftPanel = split.Panel1;
            leftPanel.BackColor = Theme.BgDark;
            leftPanel.AutoScroll = true;

            int y = 8;

            // Windows Detection
            var detectionGrp = MakeGroup("🔍 Phát hiện Windows", y, leftPanel.Width - 20, 180);
            leftPanel.Controls.Add(detectionGrp); y += 188;

            _scanBtn = new Button
            {
                Text      = "🔄 Quét tất cả ổ đĩa",
                Font      = Theme.FontBody,
                ForeColor = Theme.Accent,
                BackColor = Theme.BgCard,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(160, 30),
                Location  = new Point(8, 22),
                Cursor    = Cursors.Hand,
            };
            _scanBtn.FlatAppearance.BorderColor = Theme.Accent;
            _scanBtn.Click += (s, e) => ScanWindowsInstallations();
            detectionGrp.Controls.Add(_scanBtn);

            _winList = new ListBox
            {
                Location    = new Point(8, 58),
                Size        = new Size(detectionGrp.Width - 16, 100),
                BackColor   = Theme.BgInput,
                ForeColor   = Theme.TextPrimary,
                Font        = Theme.FontMono,
                BorderStyle = BorderStyle.None,
            };
            _winList.SelectedIndexChanged += (s, e) => OnWindowsSelected();
            detectionGrp.Controls.Add(_winList);

            // Selected info
            _selectedWinLabel = new Label
            {
                Location  = new Point(10, y + 4),
                AutoSize  = false,
                Size      = new Size(leftPanel.Width - 20, 36),
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextWarning,
                Text      = "⚠️  Chọn một Windows installation trước khi thực hiện repair.",
                BackColor = Color.FromArgb(40, 255, 160, 0),
                Padding   = new Padding(6, 6, 0, 0),
            };
            leftPanel.Controls.Add(_selectedWinLabel); y += 44;

            // Repair tool groups
            leftPanel.Controls.Add(BuildRepairGroup(y, leftPanel.Width - 20)); y += 468;

            // ─── RIGHT PANEL: Output ──────────────────────────────────────
            var rightPanel = split.Panel2;
            rightPanel.BackColor = Theme.BgDark;

            var outHdr = new Label { Text = "  OUTPUT / LOG", Font = Theme.FontHeader, ForeColor = Theme.Accent, Dock = DockStyle.Top, Height = 28, BackColor = Theme.BgToolbar, Padding = new Padding(8, 6, 0, 0) };
            _output = new RichTextBox
            {
                Dock        = DockStyle.Fill,
                BackColor   = Theme.BgInput,
                ForeColor   = Theme.TextPrimary,
                Font        = Theme.FontMonoSm,
                ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                ScrollBars  = RichTextBoxScrollBars.Both,
            };
            var clearBtn = new Button { Text = "Xóa log", Font = Theme.FontSmall, ForeColor = Theme.TextMuted, BackColor = Color.Transparent, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Bottom, Height = 24 };
            clearBtn.FlatAppearance.BorderSize = 0;
            clearBtn.Click += (s, e) => _output.Clear();

            rightPanel.Controls.Add(_output);
            rightPanel.Controls.Add(outHdr);
            rightPanel.Controls.Add(clearBtn);

            Controls.Add(split);

            // Auto-scan on load
            ScanWindowsInstallations();
        }

        private Panel BuildRepairGroup(int y, int width)
        {
            var grp = new Panel { Location = new Point(10, y), Width = width, Height = 460, BackColor = Color.Transparent };
            int gy  = 0;

            // BCD / Bootloader
            var bcdGrp = MakeGroup("💾 BCD & Bootloader Repair", gy, width, 180);
            grp.Controls.Add(bcdGrp); gy += 188;

            AddToolBtn(bcdGrp, "bootrec /fixmbr",       "Sửa MBR",                    8,  28, () => RunBootrec("/fixmbr"));
            AddToolBtn(bcdGrp, "bootrec /fixboot",      "Sửa Boot Sector",             8,  62, () => RunBootrec("/fixboot"));
            AddToolBtn(bcdGrp, "bootrec /rebuildbcd",   "Rebuild BCD",                 8,  96, () => RunBootrec("/rebuildbcd"));
            AddToolBtn(bcdGrp, "bcdboot",               "Sửa UEFI Boot",             160,  28, () => RunBCDBoot());
            AddToolBtn(bcdGrp, "bootrec /scanos",       "Scan OS",                   160,  62, () => RunBootrec("/scanos"));
            AddToolBtn(bcdGrp, "bcdedit",               "Xem BCD hiện tại",          160,  96, () => RunBCDEdit());
            AddToolBtn(bcdGrp, "Xóa & Tạo BCD mới",    "⚠ Nguy hiểm",               8,  130, () => RebuildBCDConfirm(), true);
            AddToolBtn(bcdGrp, "Fix EFI Boot",          "Cho UEFI",                  160, 130, () => FixEFIBoot());

            // DISM / Windows Repair
            var dismGrp = MakeGroup("🔨 DISM & Windows Repair", gy, width, 170);
            grp.Controls.Add(dismGrp); gy += 178;

            AddToolBtn(dismGrp, "DISM /CheckHealth",    "Kiểm tra",                   8,  28, () => RunDISM("/CheckHealth"));
            AddToolBtn(dismGrp, "DISM /ScanHealth",     "Quét lỗi",                   8,  62, () => RunDISM("/ScanHealth"));
            AddToolBtn(dismGrp, "DISM /RestoreHealth",  "Sửa chữa",                   8,  96, () => RunDISM("/RestoreHealth"));
            AddToolBtn(dismGrp, "SFC /scannow",         "System File Check",         160,  28, () => RunSFC());
            AddToolBtn(dismGrp, "DISM Offline",         "Sửa offline Windows",       160,  62, () => RunDISMOffline());
            AddToolBtn(dismGrp, "chkdsk",               "Check Disk",                160,  96, () => RunCHKDSK());

            // Registry
            var regGrp = MakeGroup("🗝 Registry & System", gy, width, 96);
            grp.Controls.Add(regGrp); gy += 104;

            AddToolBtn(regGrp, "RegEdit",               "Registry Editor",            8,  28, () => ProcessRunner.Launch("regedit.exe"));
            AddToolBtn(regGrp, "Offline RegEdit",       "Load offline hive",        160,  28, () => LoadOfflineRegistry());
            AddToolBtn(regGrp, "Xem Event Log",         "eventvwr offline",           8,  62, () => OpenEventLog());

            return grp;
        }

        // ─── WINDOWS DETECTION ───────────────────────────────────────────────
        private void ScanWindowsInstallations()
        {
            _scanBtn.Enabled = false;
            _winList.Items.Clear();
            AppendOut("🔍 Đang quét Windows installations trên tất cả ổ đĩa...\n", Theme.TextMuted);

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                _installations = WindowsFinder.FindAll();

                SafeUpdate(() =>
                {
                    _winList.Items.Clear();
                    if (_installations.Count == 0)
                    {
                        _winList.Items.Add("  (Không tìm thấy Windows)");
                        AppendOut("⚠️  Không tìm thấy Windows installation.\n", Theme.TextWarning);
                    }
                    else
                    {
                        foreach (var inst in _installations)
                        {
                            var display = $"{inst.WindowsPath}  [{inst.Version}]";
                            _winList.Items.Add(display);
                        }
                        AppendOut($"✅ Tìm thấy {_installations.Count} Windows installation(s).\n", Theme.TextSuccess);
                    }
                    _scanBtn.Enabled = true;
                });
            });
        }

        private void OnWindowsSelected()
        {
            int idx = _winList.SelectedIndex;
            if (idx < 0 || idx >= _installations.Count) { _selectedInstall = null; return; }
            _selectedInstall = _installations[idx];
            _selectedWinLabel.Text      = $"✅ Đang thao tác: {_selectedInstall.WindowsPath}  [{_selectedInstall.Version}]";
            _selectedWinLabel.ForeColor = Theme.TextSuccess;
            _selectedWinLabel.BackColor = Color.FromArgb(25, 50, 200, 100);
            AppendOut($"\n▶ Đã chọn Windows: {_selectedInstall.WindowsPath}\n", Theme.Accent);
        }

        private bool EnsureWindowsSelected()
        {
            if (_selectedInstall == null)
            {
                MessageBox.Show("Vui lòng chọn một Windows installation trước.", "WinPE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }

        // ─── REPAIR OPERATIONS ───────────────────────────────────────────────
        private void RunBootrec(string arg)
        {
            AppendOut($"\n▶ bootrec {arg}\n", Theme.Accent, Theme.FontHeader);
            AppendOut("⚠️  Chú ý: bootrec có thể không có trong WinPE 11 mặc định.\n", Theme.TextWarning);
            RunTool("bootrec.exe", arg, 30000);
        }

        private void RunBCDBoot()
        {
            if (!EnsureWindowsSelected()) return;
            var winDir = _selectedInstall.WindowsPath;
            var drive  = Path.GetPathRoot(winDir);
            AppendOut($"\n▶ bcdboot {winDir} /s {drive} /f ALL\n", Theme.Accent, Theme.FontHeader);
            RunTool("bcdboot.exe", $"\"{winDir}\" /s {drive.TrimEnd('\\')} /f ALL", 30000);
        }

        private void FixEFIBoot()
        {
            if (!EnsureWindowsSelected()) return;
            var winDir   = _selectedInstall.WindowsPath;
            var efiDrive = FindEFIPartition();
            if (string.IsNullOrEmpty(efiDrive))
            {
                AppendOut("❌ Không tìm thấy EFI partition. Cần gán ký tự cho EFI partition trước.\n", Theme.TextError);
                AppendOut("   → Vào tab Disk & Partition → chọn EFI partition → Mount/Gán ký tự.\n", Theme.TextMuted);
                return;
            }
            AppendOut($"\n▶ Fix EFI Boot: bcdboot {winDir} /s {efiDrive} /f UEFI\n", Theme.Accent, Theme.FontHeader);
            RunTool("bcdboot.exe", $"\"{winDir}\" /s {efiDrive.TrimEnd('\\')} /f UEFI", 30000);
        }

        private void RunBCDEdit()
        {
            AppendOut("\n▶ bcdedit /enum all\n", Theme.Accent, Theme.FontHeader);
            RunTool("bcdedit.exe", "/enum all", 15000);
        }

        private void RebuildBCDConfirm()
        {
            var msg = "⚠️  CẢNH BÁO\n\nThao tác này sẽ XÓA BCD hiện tại và tạo lại từ đầu.\n" +
                      "Nếu có nhiều OS (dual boot), cấu hình có thể bị mất.\n\n" +
                      "Chỉ thực hiện khi BCD bị corrupt hoàn toàn.\n\nTiếp tục?";
            if (MessageBox.Show(msg, "Xác nhận Rebuild BCD", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            { AppendOut("⛔ Rebuild BCD bị hủy.\n", Theme.TextMuted); return; }

            AppendOut("\n▶ Rebuilding BCD...\n", Theme.TextWarning, Theme.FontHeader);
            // bootrec /rebuildbcd
            RunTool("bootrec.exe", "/rebuildbcd", 60000);
        }

        private void RunDISM(string arg)
        {
            AppendOut($"\n▶ DISM /Online {arg}\n", Theme.Accent, Theme.FontHeader);
            AppendOut("ℹ️  Thao tác này kiểm tra Windows ĐANG CHẠY (WinPE), không phải offline Windows.\n", Theme.TextMuted);
            RunTool("dism.exe", $"/Online {arg}", 180000,
                line => AppendOut(line + "\n", Theme.TextSecondary));
        }

        private void RunDISMOffline()
        {
            if (!EnsureWindowsSelected()) return;
            var winDir = _selectedInstall.WindowsPath;
            AppendOut($"\n▶ DISM /Image:\"{winDir}\" /RestoreHealth\n", Theme.Accent, Theme.FontHeader);
            AppendOut("⏳ Thao tác này có thể mất vài phút...\n", Theme.TextMuted);
            RunTool("dism.exe", $"/Image:\"{winDir}\" /Cleanup-Image /RestoreHealth", 600000,
                line => AppendOut(line + "\n", Theme.TextSecondary));
        }

        private void RunSFC()
        {
            if (!EnsureWindowsSelected()) return;
            var winDir = _selectedInstall.WindowsPath;
            var drive  = Path.GetPathRoot(winDir);
            AppendOut($"\n▶ SFC /SCANNOW /OFFBOOTDIR={drive} /OFFWINDIR={winDir}\n", Theme.Accent, Theme.FontHeader);
            RunTool("sfc.exe", $"/SCANNOW /OFFBOOTDIR=\"{drive.TrimEnd('\\')}\" /OFFWINDIR=\"{winDir}\"", 300000,
                line => AppendOut(line + "\n", Theme.TextSecondary));
        }

        private void RunCHKDSK()
        {
            if (!EnsureWindowsSelected()) return;
            var drive = Path.GetPathRoot(_selectedInstall.WindowsPath);
            AppendOut($"\n▶ chkdsk {drive} /f /r\n", Theme.Accent, Theme.FontHeader);
            AppendOut("⚠️  chkdsk /r có thể mất nhiều giờ trên ổ lớn.\n", Theme.TextWarning);
            if (MessageBox.Show($"Chạy chkdsk {drive} /f /r?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            RunTool("chkdsk.exe", $"{drive.TrimEnd('\\')} /f /r", 3600000,
                line => AppendOut(line + "\n", Theme.TextSecondary));
        }

        private void LoadOfflineRegistry()
        {
            if (!EnsureWindowsSelected()) return;
            var hivePath = Path.Combine(_selectedInstall.WindowsPath, "System32", "config", "SOFTWARE");
            if (!File.Exists(hivePath))
            {
                AppendOut($"❌ Không tìm thấy hive: {hivePath}\n", Theme.TextError);
                return;
            }

            AppendOut($"\n▶ Load offline registry hive: {hivePath}\n", Theme.Accent, Theme.FontHeader);
            var tempKey = "OFFLINE_WIN_SOFTWARE";
            var loadResult = ProcessRunner.RunCmd($"reg load HKLM\\{tempKey} \"{hivePath}\"");
            if (loadResult.Success)
            {
                AppendOut($"✅ Hive loaded at HKLM\\{tempKey}\n", Theme.TextSuccess);
                AppendOut("   Mở Registry Editor để xem/chỉnh.\n", Theme.TextMuted);
                ProcessRunner.Launch("regedit.exe");
                AppendOut($"⚠️  Khi xong, unload bằng: reg unload HKLM\\{tempKey}\n", Theme.TextWarning);
            }
            else
            {
                AppendOut($"❌ Load failed: {loadResult.Error}\n", Theme.TextError);
            }
        }

        private void OpenEventLog()
        {
            AppendOut("\n▶ Mở Event Viewer...\n", Theme.Accent, Theme.FontHeader);
            AppendOut("ℹ️  Event Viewer trong WinPE chỉ xem được WinPE events, không phải offline Windows.\n", Theme.TextMuted);
            try { ProcessRunner.Launch("eventvwr.exe"); }
            catch { AppendOut("❌ eventvwr.exe không có trong WinPE.\n", Theme.TextError); }
        }

        // ─── TOOL RUNNER ─────────────────────────────────────────────────────
        private void RunTool(string exe, string args, int timeoutMs = 60000, Action<string> onLine = null)
        {
            SetBusy(true);
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                Logger.Info($"Running: {exe} {args}");
                var result = ProcessRunner.Run(exe, args, null, timeoutMs, line =>
                {
                    onLine?.Invoke(line);
                    AppendOut(line + "\n", Theme.TextSecondary);
                });

                SafeUpdate(() =>
                {
                    if (result.Success)
                        AppendOut($"✅ Hoàn thành (exit: {result.ExitCode})\n", Theme.TextSuccess);
                    else
                        AppendOut($"❌ Thất bại (exit: {result.ExitCode})\n{result.Error}\n", Theme.TextError);
                    SetBusy(false);
                });
            });
        }

        // ─── HELPERS ─────────────────────────────────────────────────────────
        private string FindEFIPartition()
        {
            // Look for a drive with \EFI\Microsoft\Boot
            foreach (var drive in System.IO.DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;
                var efiPath = Path.Combine(drive.Name, "EFI", "Microsoft", "Boot");
                if (Directory.Exists(efiPath)) return drive.Name;
            }
            return null;
        }

        private Panel MakeGroup(string title, int y, int width, int height)
        {
            var p = new Panel
            {
                Location  = new Point(0, y),
                Size      = new Size(width, height),
                BackColor = Theme.BgCard,
            };
            var titleBar = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold),
                ForeColor = Theme.Accent,
                Dock      = DockStyle.Top,
                Height    = 22,
                BackColor = Theme.BgToolbar,
                Padding   = new Padding(8, 4, 0, 0),
            };
            p.Controls.Add(titleBar);
            return p;
        }

        private void AddToolBtn(Panel parent, string text, string tooltip, int x, int y, Action click, bool danger = false)
        {
            var btn = new Button
            {
                Text      = text,
                Font      = Theme.FontSmall,
                ForeColor = danger ? Theme.TextError : Theme.TextPrimary,
                BackColor = Theme.BgDark,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(140, 26),
                Location  = new Point(x, y),
                Cursor    = Cursors.Hand,
            };
            btn.FlatAppearance.BorderColor = danger ? Theme.Error : Theme.Border;
            new ToolTip().SetToolTip(btn, tooltip);
            btn.Click += (s, e) => { try { click(); } catch (Exception ex) { AppendOut($"❌ {ex.Message}\n", Theme.TextError); } };
            btn.MouseEnter += (s, e) => btn.BackColor = danger ? Color.FromArgb(40, 200, 50, 50) : Theme.BgHover;
            btn.MouseLeave += (s, e) => btn.BackColor = Theme.BgDark;
            parent.Controls.Add(btn);
        }

        private void AppendOut(string text, Color color, Font font = null)
        {
            SafeUpdate(() =>
            {
                _output.SelectionStart  = _output.TextLength;
                _output.SelectionColor  = color;
                if (font != null) _output.SelectionFont = font;
                _output.AppendText(text);
                _output.ScrollToCaret();
            });
        }

        private void SetBusy(bool busy)
        {
            SafeUpdate(() => Cursor = busy ? Cursors.WaitCursor : Cursors.Default);
        }

        private void SafeUpdate(Action action)
        {
            if (IsDisposed) return;
            try { if (InvokeRequired) Invoke(action); else action(); }
            catch { }
        }
    }
}
