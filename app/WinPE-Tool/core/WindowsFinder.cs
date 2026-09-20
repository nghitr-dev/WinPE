using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using WinPETool.Core;

namespace WinPETool.Core
{
    public class WindowsInstallation
    {
        public string Drive          { get; set; }
        public string WindowsPath    { get; set; }
        public string SystemDrive    => Drive;
        public string WindowsDirectory => WindowsPath;
        public string Version        { get; set; }
        public string Edition        { get; set; }
        public string SystemRoot     { get; set; }
        public bool   HasBCD         { get; set; }
        public bool   IsAccessible   { get; set; }
        public long   FreeSpaceBytes { get; set; }
    }

    /// <summary>
    /// Phát hiện Windows installations trên tất cả ổ đĩa
    /// KHÔNG giả định Windows nằm ở C:
    /// </summary>
    public static class WindowsFinder
    {
        public static List<WindowsInstallation> FindInstallations() => FindAll();

        public static List<WindowsInstallation> FindAll()
        {
            var results = new List<WindowsInstallation>();
            Logger.Info("Scanning for Windows installations...");

            var drives = System.IO.DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (!drive.IsReady) continue;
                if (drive.DriveType != System.IO.DriveType.Fixed &&
                    drive.DriveType != System.IO.DriveType.Removable) continue;

                var letter  = drive.Name; // e.g. "C:\"
                var winPath = Path.Combine(letter, "Windows");
                var sysPath = Path.Combine(winPath, "System32");

                if (!Directory.Exists(winPath) || !Directory.Exists(sysPath)) continue;

                Logger.Info($"Found Windows directory at: {winPath}");

                var install = new WindowsInstallation
                {
                    Drive        = letter,
                    WindowsPath  = winPath,
                    SystemRoot   = winPath,
                    IsAccessible = true,
                    FreeSpaceBytes = drive.AvailableFreeSpace,
                };

                // Try to read version from registry hive
                try
                {
                    var version = ReadOfflineVersion(winPath);
                    install.Version = version.version;
                    install.Edition = version.edition;
                }
                catch
                {
                    install.Version = "Unknown";
                    install.Edition = "Unknown";
                }

                // Check BCD
                var bcdPath = Path.Combine(letter, "Boot", "BCD");
                var efiPath = Path.Combine(letter, "EFI", "Microsoft", "Boot", "BCD");
                install.HasBCD = File.Exists(bcdPath) || File.Exists(efiPath);

                results.Add(install);
                Logger.Info($"  Version: {install.Version} | Edition: {install.Edition} | BCD: {install.HasBCD}");
            }

            Logger.Info($"Total Windows installations found: {results.Count}");
            return results;
        }

        private static (string version, string edition) ReadOfflineVersion(string winPath)
        {
            // Read from SOFTWARE hive offline
            var hivePath = Path.Combine(winPath, "System32", "config", "SOFTWARE");
            if (!File.Exists(hivePath))
                return ("Unknown", "Unknown");

            // Use reg.exe to load hive offline
            var tempKey = $"WINPE_TMP_{Guid.NewGuid():N}";
            try
            {
                var loadResult = ProcessRunner.RunCmd($"reg load HKLM\\{tempKey} \"{hivePath}\"");
                if (!loadResult.Success) return ("Unknown", "Unknown");

                var versionResult = ProcessRunner.RunCmd(
                    $"reg query \"HKLM\\{tempKey}\\Microsoft\\Windows NT\\CurrentVersion\" /v ProductName");
                var buildResult = ProcessRunner.RunCmd(
                    $"reg query \"HKLM\\{tempKey}\\Microsoft\\Windows NT\\CurrentVersion\" /v CurrentBuildNumber");
                var editionResult = ProcessRunner.RunCmd(
                    $"reg query \"HKLM\\{tempKey}\\Microsoft\\Windows NT\\CurrentVersion\" /v EditionID");

                string version = ParseRegValue(versionResult.Output);
                string build   = ParseRegValue(buildResult.Output);
                string edition = ParseRegValue(editionResult.Output);

                if (!string.IsNullOrEmpty(build)) version += $" (Build {build})";

                return (version, edition);
            }
            finally
            {
                // Always unload hive
                ProcessRunner.RunCmd($"reg unload HKLM\\{tempKey}");
            }
        }

        private static string ParseRegValue(string regOutput)
        {
            if (string.IsNullOrEmpty(regOutput)) return "";
            // Format: "    ValueName    REG_SZ    Value"
            foreach (var line in regOutput.Split('\n'))
            {
                var parts = line.Trim().Split(new[] { "    " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3) return parts[parts.Length - 1].Trim();
                if (parts.Length == 2 && parts[0].Contains("REG_")) return "";
            }
            return "";
        }
    }
}
