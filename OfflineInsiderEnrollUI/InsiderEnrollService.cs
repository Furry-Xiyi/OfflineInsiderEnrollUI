using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OfflineInsiderEnrollUI
{
    public class InsiderStatus
    {
        public bool IsEnrolled { get; set; }
        public string ChannelName { get; set; }
        public bool FlightSigningEnabled { get; set; }
    }

    public static class InsiderEnrollService
    {

        public static async Task<int> GetWindowsBuild()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                    if (key != null)
                    {
                        var buildStr = key.GetValue("CurrentBuild")?.ToString();
                        if (int.TryParse(buildStr, out int build))
                            return build;
                    }
                }
                catch { }
                return Environment.OSVersion.Version.Build;
            });
        }
        private static void RunPowerShellAdmin(string script)
        {
            // 写入临时脚本
            string tempScript = Path.Combine(Path.GetTempPath(), $"insider_{Guid.NewGuid()}.ps1");
            File.WriteAllText(tempScript, script, Encoding.UTF8);

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScript}\"",
                Verb = "runas",            // ⭐ UAC
                UseShellExecute = true,    // ⭐ 必须 true 才能 runas
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            // ⭐ 启动 PowerShell
            using var process = Process.Start(psi);

            // ⭐ 等待脚本执行完毕（同步）
            process.WaitForExit();

#if DEBUG
            Debug.WriteLine($"===== PowerShell exited with code {process.ExitCode} =====");
#endif

            // ⭐ 删除临时脚本
            try { File.Delete(tempScript); } catch { }
        }
        private static string LoadScript(string fileName)
        {
            string scriptsDir = Path.Combine(AppContext.BaseDirectory, "Scripts");
            string path = Path.Combine(scriptsDir, fileName);

            if (!File.Exists(path))
                throw new FileNotFoundException($"Script not found: {path}");

            return File.ReadAllText(path, Encoding.UTF8);
        }
        private static void RunEnrollSuperScript(string channel, string fancy, string content, string ring, int rid, int? brl)
        {
            string script = LoadScript("EnrollSuper.ps1")
                .Replace("__CHANNEL__", channel)
                .Replace("__FANCY__", fancy)
                .Replace("__CONTENT__", content)
                .Replace("__RING__", ring)
                .Replace("__RID__", rid.ToString())
                .Replace("__SCRIPTVER__", "2.6.4");

            // 如果需要 BRL（Beta/Dev/RP）
            if (brl.HasValue)
            {
                script += $"\nSet-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'BranchReadinessLevel' -Value {brl.Value} -Type DWord -Force\n";
            }

#if DEBUG
            Debug.WriteLine("===== Final EnrollSuper.ps1 =====");
            Debug.WriteLine(script);
#endif

            RunPowerShellAdmin(script);
        }

        public static async Task<InsiderStatus> GetCurrentStatus()
        {
            return await Task.Run(() =>
            {
                var status = new InsiderStatus();

                try
                {
                    // 检查是否已注册
                    using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability");
                    if (key != null)
                    {
                        var branchName = key.GetValue("BranchName")?.ToString();
                        if (!string.IsNullOrEmpty(branchName))
                        {
                            status.IsEnrolled = true;
                            status.ChannelName = branchName;
                        }
                    }
                }
                catch { }

                // 检查 Flight Signing
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "bcdedit.exe",
                        Arguments = "/enum {current}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    if (process != null)
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        status.FlightSigningEnabled = output.Contains("flightsigning") && output.Contains("Yes");
                    }
                }
                catch { }

                return status;
            });
        }

        public static async Task<bool> EnrollToChannel(int channelIndex)
        {
            return await Task.Run(() =>
            {
                string channel, fancy, content, ring;
                int rid;
                int? brl = null;

                switch (channelIndex)
                {
                    case 0: channel = "CanaryChannel"; fancy = "Canary Channel"; content = "Mainline"; ring = "External"; rid = 11; break;
                    case 1: channel = "Dev"; fancy = "Dev Channel"; content = "Mainline"; ring = "External"; rid = 11; brl = 2; break;
                    case 2: channel = "Beta"; fancy = "Beta Channel"; content = "Mainline"; ring = "External"; rid = 11; brl = 4; break;
                    case 3: channel = "ReleasePreview"; fancy = "Release Preview Channel"; content = "Mainline"; ring = "External"; rid = 11; brl = 8; break;
                    default: throw new ArgumentException("Invalid channel index");
                }

                RunEnrollSuperScript(channel, fancy, content, ring, rid, brl);
                return true;
            });
        }
        private static void RunStopInsiderSuperScript()
        {
            string script = LoadScript("StopInsiderSuper.ps1");

#if DEBUG
            Debug.WriteLine("===== Final StopInsiderSuper.ps1 =====");
            Debug.WriteLine(script);
#endif

            RunPowerShellAdmin(script);
        }

        public static async Task<bool> StopInsider()
        {
            return await Task.Run(() =>
            {
                RunStopInsiderSuperScript();
                return true; // 需要重启
            });
        }

        public static async Task RebootSystem()
        {
            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "shutdown.exe",
                        Arguments = "/r /t 0",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Process.Start(psi);
                }
                catch { }
            });
        }
    }
}