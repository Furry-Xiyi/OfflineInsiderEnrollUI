using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
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
        private const string SCRIPT_VERSION = "2.6.4";

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
                    case 0: // Canary
                        channel = "CanaryChannel";
                        fancy = "Canary Channel";
                        content = "Mainline";
                        ring = "External";
                        rid = 11;
                        break;
                    case 1: // Dev
                        channel = "Dev";
                        fancy = "Dev Channel";
                        content = "Mainline";
                        ring = "External";
                        rid = 11;
                        brl = 2;
                        break;
                    case 2: // Beta
                        channel = "Beta";
                        fancy = "Beta Channel";
                        content = "Mainline";
                        ring = "External";
                        rid = 11;
                        brl = 4;
                        break;
                    case 3: // Release Preview
                        channel = "ReleasePreview";
                        fancy = "Release Preview Channel";
                        content = "Mainline";
                        ring = "External";
                        rid = 11;
                        brl = 8;
                        break;
                    default:
                        throw new ArgumentException("Invalid channel index");
                }

                // 重置配置
                ResetInsiderConfig();

                // 添加配置
                AddInsiderConfig(channel, fancy, content, ring, rid, brl);

                // 启用 Flight Signing
                var needReboot = EnableFlightSigning();

                return needReboot;
            });
        }

        public static async Task<bool> StopInsider()
        {
            return await Task.Run(() =>
            {
                ResetInsiderConfig();
                var needReboot = DisableFlightSigning();
                return needReboot;
            });
        }

        private static void ResetInsiderConfig()
        {
            string[] keysToDelete = new[]
            {
                @"SOFTWARE\Microsoft\WindowsSelfHost\Account",
                @"SOFTWARE\Microsoft\WindowsSelfHost\Applicability",
                @"SOFTWARE\Microsoft\WindowsSelfHost\Cache",
                @"SOFTWARE\Microsoft\WindowsSelfHost\ClientState",
                @"SOFTWARE\Microsoft\WindowsSelfHost\UI",
                @"SOFTWARE\Microsoft\WindowsSelfHost\Restricted",
                @"SOFTWARE\Microsoft\WindowsSelfHost\ToastNotification",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\SLS\Programs\WUMUDCat",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\SLS\Programs\RingExternal"
            };

            foreach (var keyPath in keysToDelete)
            {
                try
                {
                    Registry.LocalMachine.DeleteSubKeyTree(keyPath, false);
                }
                catch { }
            }

            // 删除特定值
            DeleteRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry");
            DeleteRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry");
            DeleteRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "BranchReadinessLevel");
            DeleteRegistryValue(@"SYSTEM\Setup\WindowsUpdate", "AllowWindowsUpdate");
            DeleteRegistryValue(@"SYSTEM\Setup\MoSetup", "AllowUpgradesWithUnsupportedTPMOrCPU");
            DeleteRegistryValue(@"SYSTEM\Setup\LabConfig", "BypassRAMCheck");
            DeleteRegistryValue(@"SYSTEM\Setup\LabConfig", "BypassSecureBootCheck");
            DeleteRegistryValue(@"SYSTEM\Setup\LabConfig", "BypassStorageCheck");
            DeleteRegistryValue(@"SYSTEM\Setup\LabConfig", "BypassTPMCheck");
        }

        private static void DeleteRegistryValue(string keyPath, string valueName)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(keyPath, true);
                key?.DeleteValue(valueName, false);
            }
            catch { }
        }

        private static void AddInsiderConfig(string channel, string fancy, string content, string ring, int rid, int? brl)
        {
            var build = GetWindowsBuild().Result;

            // 主要配置
            SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Orchestrator", "EnableUUPScan", 1);
            SetRegistryValue($@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\SLS\Programs\Ring{ring}", "Enabled", 1);
            SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\SLS\Programs\WUMUDCat", "WUMUDCATEnabled", 1);

            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability", "EnablePreviewBuilds", 2);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability", "IsBuildFlightingEnabled", 1);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability", "IsConfigSettingsFlightingEnabled", 1);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability", "IsConfigExpFlightingEnabled", 0);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability", "TestFlags", 32);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability", "RingId", rid);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability", "Ring", ring);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability", "ContentType", content);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability", "BranchName", channel);

            // UI 配置
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Visibility", "UIHiddenElements", 65535);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Visibility", "UIDisabledElements", 65535);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Visibility", "UIServiceDrivenElementVisibility", 0);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Visibility", "UIErrorMessageVisibility", 192);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Visibility", "UIHiddenElements_Rejuv", 65534);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Visibility", "UIDisabledElements_Rejuv", 65535);

            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection", "UIRing", ring);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection", "UIContentType", content);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection", "UIBranch", channel);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection", "UIOptin", 1);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection", "UIDialogConsent", 0);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection", "UIUsage", 26);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection", "OptOutState", 25);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection", "AdvancedToggleState", 24);

            // 备份配置
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability", "RingBackup", ring);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability", "RingBackupV2", ring);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability", "BranchBackup", channel);

            // Cache 配置
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Cache", "PropertyIgnoreList", "AccountsBlob;;CTACBlob;FlightIDBlob;ServiceDrivenActionResults");
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Cache", "RequestedCTACAppIds", "WU;FSS");

            // Account 配置
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Account", "SupportedTypes", 3);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Account", "Status", 8);

            // ClientState 配置
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability", "UseSettingsExperience", 0);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\ClientState", "AllowFSSCommunications", 0);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\ClientState", "UICapabilities", 1);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\ClientState", "IgnoreConsolidation", 1);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\ClientState", "MsaUserTicketHr", 0);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\ClientState", "MsaDeviceTicketHr", 0);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\ClientState", "ValidateOnlineHr", 0);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\ClientState", "LastHR", 0);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\ClientState", "ErrorState", 0);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\ClientState", "PilotInfoRing", 3);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\ClientState", "RegistryAllowlistVersion", 4);
            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\ClientState", "FileAllowlistVersion", 1);

            SetRegistryValue(@"SOFTWARE\Microsoft\WindowsSelfHost\UI", "UIControllableState", 0);

            // 遥测设置
            SetRegistryValue(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 3);

            // BRL 设置
            if (brl.HasValue)
            {
                SetRegistryValue(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "BranchReadinessLevel", brl.Value);
            }

            // 绕过硬件检查
            SetRegistryValue(@"SYSTEM\Setup\WindowsUpdate", "AllowWindowsUpdate", 1);
            SetRegistryValue(@"SYSTEM\Setup\MoSetup", "AllowUpgradesWithUnsupportedTPMOrCPU", 1);
            SetRegistryValue(@"SYSTEM\Setup\LabConfig", "BypassRAMCheck", 1);
            SetRegistryValue(@"SYSTEM\Setup\LabConfig", "BypassSecureBootCheck", 1);
            SetRegistryValue(@"SYSTEM\Setup\LabConfig", "BypassStorageCheck", 1);
            SetRegistryValue(@"SYSTEM\Setup\LabConfig", "BypassTPMCheck", 1);
        }

        private static void SetRegistryValue(string keyPath, string valueName, object value)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(keyPath);
                if (key != null)
                {
                    if (value is int intValue)
                        key.SetValue(valueName, intValue, RegistryValueKind.DWord);
                    else if (value is string stringValue)
                        key.SetValue(valueName, stringValue, RegistryValueKind.String);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set {keyPath}\\{valueName}: {ex.Message}");
            }
        }

        private static bool EnableFlightSigning()
        {
            try
            {
                // 检查当前状态
                var status = GetCurrentStatus().Result;
                if (status.FlightSigningEnabled)
                    return false;

                var psi = new ProcessStartInfo
                {
                    FileName = "bcdedit.exe",
                    Arguments = "/set {current} flightsigning yes",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using var process = Process.Start(psi);
                process?.WaitForExit();

                return true; // 需要重启
            }
            catch
            {
                return false;
            }
        }

        private static bool DisableFlightSigning()
        {
            try
            {
                var status = GetCurrentStatus().Result;
                if (!status.FlightSigningEnabled)
                    return false;

                var psi = new ProcessStartInfo
                {
                    FileName = "bcdedit.exe",
                    Arguments = "/deletevalue {current} flightsigning",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using var process = Process.Start(psi);
                process?.WaitForExit();

                return true; // 需要重启
            }
            catch
            {
                return false;
            }
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