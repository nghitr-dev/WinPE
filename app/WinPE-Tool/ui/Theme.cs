using System.Drawing;

namespace WinPETool.UI
{
    /// <summary>
    /// Color palette và sizing constants cho toàn bộ GUI
    /// Dark mode — phù hợp WinPE rescue environment
    /// </summary>
    public static class Theme
    {
        // ─── Background ───────────────────────────────────
        public static readonly Color BgDark       = Color.FromArgb(10,  18,  38);   // Nền tối nhất
        public static readonly Color BgPanel      = Color.FromArgb(18,  30,  58);   // Panel/Sidebar
        public static readonly Color BgCard       = Color.FromArgb(22,  38,  72);   // Card/Item bg
        public static readonly Color BgHover      = Color.FromArgb(30,  50,  95);   // Hover state
        public static readonly Color BgSelected   = Color.FromArgb(0,   90,  180);  // Selected item
        public static readonly Color BgInput      = Color.FromArgb(15,  25,  50);   // Input fields
        public static readonly Color BgToolbar    = Color.FromArgb(14,  23,  46);   // Top toolbar

        // ─── Accent ───────────────────────────────────────
        public static readonly Color Accent       = Color.FromArgb(0,   170, 255);  // Main accent (#00AAFF)
        public static readonly Color AccentHover  = Color.FromArgb(30,  190, 255);  // Accent hover
        public static readonly Color AccentDark   = Color.FromArgb(0,   110, 200);  // Accent pressed
        public static readonly Color AccentGlow   = Color.FromArgb(0,   100, 180);  // Subtle glow

        // ─── Text ─────────────────────────────────────────
        public static readonly Color TextPrimary   = Color.FromArgb(230, 235, 245); // Main text
        public static readonly Color TextSecondary = Color.FromArgb(140, 160, 200); // Secondary/dim
        public static readonly Color TextMuted     = Color.FromArgb(80,  100, 150); // Muted/disabled
        public static readonly Color TextAccent    = Color.FromArgb(0,   170, 255); // Accent text
        public static readonly Color TextSuccess   = Color.FromArgb(50,  210, 120); // Green
        public static readonly Color TextWarning   = Color.FromArgb(255, 180, 0);   // Yellow/orange
        public static readonly Color TextError     = Color.FromArgb(255, 80,  70);  // Red
        public static readonly Color TextInfo      = Color.FromArgb(100, 200, 255); // Light blue

        // ─── Status colors ────────────────────────────────
        public static readonly Color Success      = Color.FromArgb(40,  190, 110);
        public static readonly Color Warning      = Color.FromArgb(230, 160, 0);
        public static readonly Color Error        = Color.FromArgb(220, 60,  55);
        public static readonly Color Info         = Color.FromArgb(0,   140, 220);

        // ─── Border / Separator ───────────────────────────
        public static readonly Color Border       = Color.FromArgb(35,  55,  100);
        public static readonly Color BorderLight  = Color.FromArgb(50,  75,  130);
        public static readonly Color Separator    = Color.FromArgb(28,  45,  85);

        // ─── Fonts ────────────────────────────────────────
        public static readonly Font FontTitle    = new Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold);
        public static readonly Font FontHeader   = new Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold);
        public static readonly Font FontBody     = new Font("Segoe UI", 9f);
        public static readonly Font FontSmall    = new Font("Segoe UI", 8f);
        public static readonly Font FontMono     = new Font("Consolas", 9f);
        public static readonly Font FontMonoSm   = new Font("Consolas", 8f);
        public static readonly Font FontNav      = new Font("Segoe UI", 9.5f);
        public static readonly Font FontNavSel   = new Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
        public static readonly Font FontButton   = new Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);

        // ─── Sizing ───────────────────────────────────────
        public static readonly int SidebarWidth   = 260;
        public static readonly int ToolbarHeight  = 52;
        public static readonly int StatusBarHeight = 28;
        public static readonly int NavItemHeight  = 44;
        public static readonly int CardPadding    = 16;
        public static readonly int BorderRadius   = 6;

        // ─── Category colors (sidebar groups) ────────────
        public static readonly Color CatRecovery  = Color.FromArgb(255, 100, 80);
        public static readonly Color CatDisk      = Color.FromArgb(255, 160, 0);
        public static readonly Color CatBackup    = Color.FromArgb(50,  200, 120);
        public static readonly Color CatDrivers   = Color.FromArgb(100, 180, 255);
        public static readonly Color CatHardware  = Color.FromArgb(180, 130, 255);
        public static readonly Color CatNetwork   = Color.FromArgb(0,   200, 200);
        public static readonly Color CatFiles     = Color.FromArgb(255, 200, 50);
        public static readonly Color CatSecurity  = Color.FromArgb(255, 80,  150);
        public static readonly Color CatSystem    = Color.FromArgb(150, 200, 255);
        public static readonly Color CatSettings  = Color.FromArgb(130, 140, 160);
    }
}
