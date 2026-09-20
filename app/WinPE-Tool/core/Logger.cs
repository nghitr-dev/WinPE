using System;
using System.IO;
using System.Text;

namespace WinPETool.Core
{
    public enum LogLevel { DEBUG, INFO, WARN, ERROR, SUCCESS }

    /// <summary>
    /// Thread-safe logger — ghi ra file + raise event cho UI
    /// </summary>
    public static class Logger
    {
        private static string _logPath;
        private static readonly object _lock = new object();

        // UI components có thể subscribe để hiển thị log real-time
        public static event Action<string, LogLevel> OnLog;

        public static void Initialize(string logDir, string logFile)
        {
            try
            {
                Directory.CreateDirectory(logDir);
                _logPath = Path.Combine(logDir, logFile);
            }
            catch
            {
                // Fallback: log cạnh exe
                _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logFile);
            }

            Info("=== WinPE Nghitr Dev — Log Started ===");
            Info($"Log file: {_logPath}");
        }

        public static void Info(string msg)    => Write(msg, LogLevel.INFO);
        public static void Warn(string msg)    => Write(msg, LogLevel.WARN);
        public static void Error(string msg)   => Write(msg, LogLevel.ERROR);
        public static void Success(string msg) => Write(msg, LogLevel.SUCCESS);
        public static void Debug(string msg)   => Write(msg, LogLevel.DEBUG);

        public static void Write(string message, LogLevel level)
        {
            var ts   = DateTime.Now.ToString("HH:mm:ss");
            var line = $"[{ts}][{level,-7}] {message}";

            // File log
            if (!string.IsNullOrEmpty(_logPath))
            {
                lock (_lock)
                {
                    try { File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8); }
                    catch { /* never crash on log failure */ }
                }
            }

            // Console (for debug)
            Console.WriteLine(line);

            // Notify UI subscribers
            try { OnLog?.Invoke(line, level); }
            catch { }
        }

        public static string GetLogPath() => _logPath;

        public static void SaveCopy(string destinationPath)
        {
            if (string.IsNullOrEmpty(_logPath) || !File.Exists(_logPath)) return;
            try { File.Copy(_logPath, destinationPath, overwrite: true); }
            catch (Exception ex) { Error($"Failed to save log copy: {ex.Message}"); }
        }
    }
}
