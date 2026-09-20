using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WinPETool.Core;
using WinPETool.UI;
using WinPETool.UI.Pages;

namespace WinPETool
{
    /// <summary>
    /// Main application window — WinPE Nghitr Dev
    /// Layout: Sidebar (trái) + Content area (phải) + Status bar (dưới)
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly AppConfig _config;

        // Layout panels
        private Panel _sidebar;
        private Panel _topBar;
        private Panel _contentArea;
        private Panel _statusBar;
        private Panel _navContainer;

        // Current page
        private Panel _currentPage;
        private string _currentPageId = "";

        // Log panel (bottom of content)
        private RichTextBox _logBox;
        private Panel _logPanel;
        private bool _logPanelVisible = false;

        // Status labels
        private Label _statusLabel;
        private Label _statusTime;

        // Nav items tracking
        private readonly Dictionary<string, Panel> _navItems = new Dictionary<string, Panel>();
        private Panel _selectedNavItem;

        public MainForm(AppConfig config, string initialPage = "dashboard")
        {
            _config = config;
            InitializeUI();
            SetupLogger();
            NavigateTo(string.IsNullOrWhiteSpace(initialPage) ? "dashboard" : initialPage);
        }

        private void InitializeUI()
        {
            // ─── Form setup ─────────────────────────────
            this.Text            = _config.WindowTitle;
            this.Size            = new Size(1280, 780);
            this.MinimumSize     = new Size(1024, 660);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.BackColor       = Theme.BgDark;
            this.ForeColor       = Theme.TextPrimary;
            this.Font            = Theme.FontBody;
            this.Icon            = null; // Set icon if file exists
            this.DoubleBuffered  = true;

            BuildTopBar();
            BuildSidebar();
            BuildContentArea();
            BuildStatusBar();

            // Layout anchoring
            _topBar.Dock      = DockStyle.Top;
            _sidebar.Dock     = DockStyle.Left;
            _statusBar.Dock   = DockStyle.Bottom;
            _contentArea.Dock = DockStyle.Fill;

            this.Controls.Add(_contentArea);
            this.Controls.Add(_sidebar);
            this.Controls.Add(_topBar);
            this.Controls.Add(_statusBar);

            // Window events
            this.Resize     += (s, e) => UpdateLayout();
            this.FormClosed += (s, e) => Logger.Info("Application closed.");
        }

        // ─────────────────────────────────────────────────────────────
        // TOP BAR
        // ─────────────────────────────────────────────────────────────
        private void BuildTopBar()
        {
            _topBar = new Panel
            {
                Height    = Theme.ToolbarHeight,
                BackColor = Theme.BgToolbar,
                Padding   = new Padding(16, 0, 16, 0),
            };

            // App title
            var titleLbl = new Label
            {
                Text      = "WinPE Nghitr Dev",
                Font      = Theme.FontTitle,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(16, 14),
            };

            // Version badge
            var verBadge = new Label
            {
                Text      = $"v{_config.Version}",
                Font      = Theme.FontSmall,
                ForeColor = Theme.BgDark,
                BackColor = Theme.Accent,
                AutoSize  = true,
                Location  = new Point(210, 17),
                Padding   = new Padding(6, 2, 6, 2),
            };

            // Clock (top right)
            _statusTime = new Label
            {
                Text      = DateTime.Now.ToString("HH:mm:ss"),
                Font      = Theme.FontBody,
                ForeColor = Theme.TextSecondary,
                AutoSize  = true,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            };

            // Log toggle button
            var logBtn = MakeTopButton("📋 Log", () => ToggleLogPanel());
            logBtn.Anchor   = AnchorStyles.Top | AnchorStyles.Right;

            // Shutdown buttons
            var rebootBtn   = MakeTopButton("↺ Khởi động lại", () => ConfirmAction("Khởi động lại máy?", () => ProcessRunner.RunCmd("shutdown /r /t 0")));
            var shutdownBtn = MakeTopButton("⏻ Tắt máy", () => ConfirmAction("Tắt máy?", () => ProcessRunner.RunCmd("shutdown /s /t 0")));

            rebootBtn.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
            shutdownBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            _topBar.Controls.AddRange(new Control[] { titleLbl, verBadge });

            // Right-side controls — positioned in UpdateLayout
            _topBar.Controls.Add(_statusTime);
            _topBar.Controls.Add(logBtn);
            _topBar.Controls.Add(rebootBtn);
            _topBar.Controls.Add(shutdownBtn);

            // Clock timer
            var timer = new Timer { Interval = 1000 };
            timer.Tick += (s, e) => { if (_statusTime != null) _statusTime.Text = DateTime.Now.ToString("HH:mm:ss"); };
            timer.Start();

            // Position right-side controls
            _topBar.Resize += (s, e) =>
            {
                int x = _topBar.Width - 16;
                foreach (Control c in new Control[] { _statusTime, logBtn, shutdownBtn, rebootBtn })
                {
                    c.Location = new Point(x - c.Width - 4, (_topBar.Height - c.Height) / 2);
                    x -= c.Width + 8;
                }
            };
        }

