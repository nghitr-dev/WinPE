using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WinPETool.Core;
using WinPETool.UI;

namespace WinPETool.UI.Pages
{
    /// <summary>
    /// File Manager Page — Trình duyệt và quản lý file offline trong WinPE
    /// Cho phép duyệt file trên tất cả các ổ đĩa, copy/paste, xóa, mở file, chỉnh sửa bằng Notepad.
    /// </summary>
    public class FileManagerPage : Panel
    {
        private readonly AppConfig _config;

        private ComboBox _driveCombo;
        private TextBox _pathBox;
        private ListView _fileListView;
        private TreeView _dirTree;
        private Label _statusLabel;
        private string _clipboardPath = "";
        private bool _isCut = false;

        public FileManagerPage(AppConfig config)
        {
            _config = config;
            BackColor = Theme.BgDark;
            Dock = DockStyle.Fill;
            Padding = new Padding(12);

            BuildUI();
            LoadDrives();
        }

        private void BuildUI()
        {
            // Header
            var header = new Panel { Dock = DockStyle.Top, Height = 42 };
            var bar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Theme.CatFiles };
            var lbl = new Label
            {
                Text = "📁  Quản lý File & Thư mục (Offline File Explorer)",
                Font = Theme.FontTitle,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(14, 8)
            };
            header.Controls.Add(bar);
            header.Controls.Add(lbl);
            Controls.Add(header);

            // Toolbar
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.BgToolbar, Padding = new Padding(6) };

