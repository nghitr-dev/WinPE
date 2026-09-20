using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Windows.Forms;
using WinPETool.Core;
using WinPETool.UI;

namespace WinPETool.UI.Pages
{
    /// <summary>
    /// Disk & Partition Management Page
    /// CÁC THAO TÁC PHÁ HỦY DỮ LIỆU ĐỀU CÓ CONFIRMATION
    /// </summary>
    public class DiskPage : Panel
    {
        private ListView _diskList;
        private ListView _partList;
        private RichTextBox _output;
        private Button _refreshBtn;
        private Label _selectionInfo;

        private List<DiskInfo>      _disks      = new List<DiskInfo>();
        private List<PartitionInfo> _partitions = new List<PartitionInfo>();

        public DiskPage(AppConfig config)
        {
            BackColor = Theme.BgDark;
            Dock      = DockStyle.Fill;
            BuildUI();
            LoadDisksAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // UI BUILD
        // ─────────────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // Header
            var header = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent };
            var bar    = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Theme.CatDisk };
            var lbl    = new Label { Text = "💿  Quản lý ổ đĩa & Phân vùng", Font = Theme.FontTitle, ForeColor = Theme.TextPrimary, AutoSize = true, Location = new Point(14, 12) };
            header.Controls.Add(bar); header.Controls.Add(lbl);
            Controls.Add(header);

