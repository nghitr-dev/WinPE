using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Collections.Generic;

namespace WinPETool
{
    /// <summary>
    /// Cấu hình ứng dụng — load từ winpe-config.json
    /// </summary>
    public class AppConfig
    {
        // Paths mặc định
        private static readonly string[] ConfigSearchPaths = new[]
        {
            // WinPE runtime path
            @"X:\WinPE\Config\winpe-config.json",
            // Development path (cạnh exe)
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "winpe-config.json"),
            // Relative to exe
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "winpe-config.json"),
        };

        // ─── Project ──────────────────────────────────────
        public string ProjectName    { get; set; } = "WinPE_Nghitr-dev";
        public string DisplayName    { get; set; } = "WinPE Nghitr Dev";
        public string Version        { get; set; } = "1.0.0";
        public string Author         { get; set; } = "Nghitr";
        public string Description    { get; set; } = "Công cụ cứu hộ và bảo trì Windows";

        // ─── Branding ─────────────────────────────────────
        public string WindowTitle    { get; set; } = "WinPE Nghitr Dev";
        public string LogoFile       { get; set; } = "";
        public string WallpaperFile  { get; set; } = "";
        public string PrimaryColor   { get; set; } = "#1A2744";
        public string AccentColor    { get; set; } = "#00AAFF";
        public string SecondaryColor { get; set; } = "#0D1B35";

        // ─── UI ───────────────────────────────────────────
        public int  SidebarWidth        { get; set; } = 260;
        public string DefaultPage       { get; set; } = "Dashboard";
        public bool ShowSplash          { get; set; } = true;
        public int  SplashDurationMs    { get; set; } = 2000;
        public bool AnimationsEnabled   { get; set; } = true;

        // ─── Logging ──────────────────────────────────────
        public string LogDir  { get; set; } = @"X:\WinPE\Logs";
        public string LogFile { get; set; } = "WinPE_Nghitr.log";
        public string LogLevel { get; set; } = "INFO";

        // ─── Safety ───────────────────────────────────────
        public bool RequireConfirmationForDestructive { get; set; } = true;
        public List<string> DestructiveOperations { get; set; } = new List<string>
        {
            "FormatPartition", "DeletePartition", "CleanDisk",
            "OverwriteBCD", "RestoreImage", "ResetPassword"
        };

        // Loaded config file path (for debugging)
        public string ConfigFilePath { get; set; } = "(defaults)";

        /// <summary>
        /// Load config từ file JSON, fallback về defaults nếu không tìm thấy
        /// </summary>
        public static AppConfig Load()
        {
            foreach (var path in ConfigSearchPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        var config = LoadFromFile(path);
                        config.ConfigFilePath = path;
                        return config;
                    }
                    catch
                    {
                        // Continue to next path on parse error
                    }
                }
            }

            // Return defaults
            return new AppConfig { ConfigFilePath = "(defaults — config not found)" };
        }

        private static AppConfig LoadFromFile(string path)
        {
            // Simple JSON parsing — avoid heavy dependencies in WinPE
            var json = File.ReadAllText(path, Encoding.UTF8);
            var config = new AppConfig();

            // Parse key fields using simple string extraction
            config.ProjectName   = ExtractJsonString(json, "name",         "WinPE_Nghitr-dev");
            config.DisplayName   = ExtractJsonString(json, "displayName",   "WinPE Nghitr Dev");
            config.Version       = ExtractJsonString(json, "version",       "1.0.0");
            config.Author        = ExtractJsonString(json, "author",        "Nghitr");
            config.WindowTitle   = ExtractJsonString(json, "windowTitle",   "WinPE Nghitr Dev");
            config.LogoFile      = ExtractJsonString(json, "logoFile",      "");
            config.WallpaperFile = ExtractJsonString(json, "wallpaperFile", "");
            config.PrimaryColor  = ExtractJsonString(json, "primaryColor",  "#1A2744");
            config.AccentColor   = ExtractJsonString(json, "accentColor",   "#00AAFF");
            config.LogDir        = ExtractJsonString(json, "logDir",        @"X:\WinPE\Logs");
            config.LogFile       = ExtractJsonString(json, "logFile",       "WinPE_Nghitr.log");
            config.SidebarWidth  = ExtractJsonInt(json,    "sidebarWidth",  260);

            return config;
        }

        private static string ExtractJsonString(string json, string key, string defaultVal)
        {
            var pattern = $"\"{key}\"";
            int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return defaultVal;

            int colon = json.IndexOf(':', idx + pattern.Length);
            if (colon < 0) return defaultVal;

            int q1 = json.IndexOf('"', colon + 1);
            if (q1 < 0) return defaultVal;

            int q2 = json.IndexOf('"', q1 + 1);
            if (q2 < 0) return defaultVal;

            return json.Substring(q1 + 1, q2 - q1 - 1);
        }

        private static int ExtractJsonInt(string json, string key, int defaultVal)
        {
            var pattern = $"\"{key}\"";
            int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return defaultVal;

            int colon = json.IndexOf(':', idx + pattern.Length);
            if (colon < 0) return defaultVal;

            // Find number after colon
            int start = colon + 1;
            while (start < json.Length && (json[start] == ' ' || json[start] == '\n' || json[start] == '\r'))
                start++;

            int end = start;
            while (end < json.Length && char.IsDigit(json[end]))
                end++;

            if (end == start) return defaultVal;
            return int.TryParse(json.Substring(start, end - start), out int val) ? val : defaultVal;
        }
    }
}