        private Button MakeTopButton(string text, Action onClick)
        {
            var btn = new Button
            {
                Text      = text,
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextSecondary,
                BackColor = Theme.BgCard,
                FlatStyle = FlatStyle.Flat,
                AutoSize  = true,
                Padding   = new Padding(8, 4, 8, 4),
                Cursor    = Cursors.Hand,
            };
            btn.FlatAppearance.BorderColor = Theme.Border;
            btn.FlatAppearance.BorderSize  = 1;
            btn.Click += (s, e) => onClick();
            btn.MouseEnter += (s, e) => btn.BackColor = Theme.BgHover;
            btn.MouseLeave += (s, e) => btn.BackColor = Theme.BgCard;
            return btn;
        }

        // ─────────────────────────────────────────────────────────────
        // SIDEBAR
        // ─────────────────────────────────────────────────────────────
        private void BuildSidebar()
        {
            _sidebar = new Panel
            {
                Width     = Theme.SidebarWidth,
                BackColor = Theme.BgPanel,
            };

            // Brand area at top
            var brand = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 56,
                BackColor = Theme.BgToolbar,
                Padding   = new Padding(16, 0, 0, 0),
            };
            var brandLbl = new Label
            {
                Text      = "⚙  MENU CHÍNH",
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                AutoSize  = true,
                Location  = new Point(16, 20),
            };
            brand.Controls.Add(brandLbl);

            // Nav scroll container
            _navContainer = new Panel
            {
                Dock        = DockStyle.Fill,
                AutoScroll  = true,
                BackColor   = Theme.BgPanel,
                Padding     = new Padding(8, 8, 8, 8),
            };

            _sidebar.Controls.Add(_navContainer);
            _sidebar.Controls.Add(brand);