            // Toolbar
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.BgToolbar, Padding = new Padding(8, 6, 8, 6) };
            _refreshBtn = MakeBtn("🔄 Làm mới",    () => LoadDisksAsync(), Theme.Accent);
            var assignBtn  = MakeBtn("📌 Gán ký tự", () => AssignDriveLetter(), Theme.CatBackup);
            var mountBtn   = MakeBtn("▶ Mount",      () => MountVolume(), Theme.CatBackup);
            var formatBtn  = MakeBtn("⚠ Format",    () => FormatPartitionConfirm(), Theme.TextWarning);
            var deleteBtn  = MakeBtn("🗑 Xóa phân vùng", () => DeletePartitionConfirm(), Theme.TextError);
            var diskpartBtn= MakeBtn("🖥 DiskPart CLI", () => ProcessRunner.Launch("cmd.exe", "/k diskpart"), Theme.TextSecondary);

            var toolFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            toolFlow.Controls.AddRange(new Control[] { _refreshBtn, assignBtn, mountBtn, formatBtn, deleteBtn, diskpartBtn });
            toolbar.Controls.Add(toolFlow);
            Controls.Add(toolbar);

            // Split: Disks (top) + Partitions (mid) + Output (bottom)
            var split1 = new SplitContainer
            {
                Dock        = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 200,
                BackColor   = Theme.BgDark,
            };

            // TOP: Disk list
            var diskPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgDark };
            var diskHeader = new Label { Text = "  ОЖ ĐĨA VẬT LÝ", Font = Theme.FontHeader, ForeColor = Theme.Accent, Dock = DockStyle.Top, Height = 24, BackColor = Theme.BgToolbar };
            _diskList = MakeListView();
            _diskList.Columns.Add("Disk #", 55);
            _diskList.Columns.Add("Model",  220);
            _diskList.Columns.Add("Kích thước", 90);
            _diskList.Columns.Add("Interface", 80);
            _diskList.Columns.Add("Loại", 65);
            _diskList.Columns.Add("Tình trạng", 90);
            _diskList.SelectedIndexChanged += (s, e) => OnDiskSelected();
            diskPanel.Controls.Add(_diskList);
            diskPanel.Controls.Add(diskHeader);

            // BOTTOM: Partition list + output
            var split2 = new SplitContainer
            {
                Dock        = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 180,
                BackColor   = Theme.BgDark,
            };

            var partPanel = new Panel { Dock = DockStyle.Fill };
            var partHeader = new Label { Text = "  PHÂN VÙNG", Font = Theme.FontHeader, ForeColor = Theme.Accent, Dock = DockStyle.Top, Height = 24, BackColor = Theme.BgToolbar };
            _partList = MakeListView();
            _partList.Columns.Add("Part #", 55);
            _partList.Columns.Add("Ký tự", 50);
            _partList.Columns.Add("File System", 80);
            _partList.Columns.Add("Kích thước", 90);
            _partList.Columns.Add("Trống", 90);
            _partList.Columns.Add("Loại", 90);
            _partList.Columns.Add("Trạng thái", 100);
            partPanel.Controls.Add(_partList);
            partPanel.Controls.Add(partHeader);

            _selectionInfo = new Label { Dock = DockStyle.Bottom, Height = 20, Font = Theme.FontSmall, ForeColor = Theme.TextMuted, BackColor = Theme.BgToolbar, Padding = new Padding(8, 2, 0, 0) };
            partPanel.Controls.Add(_selectionInfo);

            // Output log
            var outPanel = new Panel { Dock = DockStyle.Fill };
            var outHeader = new Label { Text = "  OUTPUT", Font = Theme.FontHeader, ForeColor = Theme.Accent, Dock = DockStyle.Top, Height = 24, BackColor = Theme.BgToolbar };
            _output = new RichTextBox { Dock = DockStyle.Fill, BackColor = Theme.BgInput, ForeColor = Theme.TextPrimary, Font = Theme.FontMonoSm, ReadOnly = true, BorderStyle = BorderStyle.None };
            outPanel.Controls.Add(_output);
            outPanel.Controls.Add(outHeader);

            split2.Panel1.Controls.Add(partPanel);
            split2.Panel2.Controls.Add(outPanel);
            split1.Panel1.Controls.Add(diskPanel);
            split1.Panel2.Controls.Add(split2);
            Controls.Add(split1);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DATA LOADING
        // ─────────────────────────────────────────────────────────────────────
        private void LoadDisksAsync()
        {
            SafeUpdate(() =>
            {
                _diskList.Items.Clear();
                _partList.Items.Clear();
                _refreshBtn.Enabled = false;
                AppendOut("⏳ Đang đọc thông tin ổ đĩa...\n", Theme.TextMuted);
            });

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _disks.Clear();
                    using var q = new ManagementObjectSearcher(
                        "SELECT Index, Model, Size, InterfaceType, MediaType, Partitions, SerialNumber FROM Win32_DiskDrive ORDER BY Index");

                    foreach (ManagementObject o in q.Get())
                    {
                        var sizeBytes = Convert.ToInt64(o["Size"] ?? 0);
                        _disks.Add(new DiskInfo
                        {
                            Index      = Convert.ToInt32(o["Index"]),
                            Model      = o["Model"]?.ToString()?.Trim() ?? "Unknown",
                            SizeBytes  = sizeBytes,
                            Interface  = o["InterfaceType"]?.ToString() ?? "Unknown",
                            MediaType  = o["MediaType"]?.ToString() ?? "Unknown",
                            Partitions = Convert.ToInt32(o["Partitions"] ?? 0),
                            Serial     = o["SerialNumber"]?.ToString()?.Trim() ?? "",
                        });
                    }

                    SafeUpdate(() =>
                    {
                        _diskList.Items.Clear();
                        foreach (var d in _disks)
                        {
                            var item = new ListViewItem(new[]
                            {
                                $"Disk {d.Index}",
                                d.Model,
                                $"{d.SizeBytes/1024.0/1024/1024:F1} GB",
                                d.Interface,
                                d.MediaType,
                                "Online"
                            });
                            item.Tag = d;
                            // NVMe highlight
                            if (d.Interface.Contains("SCSI") || d.Model.ToLower().Contains("nvme"))
                                item.ForeColor = Theme.Accent;
                            _diskList.Items.Add(item);
                        }
                        AppendOut($"✅ Tìm thấy {_disks.Count} ổ đĩa.\n", Theme.TextSuccess);
                        _refreshBtn.Enabled = true;
                    });
                }
                catch (Exception ex) { SafeUpdate(() => AppendOut($"❌ Lỗi: {ex.Message}\n", Theme.TextError)); }
            });
        }

        private void OnDiskSelected()
        {
            if (_diskList.SelectedItems.Count == 0) return;
            var disk = _diskList.SelectedItems[0].Tag as DiskInfo;
            if (disk == null) return;

            _selectionInfo.Text = $"Đã chọn: Disk {disk.Index} — {disk.Model} ({disk.SizeBytes/1024/1024/1024} GB)";
            LoadPartitions(disk.Index);
        }

        private void LoadPartitions(int diskIndex)
        {
            _partList.Items.Clear();

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _partitions.Clear();
                    // Get partitions for this disk
                    using var q = new ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='//./PHYSICALDRIVE{diskIndex}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");

                    foreach (ManagementObject part in q.Get())
                    {
                        // Get logical disk for this partition
                        using var lq = new ManagementObjectSearcher(
                            $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{part["DeviceID"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");

                        var pi = new PartitionInfo
                        {
                            DiskIndex  = diskIndex,
                            Name       = part["Name"]?.ToString() ?? "",
                            SizeBytes  = Convert.ToInt64(part["Size"] ?? 0),
                            Type       = part["Type"]?.ToString() ?? "",
                            DeviceId   = part["DeviceID"]?.ToString() ?? "",
                        };

                        foreach (ManagementObject ld in lq.Get())
                        {
                            pi.DriveLetter = ld["DeviceID"]?.ToString() ?? "";
                            pi.FileSystem  = ld["FileSystem"]?.ToString() ?? "";
                            pi.FreeBytes   = Convert.ToInt64(ld["FreeSpace"] ?? 0);
                            pi.Label       = ld["VolumeName"]?.ToString() ?? "";
                        }

                        _partitions.Add(pi);

                        SafeUpdate(() =>
                        {
                            var item = new ListViewItem(new[]
                            {
                                pi.Name,
                                pi.DriveLetter,
                                pi.FileSystem,
                                $"{pi.SizeBytes/1024.0/1024/1024:F1} GB",
                                pi.FreeBytes > 0 ? $"{pi.FreeBytes/1024.0/1024/1024:F1} GB" : "—",
                                pi.Type,
                                "Active",
                            });
                            item.Tag = pi;
                            if (string.IsNullOrEmpty(pi.DriveLetter)) item.ForeColor = Theme.TextMuted;
                            _partList.Items.Add(item);
                        });
                    }
                }
                catch (Exception ex)
                {
                    SafeUpdate(() => AppendOut($"❌ Lỗi đọc partition: {ex.Message}\n", Theme.TextError));
                }
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // DISK OPERATIONS — TẤT CẢ CÓ CONFIRMATION
        // ─────────────────────────────────────────────────────────────────────
        private PartitionInfo GetSelectedPartition()
        {
            if (_partList.SelectedItems.Count == 0)
            {
                MessageBox.Show("Chọn một phân vùng trước.", "WinPE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
            return _partList.SelectedItems[0].Tag as PartitionInfo;
        }

        private DiskInfo GetSelectedDisk()
        {
            if (_diskList.SelectedItems.Count == 0)
            {
                MessageBox.Show("Chọn một ổ đĩa trước.", "WinPE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
            return _diskList.SelectedItems[0].Tag as DiskInfo;
        }

        private void AssignDriveLetter()
        {
            var part = GetSelectedPartition();
            if (part == null) return;

            var letter = Microsoft.VisualBasic.Interaction.InputBox(
                $"Nhập ký tự ổ đĩa (A-Z) cho phân vùng:\n{part.Name} ({part.SizeBytes/1024/1024/1024} GB)",
                "Gán ký tự ổ đĩa", "");
            if (string.IsNullOrEmpty(letter) || letter.Length < 1) return;

            letter = letter[0].ToString().ToUpper();
            if (!char.IsLetter(letter[0])) { MessageBox.Show("Ký tự không hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            var script = $"select disk {part.DiskIndex}\nselect partition {GetPartNum(part.Name)}\nassign letter={letter}\nexit\n";
            AppendOut($"▶ Gán ký tự {letter}: cho {part.Name}...\n", Theme.TextWarning);
            var result = ProcessRunner.RunDiskPart(script);
            AppendOut(result.Success ? $"✅ Đã gán ký tự {letter}:\n" : $"❌ Thất bại: {result.Error}\n",
                result.Success ? Theme.TextSuccess : Theme.TextError);
            LoadDisksAsync();
        }

        private void MountVolume()
        {
            var part = GetSelectedPartition();
            if (part == null) return;
            if (!string.IsNullOrEmpty(part.DriveLetter))
            {
                MessageBox.Show($"Phân vùng đã có ký tự: {part.DriveLetter}", "WinPE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            AssignDriveLetter();
        }

        private void FormatPartitionConfirm()
        {
            var part = GetSelectedPartition();
            if (part == null) return;

            // SAFETY DIALOG
            var msg = $"⚠️  CẢNH BÁO — THAO TÁC PHÁ HỦY DỮ LIỆU\n\n" +
                      $"Bạn sắp FORMAT:\n" +
                      $"  Ổ đĩa    : Disk {part.DiskIndex}\n" +
                      $"  Phân vùng: {part.Name}\n" +
                      $"  Ký tự    : {(string.IsNullOrEmpty(part.DriveLetter) ? "(chưa gán)" : part.DriveLetter)}\n" +
                      $"  File System: {part.FileSystem}\n" +
                      $"  Kích thước: {part.SizeBytes/1024/1024/1024} GB\n\n" +
                      $"Toàn bộ dữ liệu sẽ bị XÓA VĨNH VIỄN.\n" +
                      $"Bạn có chắc chắn muốn tiếp tục không?";

            var confirm = MessageBox.Show(msg, "XÁC NHẬN FORMAT", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes) { AppendOut("⛔ Format bị hủy bởi người dùng.\n", Theme.TextMuted); return; }

            // Double confirm
            var typed = Microsoft.VisualBasic.Interaction.InputBox(
                "Nhập 'CONFIRM' (chữ hoa) để xác nhận:", "Xác nhận lần 2", "");
            if (typed != "CONFIRM") { AppendOut("⛔ Format bị hủy — Không xác nhận đúng.\n", Theme.TextMuted); return; }

            var fsDialog = new Form { Text = "Chọn File System", Width = 280, Height = 150, StartPosition = FormStartPosition.CenterParent, BackColor = Theme.BgCard };
            var fsCb = new ComboBox { Items = { "NTFS", "FAT32", "exFAT" }, SelectedIndex = 0, Location = new Point(20, 30), Width = 200 };
            var ok = new Button { Text = "Format", Location = new Point(20, 70), Width = 100 };
            var cancel = new Button { Text = "Hủy", Location = new Point(130, 70), Width = 100 };
            ok.Click += (s, e) => fsDialog.DialogResult = DialogResult.OK;
            cancel.Click += (s, e) => fsDialog.DialogResult = DialogResult.Cancel;
            fsDialog.Controls.AddRange(new Control[] { fsCb, ok, cancel });

            if (fsDialog.ShowDialog() != DialogResult.OK) { AppendOut("⛔ Format bị hủy.\n", Theme.TextMuted); return; }
            var fs = fsCb.SelectedItem.ToString();

            AppendOut($"▶ Formatting {part.Name} as {fs}...\n", Theme.TextWarning);
            Logger.Warn($"FORMAT: Disk {part.DiskIndex} Partition {GetPartNum(part.Name)} as {fs}");

            var script = $"select disk {part.DiskIndex}\nselect partition {GetPartNum(part.Name)}\nformat fs={fs.ToLower()} quick\nexit\n";
            var result = ProcessRunner.RunDiskPart(script, 120000);
            AppendOut(result.Success ? $"✅ Format hoàn thành ({fs})\n" : $"❌ Format thất bại: {result.Error}\n",
                result.Success ? Theme.TextSuccess : Theme.TextError);
            LoadDisksAsync();
        }

        private void DeletePartitionConfirm()
        {
            var part = GetSelectedPartition();
            if (part == null) return;

            var msg = $"🚨  CẢNH BÁO CỰC KỲ NGUY HIỂM\n\n" +
                      $"Bạn sắp XÓA PHÂN VÙNG:\n" +
                      $"  Ổ đĩa    : Disk {part.DiskIndex}\n" +
                      $"  Phân vùng: {part.Name}\n" +
                      $"  Ký tự    : {(string.IsNullOrEmpty(part.DriveLetter) ? "(không có)" : part.DriveLetter)}\n" +
                      $"  File System: {part.FileSystem}\n" +
                      $"  Kích thước: {part.SizeBytes/1024/1024/1024} GB\n\n" +
                      $"⚠️  DỮ LIỆU KHÔNG THỂ PHỤC HỒI sau thao tác này!\n\n" +
                      $"Bạn có CHẮC CHẮN muốn XÓA không?";

            var confirm = MessageBox.Show(msg, "XÁC NHẬN XÓA PHÂN VÙNG", MessageBoxButtons.YesNo, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes) { AppendOut("⛔ Xóa phân vùng bị hủy.\n", Theme.TextMuted); return; }

            var typed = Microsoft.VisualBasic.Interaction.InputBox("Nhập 'DELETE' (chữ hoa) để xác nhận:", "Xác nhận lần 2", "");
            if (typed != "DELETE") { AppendOut("⛔ Xóa phân vùng bị hủy — Không xác nhận.\n", Theme.TextMuted); return; }

            AppendOut($"▶ Xóa phân vùng: {part.Name}...\n", Theme.TextWarning);
            Logger.Warn($"DELETE PARTITION: Disk {part.DiskIndex} Partition {GetPartNum(part.Name)}");

            var script = $"select disk {part.DiskIndex}\nselect partition {GetPartNum(part.Name)}\ndelete partition override\nexit\n";
            var result = ProcessRunner.RunDiskPart(script);
            AppendOut(result.Success ? "✅ Đã xóa phân vùng.\n" : $"❌ Xóa thất bại: {result.Error}\n",
                result.Success ? Theme.TextSuccess : Theme.TextError);
            LoadDisksAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────
        private Button MakeBtn(string text, Action click, Color fg)
        {
            var btn = new Button
            {
                Text      = text,
                Font      = Theme.FontSmall,
                ForeColor = fg,
                BackColor = Theme.BgCard,
                FlatStyle = FlatStyle.Flat,
                AutoSize  = true,
                Margin    = new Padding(0, 0, 6, 0),
                Cursor    = Cursors.Hand,
                Padding   = new Padding(8, 2, 8, 2),
            };
            btn.FlatAppearance.BorderColor = Theme.Border;
            btn.Click += (s, e) => { try { click(); } catch (Exception ex) { AppendOut($"❌ {ex.Message}\n", Theme.TextError); } };
            btn.MouseEnter += (s, e) => btn.BackColor = Theme.BgHover;
            btn.MouseLeave += (s, e) => btn.BackColor = Theme.BgCard;
            return btn;
        }

        private ListView MakeListView()
        {
            return new ListView
            {
                Dock           = DockStyle.Fill,
                View           = View.Details,
                FullRowSelect  = true,
                GridLines      = false,
                BackColor      = Theme.BgCard,
                ForeColor      = Theme.TextPrimary,
                Font           = Theme.FontBody,
                BorderStyle    = BorderStyle.None,
                MultiSelect    = false,
                HeaderStyle    = ColumnHeaderStyle.Nonclickable,
            };
        }

        private void AppendOut(string text, Color color)
        {
            SafeUpdate(() =>
            {
                _output.SelectionStart  = _output.TextLength;
                _output.SelectionColor  = color;
                _output.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}");
                _output.ScrollToCaret();
            });
        }

        private void SafeUpdate(Action action)
        {
            if (IsDisposed) return;
            try { if (InvokeRequired) Invoke(action); else action(); }
            catch { }
        }

        private int GetPartNum(string partName)
        {
            // "Disk #0, Partition #2" → 2
            var parts = partName.Split('#');
            if (parts.Length >= 2 && int.TryParse(parts[parts.Length - 1], out int n)) return n;
            return 1;
        }

        // ─────────────────────────────────────────────────────────────────────
        // DATA CLASSES
        // ─────────────────────────────────────────────────────────────────────
        private class DiskInfo
        {
            public int    Index      { get; set; }
            public string Model      { get; set; }
            public long   SizeBytes  { get; set; }
            public string Interface  { get; set; }
            public string MediaType  { get; set; }
            public int    Partitions { get; set; }
            public string Serial     { get; set; }
        }

        private class PartitionInfo
        {
            public int    DiskIndex   { get; set; }
            public string Name        { get; set; }
            public string DeviceId    { get; set; }
            public string DriveLetter { get; set; }
            public string FileSystem  { get; set; }
            public string Type        { get; set; }
            public string Label       { get; set; }
            public long   SizeBytes   { get; set; }
            public long   FreeBytes   { get; set; }
        }
    }
}