            var driveLbl = new Label { Text = "Ổ đĩa:", ForeColor = Theme.TextSecondary, Font = Theme.FontSmall, AutoSize = true, Location = new Point(8, 12) };
            _driveCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontBody,
                Width = 120,
                Location = new Point(50, 8)
            };
            _driveCombo.SelectedIndexChanged += (s, e) =>
            {
                if (_driveCombo.SelectedItem is string drive)
                {
                    NavigateTo(drive);
                }
            };

            var btnUp = MakeBtn("⬆ Lên", () => GoUp(), Theme.BgCard);
            btnUp.Location = new Point(180, 7);

            var btnRefresh = MakeBtn("🔄 Tải lại", () => RefreshCurrent(), Theme.BgCard);
            btnRefresh.Location = new Point(245, 7);

            _pathBox = new TextBox
            {
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontBody,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(325, 8),
                Width = 380,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _pathBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    NavigateTo(_pathBox.Text.Trim());
                    e.SuppressKeyPress = true;
                }
            };

            var btnGo = MakeBtn("Đi ➡", () => NavigateTo(_pathBox.Text.Trim()), Theme.AccentDark);
            btnGo.Location = new Point(715, 7);
            btnGo.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var btnOpenCmd = MakeBtn("💻 CMD tại đây", () => OpenCmdHere(), Theme.BgCard);
            btnOpenCmd.Location = new Point(780, 7);
            btnOpenCmd.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var btnNotepad = MakeBtn("📝 Notepad", () => OpenWithNotepad(), Theme.BgCard);
            btnNotepad.Location = new Point(885, 7);
            btnNotepad.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            toolbar.Controls.AddRange(new Control[] { driveLbl, _driveCombo, btnUp, btnRefresh, _pathBox, btnGo, btnOpenCmd, btnNotepad });
            Controls.Add(toolbar);

            // Action strip for file operations
            var actionStrip = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Theme.BgPanel, Padding = new Padding(6) };
            var btnNewFolder = MakeBtn("➕ Thư mục mới", () => CreateNewFolder(), Theme.BgCard);
            btnNewFolder.Location = new Point(8, 4);

            var btnCopy = MakeBtn("📋 Copy", () => CopySelected(false), Theme.BgCard);
            btnCopy.Location = new Point(130, 4);

            var btnCut = MakeBtn("✂ Cắt", () => CopySelected(true), Theme.BgCard);
            btnCut.Location = new Point(195, 4);

            var btnPaste = MakeBtn("📥 Dán", () => PasteClipboard(), Theme.BgCard);
            btnPaste.Location = new Point(255, 4);

            var btnDelete = MakeBtn("🗑 Xóa", () => DeleteSelected(), Color.FromArgb(120, 40, 40));
            btnDelete.Location = new Point(315, 4);

            var btnRename = MakeBtn("✏ Đổi tên", () => RenameSelected(), Theme.BgCard);
            btnRename.Location = new Point(375, 4);

            // Shortcuts to common Windows folders
            var lblShortcuts = new Label { Text = "Đến nhanh:", ForeColor = Theme.TextMuted, Font = Theme.FontSmall, AutoSize = true, Location = new Point(460, 9) };
            var btnWinDir = MakeBtn("🪟 Windows", () => GoToWindowsShortcut(), Theme.BgCard);
            btnWinDir.Location = new Point(530, 4);

            var btnUsers = MakeBtn("👤 Users", () => GoToUsersShortcut(), Theme.BgCard);
            btnUsers.Location = new Point(625, 4);

            var btnSystem32 = MakeBtn("⚙ System32", () => GoToSystem32Shortcut(), Theme.BgCard);
            btnSystem32.Location = new Point(695, 4);

            actionStrip.Controls.AddRange(new Control[] { btnNewFolder, btnCopy, btnCut, btnPaste, btnDelete, btnRename, lblShortcuts, btnWinDir, btnUsers, btnSystem32 });
            Controls.Add(actionStrip);

            // Status bar at bottom
            var statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 26, BackColor = Theme.BgToolbar, Padding = new Padding(6, 4, 6, 4) };
            _statusLabel = new Label { Text = "Sẵn sàng", Font = Theme.FontSmall, ForeColor = Theme.TextSecondary, AutoSize = true, Location = new Point(8, 5) };
            statusPanel.Controls.Add(_statusLabel);
            Controls.Add(statusPanel);

            // Main Split container: Left TreeView, Right ListView
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 220,
                BackColor = Theme.Border
            };

            _dirTree = new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgPanel,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontBody,
                BorderStyle = BorderStyle.None,
                ShowLines = true,
                ShowPlusMinus = true
            };
            _dirTree.BeforeExpand += (s, e) => TreeBeforeExpand(e.Node);
            _dirTree.AfterSelect += (s, e) =>
            {
                if (e.Node?.Tag is string path && Directory.Exists(path))
                {
                    NavigateTo(path, false);
                }
            };
            split.Panel1.Controls.Add(_dirTree);

            _fileListView = new ListView
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgDark,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontBody,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = true,
                BorderStyle = BorderStyle.None
            };
            _fileListView.Columns.Add("Tên", 320);
            _fileListView.Columns.Add("Kích thước", 110, HorizontalAlignment.Right);
            _fileListView.Columns.Add("Loại", 120);
            _fileListView.Columns.Add("Ngày sửa đổi", 150);

            _fileListView.ItemActivate += (s, e) => OpenSelectedItem();

            // Context menu for listview
            var cm = new ContextMenuStrip();
            cm.Items.Add("Mở / Thực thi", null, (s, e) => OpenSelectedItem());
            cm.Items.Add("Chỉnh sửa bằng Notepad", null, (s, e) => OpenWithNotepad());
            cm.Items.Add(new ToolStripSeparator());
            cm.Items.Add("Sao chép", null, (s, e) => CopySelected(false));
            cm.Items.Add("Cắt", null, (s, e) => CopySelected(true));
            cm.Items.Add("Dán vào thư mục này", null, (s, e) => PasteClipboard());
            cm.Items.Add(new ToolStripSeparator());
            cm.Items.Add("Đổi tên", null, (s, e) => RenameSelected());
            cm.Items.Add("Xóa", null, (s, e) => DeleteSelected());
            _fileListView.ContextMenuStrip = cm;

            split.Panel2.Controls.Add(_fileListView);
            Controls.Add(split);

            // Reorder controls for proper docking
            header.SendToBack();
            toolbar.SendToBack();
            actionStrip.SendToBack();
            statusPanel.SendToBack();
            split.BringToFront();
        }

        private void LoadDrives()
        {
            _driveCombo.Items.Clear();
            _dirTree.Nodes.Clear();

            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        var label = $"{drive.Name} ({FormatSize(drive.TotalFreeSpace)} trống / {FormatSize(drive.TotalSize)})";
                        _driveCombo.Items.Add(drive.Name);

                        var node = new TreeNode($"{drive.Name} [{drive.DriveType}]")
                        {
                            Tag = drive.RootDirectory.FullName
                        };
                        node.Nodes.Add(new TreeNode("Loading..."));
                        _dirTree.Nodes.Add(node);
                    }
                }

                if (_driveCombo.Items.Count > 0)
                {
                    _driveCombo.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"FileManager: LoadDrives error: {ex.Message}");
            }
        }

        private void TreeBeforeExpand(TreeNode node)
        {
            if (node.Tag is string path && Directory.Exists(path))
            {
                node.Nodes.Clear();
                try
                {
                    var dirs = Directory.GetDirectories(path);
                    foreach (var d in dirs)
                    {
                        var subNode = new TreeNode(Path.GetFileName(d)) { Tag = d };
                        try
                        {
                            if (Directory.GetDirectories(d).Length > 0)
                                subNode.Nodes.Add(new TreeNode("Loading..."));
                        }
                        catch { }
                        node.Nodes.Add(subNode);
                    }
                }
                catch { }
            }
        }

        public void NavigateTo(string path, bool updateTree = true)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (!Directory.Exists(path))
            {
                if (File.Exists(path))
                {
                    OpenFile(path);
                    return;
                }
                MessageBox.Show($"Thư mục không tồn tại:\n{path}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _pathBox.Text = path;
                _fileListView.Items.Clear();

                var dirInfo = new DirectoryInfo(path);

                // Add parent directory item if not root
                if (dirInfo.Parent != null)
                {
                    var upItem = new ListViewItem(new[] { ".. (Lên thư mục cha)", "", "Thư mục", "" })
                    {
                        Tag = dirInfo.Parent.FullName,
                        ForeColor = Theme.Accent
                    };
                    _fileListView.Items.Add(upItem);
                }

                int dirCount = 0;
                int fileCount = 0;
                long totalBytes = 0;

                // Add Directories
                foreach (var d in dirInfo.GetDirectories())
                {
                    var item = new ListViewItem(new[]
                    {
                        "📁 " + d.Name,
                        "",
                        "Thư mục",
                        d.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
                    })
                    {
                        Tag = d.FullName,
                        ForeColor = Theme.TextPrimary
                    };
                    _fileListView.Items.Add(item);
                    dirCount++;
                }

                // Add Files
                foreach (var f in dirInfo.GetFiles())
                {
                    var item = new ListViewItem(new[]
                    {
                        "📄 " + f.Name,
                        FormatSize(f.Length),
                        f.Extension.ToUpperInvariant() + " File",
                        f.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
                    })
                    {
                        Tag = f.FullName,
                        ForeColor = Theme.TextSecondary
                    };
                    _fileListView.Items.Add(item);
                    fileCount++;
                    totalBytes += f.Length;
                }

                _statusLabel.Text = $"{dirCount} thư mục, {fileCount} files ({FormatSize(totalBytes)}) | Vị trí: {path}";
            }
            catch (UnauthorizedAccessException)
            {
                _statusLabel.Text = "⚠️ Không có quyền truy cập thư mục này";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Lỗi: {ex.Message}";
            }
        }

        private void GoUp()
        {
            var cur = _pathBox.Text.Trim();
            if (Directory.Exists(cur))
            {
                var parent = Directory.GetParent(cur);
                if (parent != null) NavigateTo(parent.FullName);
            }
        }

        private void RefreshCurrent()
        {
            NavigateTo(_pathBox.Text.Trim());
        }

        private void OpenSelectedItem()
        {
            if (_fileListView.SelectedItems.Count == 0) return;
            var path = _fileListView.SelectedItems[0].Tag as string;
            if (string.IsNullOrEmpty(path)) return;

            if (Directory.Exists(path))
            {
                NavigateTo(path);
            }
            else if (File.Exists(path))
            {
                OpenFile(path);
            }
        }

        private void OpenFile(string filePath)
        {
            try
            {
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext == ".txt" || ext == ".log" || ext == ".ini" || ext == ".inf" || ext == ".cfg" || ext == ".xml" || ext == ".json" || ext == ".bat" || ext == ".cmd" || ext == ".ps1")
                {
                    Process.Start("notepad.exe", $"\"{filePath}\"");
                }
                else if (ext == ".exe")
                {
                    if (MessageBox.Show($"Chạy chương trình này?\n{filePath}", "Xác nhận thực thi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        Process.Start(filePath);
                    }
                }
                else
                {
                    Process.Start("notepad.exe", $"\"{filePath}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở file: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenWithNotepad()
        {
            if (_fileListView.SelectedItems.Count == 0) return;
            var path = _fileListView.SelectedItems[0].Tag as string;
            if (File.Exists(path))
            {
                Process.Start("notepad.exe", $"\"{path}\"");
            }
        }

        private void OpenCmdHere()
        {
            var cur = _pathBox.Text.Trim();
            if (Directory.Exists(cur))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = cur
                });
            }
        }

        private void CopySelected(bool isCut)
        {
            if (_fileListView.SelectedItems.Count == 0) return;
            var path = _fileListView.SelectedItems[0].Tag as string;
            if (File.Exists(path) || Directory.Exists(path))
            {
                _clipboardPath = path;
                _isCut = isCut;
                _statusLabel.Text = $"Đã {(isCut ? "cắt" : "copy")}: {Path.GetFileName(path)}";
            }
        }

        private void PasteClipboard()
        {
            if (string.IsNullOrEmpty(_clipboardPath)) return;
            var targetDir = _pathBox.Text.Trim();
            if (!Directory.Exists(targetDir)) return;

            try
            {
                var name = Path.GetFileName(_clipboardPath);
                var dest = Path.Combine(targetDir, name);

                if (File.Exists(_clipboardPath))
                {
                    if (_isCut)
                    {
                        File.Move(_clipboardPath, dest);
                        _clipboardPath = "";
                    }
                    else
                    {
                        File.Copy(_clipboardPath, dest, true);
                    }
                }
                else if (Directory.Exists(_clipboardPath))
                {
                    CopyDirectory(_clipboardPath, dest);
                    if (_isCut)
                    {
                        Directory.Delete(_clipboardPath, true);
                        _clipboardPath = "";
                    }
                }

                RefreshCurrent();
                _statusLabel.Text = $"✅ Đã dán: {name}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi dán: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteSelected()
        {
            if (_fileListView.SelectedItems.Count == 0) return;
            var path = _fileListView.SelectedItems[0].Tag as string;
            if (string.IsNullOrEmpty(path)) return;

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa vĩnh viễn?\n{path}", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                RefreshCurrent();
                _statusLabel.Text = $"✅ Đã xóa: {Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xóa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenameSelected()
        {
            if (_fileListView.SelectedItems.Count == 0) return;
            var path = _fileListView.SelectedItems[0].Tag as string;
            if (string.IsNullOrEmpty(path)) return;

            var oldName = Path.GetFileName(path);
            var newName = PromptInput("Đổi tên", "Nhập tên mới:", oldName);
            if (string.IsNullOrWhiteSpace(newName) || newName == oldName) return;

            try
            {
                var dir = Path.GetDirectoryName(path);
                var newPath = Path.Combine(dir, newName);

                if (File.Exists(path))
                    File.Move(path, newPath);
                else if (Directory.Exists(path))
                    Directory.Move(path, newPath);

                RefreshCurrent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đổi tên: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateNewFolder()
        {
            var cur = _pathBox.Text.Trim();
            if (!Directory.Exists(cur)) return;

            var name = PromptInput("Tạo thư mục mới", "Tên thư mục:", "NewFolder");
            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                Directory.CreateDirectory(Path.Combine(cur, name));
                RefreshCurrent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo thư mục: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GoToWindowsShortcut()
        {
            var installs = WindowsFinder.FindInstallations();
            if (installs.Count > 0)
                NavigateTo(installs[0].WindowsDirectory);
            else
                NavigateTo(@"C:\Windows");
        }

        private void GoToUsersShortcut()
        {
            var installs = WindowsFinder.FindInstallations();
            if (installs.Count > 0)
                NavigateTo(Path.Combine(installs[0].SystemDrive, "Users"));
            else
                NavigateTo(@"C:\Users");
        }

        private void GoToSystem32Shortcut()
        {
            var installs = WindowsFinder.FindInstallations();
            if (installs.Count > 0)
                NavigateTo(Path.Combine(installs[0].WindowsDirectory, "System32"));
            else
                NavigateTo(@"C:\Windows\System32");
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            var dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            foreach (var file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            foreach (var subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }

        private static string PromptInput(string title, string prompt, string defaultValue)
        {
            using var form = new Form
            {
                Width = 400,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Theme.BgPanel,
                ForeColor = Theme.TextPrimary
            };
            var textLabel = new Label { Left = 20, Top = 15, Text = prompt, AutoSize = true, ForeColor = Theme.TextPrimary };
            var textBox = new TextBox { Left = 20, Top = 40, Width = 340, Text = defaultValue, BackColor = Theme.BgInput, ForeColor = Theme.TextPrimary };
            var confirmation = new Button { Text = "OK", Left = 180, Width = 80, Top = 80, DialogResult = DialogResult.OK, BackColor = Theme.AccentDark, ForeColor = Theme.TextPrimary, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "Hủy", Left = 280, Width = 80, Top = 80, DialogResult = DialogResult.Cancel, BackColor = Theme.BgCard, ForeColor = Theme.TextPrimary, FlatStyle = FlatStyle.Flat };

            confirmation.Click += (sender, e) => form.Close();
            cancel.Click += (sender, e) => form.Close();

            form.Controls.Add(textLabel);
            form.Controls.Add(textBox);
            form.Controls.Add(confirmation);
            form.Controls.Add(cancel);
            form.AcceptButton = confirmation;
            form.CancelButton = cancel;

            return form.ShowDialog() == DialogResult.OK ? textBox.Text : "";
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

        private static string FormatSize(long bytes)
        {
            if (bytes >= 1073741824) return $"{bytes / 1073741824.0:F1} GB";
            if (bytes >= 1048576) return $"{bytes / 1048576.0:F1} MB";
            if (bytes >= 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes} B";
        }
    }
}