            BuildNavItems();
        }

        private void BuildNavItems()
        {
            // Navigation groups definition
            var navGroups = new[]
            {
                new NavGroup("PHỤC HỒI WINDOWS", new[]
                {
                    new NavItem("dashboard",    "🏠", "Dashboard",           Theme.CatSystem),
                    new NavItem("recovery",     "🔧", "Phục hồi Windows",     Theme.CatRecovery),
                    new NavItem("bcd",          "💾", "Sửa BCD / Bootloader", Theme.CatRecovery),
                }),
                new NavGroup("Ổ ĐĨA & PHÂN VÙNG", new[]
                {
                    new NavItem("disk",         "💿", "Quản lý ổ đĩa",        Theme.CatDisk),
                    new NavItem("filemanager",  "📁", "File Manager",          Theme.CatFiles),
                    new NavItem("backup",       "📦", "Backup & Restore",      Theme.CatBackup),
                }),
                new NavGroup("PHẦN CỨNG & HỆ THỐNG", new[]
                {
                    new NavItem("hardware",     "🖥️", "Phần cứng",            Theme.CatHardware),
                    new NavItem("drivers",      "🔌", "Driver",               Theme.CatDrivers),
                    new NavItem("network",      "🌐", "Mạng",                 Theme.CatNetwork),
                }),
                new NavGroup("BẢO MẬT & TÀI KHOẢN", new[]
                {
                    new NavItem("security",     "🛡️", "Bảo mật / Quét virus", Theme.CatSecurity),
                    new NavItem("account",      "👤", "Tài khoản Windows",     Theme.CatSecurity),
                }),
                new NavGroup("CÔNG CỤ KHÁC", new[]
                {
                    new NavItem("systemtools",  "🔩", "Công cụ hệ thống",     Theme.CatSystem),
                    new NavItem("remote",       "📡", "Hỗ trợ từ xa",         Theme.CatNetwork),
                    new NavItem("logs",         "📋", "Nhật ký / Logs",        Theme.CatSettings),
                    new NavItem("settings",     "⚙️",  "Cài đặt",              Theme.CatSettings),
                }),
            };

            int y = 0;
            foreach (var group in navGroups)
            {
                // Group label
                var groupLbl = new Label
                {
                    Text      = group.Title,
                    Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = Theme.TextMuted,
                    AutoSize  = false,
                    Width     = Theme.SidebarWidth - 32,
                    Height    = 24,
                    Location  = new Point(8, y),
                    Padding   = new Padding(4, 6, 0, 0),
                };
                _navContainer.Controls.Add(groupLbl);
                y += 28;

                foreach (var item in group.Items)
                {
                    var navPanel = CreateNavItem(item, y);
                    _navContainer.Controls.Add(navPanel);
                    _navItems[item.Id] = navPanel;
                    y += Theme.NavItemHeight + 2;
                }

                y += 6; // Group spacing
            }

            _navContainer.AutoScrollMinSize = new Size(0, y + 20);
        }

        private Panel CreateNavItem(NavItem item, int y)
        {
            var panel = new Panel
            {
                Width     = Theme.SidebarWidth - 32,
                Height    = Theme.NavItemHeight,
                Location  = new Point(8, y),
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand,
                Tag       = item.Id,
            };

            // Left accent bar (hidden by default)
            var accentBar = new Panel
            {
                Width     = 3,
                Height    = panel.Height,
                Location  = new Point(0, 0),
                BackColor = item.Color,
                Visible   = false,
            };

            // Icon label
            var iconLbl = new Label
            {
                Text      = item.Icon,
                Font      = new Font("Segoe UI Emoji", 13f),
                ForeColor = Theme.TextSecondary,
                AutoSize  = false,
                Width     = 36,
                Height    = panel.Height,
                Location  = new Point(8, 0),
                TextAlign = ContentAlignment.MiddleCenter,
            };

            // Name label
            var nameLbl = new Label
            {
                Text      = item.Name,
                Font      = Theme.FontNav,
                ForeColor = Theme.TextSecondary,
                AutoSize  = false,
                Width     = panel.Width - 50,
                Height    = panel.Height,
                Location  = new Point(46, 0),
                TextAlign = ContentAlignment.MiddleLeft,
            };

            panel.Controls.AddRange(new Control[] { accentBar, iconLbl, nameLbl });

            // Hover + click events
            Action highlight = () =>
            {
                panel.BackColor = Theme.BgHover;
                iconLbl.ForeColor = item.Color;
                nameLbl.ForeColor = Theme.TextPrimary;
                accentBar.Visible = true;
            };
            Action unhighlight = () =>
            {
                if (_selectedNavItem == panel) return; // Keep selected state
                panel.BackColor = Color.Transparent;
                iconLbl.ForeColor = Theme.TextSecondary;
                nameLbl.ForeColor = Theme.TextSecondary;
                accentBar.Visible = false;
            };
            Action select = () =>
            {
                SelectNavItem(panel, iconLbl, nameLbl, accentBar, item.Color);
                NavigateTo(item.Id);
            };

            foreach (Control c in panel.Controls)
            {
                c.MouseEnter += (s, e) => highlight();
                c.MouseLeave += (s, e) => unhighlight();
                c.Click      += (s, e) => select();
            }
            panel.MouseEnter += (s, e) => highlight();
            panel.MouseLeave += (s, e) => unhighlight();
            panel.Click      += (s, e) => select();

            return panel;
        }

        private void SelectNavItem(Panel panel, Label icon, Label name, Panel bar, Color color)
        {
            // Deselect previous
            if (_selectedNavItem != null)
            {
                _selectedNavItem.BackColor = Color.Transparent;
                foreach (Control c in _selectedNavItem.Controls)
                {
                    if (c is Label l && l.Font == Theme.FontNavSel) l.Font = Theme.FontNav;
                    if (c is Label lbl) lbl.ForeColor = Theme.TextSecondary;
                    if (c is Panel p) p.Visible = false;
                }
            }

            // Select new
            _selectedNavItem  = panel;
            panel.BackColor   = Color.FromArgb(20, color.R, color.G, color.B);
            bar.Visible       = true;
            icon.ForeColor    = color;
            name.ForeColor    = Theme.TextPrimary;
            name.Font         = Theme.FontNavSel;
        }

        // ─────────────────────────────────────────────────────────────
        // CONTENT AREA
        // ─────────────────────────────────────────────────────────────
        private void BuildContentArea()
        {
            _contentArea = new Panel
            {
                BackColor = Theme.BgDark,
                Padding   = new Padding(0),
            };

            // Log panel at bottom
            _logPanel = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 180,
                BackColor = Theme.BgPanel,
                Visible   = false,
                Padding   = new Padding(0, 2, 0, 0),
            };

            var logHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 28,
                BackColor = Theme.BgToolbar,
                Padding   = new Padding(8, 0, 8, 0),
            };
            var logTitle = new Label
            {
                Text      = "📋 NHẬT KÝ HOẠT ĐỘNG",
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                AutoSize  = true,
                Location  = new Point(8, 7),
            };
            var clearBtn = new Button
            {
                Text      = "Xóa",
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(48, 20),
                Location  = new Point(_logPanel.Width - 60, 4),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                Cursor    = Cursors.Hand,
            };
            clearBtn.FlatAppearance.BorderSize = 0;
            clearBtn.Click += (s, e) => _logBox?.Clear();
            logHeader.Controls.AddRange(new Control[] { logTitle, clearBtn });

            _logBox = new RichTextBox
            {
                Dock      = DockStyle.Fill,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextPrimary,
                Font      = Theme.FontMonoSm,
                ReadOnly  = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Padding   = new Padding(8),
            };

            _logPanel.Controls.Add(_logBox);
            _logPanel.Controls.Add(logHeader);

            _contentArea.Controls.Add(_logPanel);
        }

        // ─────────────────────────────────────────────────────────────
        // STATUS BAR
        // ─────────────────────────────────────────────────────────────
        private void BuildStatusBar()
        {
            _statusBar = new Panel
            {
                Height    = Theme.StatusBarHeight,
                BackColor = Theme.BgToolbar,
                Padding   = new Padding(12, 0, 12, 0),
            };

            _statusLabel = new Label
            {
                Text      = "✅  Sẵn sàng",
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextSuccess,
                AutoSize  = true,
                Location  = new Point(12, 8),
            };

            var configInfo = new Label
            {
                Text      = $"Config: {_config.ConfigFilePath}",
                Font      = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                AutoSize  = true,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            };

            _statusBar.Controls.Add(_statusLabel);
            _statusBar.Controls.Add(configInfo);

            _statusBar.Resize += (s, e) =>
            {
                configInfo.Location = new Point(
                    _statusBar.Width - configInfo.Width - 12,
                    (_statusBar.Height - configInfo.Height) / 2
                );
            };
        }

        // ─────────────────────────────────────────────────────────────
        // NAVIGATION
        // ─────────────────────────────────────────────────────────────
        private void NavigateTo(string pageId)
        {
            if (_currentPageId == pageId) return;
            _currentPageId = pageId;

            Logger.Info($"Navigate to: {pageId}");
            SetStatus($"Đang tải: {pageId}...");

            // Remove current page
            if (_currentPage != null)
            {
                _contentArea.Controls.Remove(_currentPage);
                _currentPage.Dispose();
                _currentPage = null;
            }

            // Create new page
            _currentPage = PageFactory.Create(pageId, _config);
            if (_currentPage != null)
            {
                _currentPage.Dock = DockStyle.Fill;
                _contentArea.Controls.Add(_currentPage);
                _currentPage.BringToFront();
            }

            SetStatus("✅  Sẵn sàng");
        }

        private void LoadInitialPage()
        {
            NavigateTo("dashboard");

            // Select dashboard nav item visually
            if (_navItems.TryGetValue("dashboard", out var dashNav))
            {
                // Simulate selection
                dashNav.PerformLayout();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // LOG
        // ─────────────────────────────────────────────────────────────
        private void SetupLogger()
        {
            Logger.Initialize(_config.LogDir, _config.LogFile);

            Logger.OnLog += (line, level) =>
            {
                if (_logBox == null || _logBox.IsDisposed) return;
                if (_logBox.InvokeRequired)
                {
                    _logBox.Invoke(new Action(() => AppendLog(line, level)));
                }
                else
                {
                    AppendLog(line, level);
                }
            };

            Logger.Info($"WinPE Nghitr Dev v{_config.Version} started");
            Logger.Info($"Config: {_config.ConfigFilePath}");
        }

        private void AppendLog(string line, Core.LogLevel level)
        {
            var color = level switch
            {
                Core.LogLevel.ERROR   => Theme.TextError,
                Core.LogLevel.WARN    => Theme.TextWarning,
                Core.LogLevel.SUCCESS => Theme.TextSuccess,
                Core.LogLevel.DEBUG   => Theme.TextMuted,
                _                     => Theme.TextSecondary,
            };

            _logBox.SelectionStart  = _logBox.TextLength;
            _logBox.SelectionLength = 0;
            _logBox.SelectionColor  = color;
            _logBox.AppendText(line + "\n");
            _logBox.ScrollToCaret();
        }

        private void ToggleLogPanel()
        {
            _logPanelVisible     = !_logPanelVisible;
            _logPanel.Visible    = _logPanelVisible;
        }

        // ─────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────
        public void SetStatus(string message)
        {
            if (_statusLabel.InvokeRequired)
                _statusLabel.Invoke(new Action(() => _statusLabel.Text = message));
            else
                _statusLabel.Text = message;
        }

        private void UpdateLayout() { /* Responsive adjustments if needed */ }

        private void ConfirmAction(string message, Action action)
        {
            var result = MessageBox.Show(
                message,
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            if (result == DialogResult.Yes) action();
        }

        // ─────────────────────────────────────────────────────────────
        // DATA CLASSES
        // ─────────────────────────────────────────────────────────────
        private class NavGroup
        {
            public string   Title { get; }
            public NavItem[] Items { get; }
            public NavGroup(string title, NavItem[] items) { Title = title; Items = items; }
        }

        private class NavItem
        {
            public string Id    { get; }
            public string Icon  { get; }
            public string Name  { get; }
            public Color  Color { get; }
            public NavItem(string id, string icon, string name, Color color)
            { Id = id; Icon = icon; Name = name; Color = color; }
        }
    }
}
