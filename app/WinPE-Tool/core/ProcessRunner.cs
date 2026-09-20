using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using WinPETool.Core;

namespace WinPETool.Core
{
    public class ProcessResult
    {
        public int    ExitCode  { get; set; }
        public string Output    { get; set; }
        public string Error     { get; set; }
        public bool   Success   => ExitCode == 0;
    }

    /// <summary>
    /// Chạy external processes — CMD, PowerShell, diskpart, dism, v.v.
    /// </summary>
    public static class ProcessRunner
    {
        /// <summary>
        /// Chạy process và trả về kết quả
        /// </summary>
        public static ProcessResult Run(
            string executable,
            string arguments = "",
            string workingDir = null,
            int timeoutMs = 30000,
            Action<string> onOutput = null)
        {
            Logger.Debug($"Run: {executable} {arguments}");

            var result = new ProcessResult();
            var output = new StringBuilder();
            var error  = new StringBuilder();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = executable,
                    Arguments              = arguments,
                    WorkingDirectory       = workingDir ?? Environment.CurrentDirectory,
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding  = Encoding.UTF8,
                };

                using (var proc = new Process { StartInfo = psi })
                {
                    proc.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data == null) return;
                        output.AppendLine(e.Data);
                        onOutput?.Invoke(e.Data);
                    };
                    proc.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data == null) return;
                        error.AppendLine(e.Data);
                    };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    bool exited = proc.WaitForExit(timeoutMs);
                    if (!exited)
                    {
                        proc.Kill();
                        result.ExitCode = -1;
                        result.Error    = "Process timed out";
                        Logger.Warn($"Process timed out: {executable}");
                        return result;
                    }

                    result.ExitCode = proc.ExitCode;
                }
            }
            catch (Exception ex)
            {
                result.ExitCode = -1;
                result.Error    = ex.Message;
                Logger.Error($"ProcessRunner failed: {ex.Message}");
            }

            result.Output = output.ToString();
            result.Error  = error.ToString();

            if (!result.Success)
                Logger.Warn($"Process exited {result.ExitCode}: {executable}");

            return result;
        }

        /// <summary>
        /// Chạy PowerShell script hoặc command
        /// </summary>
        public static ProcessResult RunPowerShell(
            string script,
            bool isFile = false,
            int timeoutMs = 60000,
            Action<string> onOutput = null)
        {
            var args = isFile
                ? $"-ExecutionPolicy Bypass -NonInteractive -File \"{script}\""
                : $"-ExecutionPolicy Bypass -NonInteractive -Command \"{script.Replace("\"", "\\\"")}\"";

            return Run("powershell.exe", args, null, timeoutMs, onOutput);
        }

        /// <summary>
        /// Chạy lệnh qua CMD
        /// </summary>
        public static ProcessResult RunCmd(string command, int timeoutMs = 30000)
        {
            return Run("cmd.exe", $"/c {command}", null, timeoutMs);
        }

        /// <summary>
        /// Chạy diskpart với script
        /// </summary>
        public static ProcessResult RunDiskPart(string scriptContent, int timeoutMs = 60000)
        {
            var tmpScript = Path.Combine(Path.GetTempPath(), "winpe_diskpart.txt");
            try
            {
                File.WriteAllText(tmpScript, scriptContent, Encoding.ASCII);
                return Run("diskpart.exe", $"/s \"{tmpScript}\"", null, timeoutMs);
            }
            finally
            {
                if (File.Exists(tmpScript))
                    File.Delete(tmpScript);
            }
        }

        /// <summary>
        /// Mở ứng dụng (không chờ exit)
        /// </summary>
        public static void Launch(string executable, string arguments = "")
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName        = executable,
                    Arguments       = arguments,
                    UseShellExecute = true
                });
                Logger.Info($"Launched: {executable}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to launch {executable}: {ex.Message}");
                throw;
            }
        }
    }
}
