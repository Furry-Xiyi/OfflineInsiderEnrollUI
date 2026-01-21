using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OfflineInsiderEnrollUI.Dialogs;
using System;
using System.Threading.Tasks;

namespace OfflineInsiderEnrollUI
{
    public sealed partial class HomePage : Page
    {
        private int selectedChannel = -1;
        private int currentEnrolledChannel = -1;
        private bool isLoading = false;
        private MainWindow Main => (MainWindow)App.MainWindow;
        public HomePage()
        {
            this.InitializeComponent();
            Loaded += HomePage_Loaded;
        }

        private async void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            isLoading = true;
            await RefreshStatus();
            isLoading = false;
        }

        private async Task RefreshStatus()
        {
            try
            {
                // 检查系统信息
                var build = await InsiderEnrollService.GetWindowsBuild();

                int ubr = (int)(Microsoft.Win32.Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                    "UBR",
                    0
                ) ?? 0);

                // 显示更精确的版本号
                SystemInfoBarTitle.Text = "系统信息";
                SystemInfoBarMessage.Text = $"Windows 10.0.{build}.{ubr}";

                if (build < 17763)
                {
                    SystemInfoBarTitle.Text = "错误";
                    SystemInfoBarMessage.Text = "不兼容: 此工具仅支持 Windows 10 v1809 (Build 17763) 及更高版本";

                    EnrollButton.IsEnabled = false;
                    UnenrollButton.IsEnabled = false;
                    ChannelSelection.IsEnabled = false;
                    return;
                }

                // 检查当前状态
                var status = await InsiderEnrollService.GetCurrentStatus();
                CurrentStatusText.Text = status.IsEnrolled ? $"已注册到 {status.ChannelName}" : "未注册";
                FlightSigningText.Text = $"Flight Signing: {(status.FlightSigningEnabled ? "已启用" : "未启用")}";

                if (status.IsEnrolled)
                {
                    currentEnrolledChannel = GetChannelIndexFromName(status.ChannelName);
                    SelectChannelByIndex(currentEnrolledChannel);
                }
                else
                {
                    currentEnrolledChannel = -1;
                }
            }
            catch (Exception ex)
            {
                SystemInfoBarTitle.Text = "错误";
                SystemInfoBarMessage.Text = $"读取状态失败: {ex.Message}";
            }
        }

        private int GetChannelIndexFromName(string channelName)
        {
            return channelName switch
            {
                "CanaryChannel" => 0,
                "Dev" => 1,
                "Beta" => 2,
                "ReleasePreview" => 3,
                _ => -1
            };
        }

        private void SelectChannelByIndex(int index)
        {
            if (index < 0 || index > 3)
                return;

            var radioButtons = ChannelSelection.Items;
            if (index < radioButtons.Count && radioButtons[index] is RadioButton rb)
            {
                rb.IsChecked = true;
                selectedChannel = index;
            }
        }

        private void ChannelSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (isLoading) return;

            if (ChannelSelection.SelectedItem is RadioButton rb)
            {
                selectedChannel = int.Parse(rb.Tag.ToString());

                // 只有当选择的频道与当前注册的频道不同时，才启用注册按钮
                EnrollButton.IsEnabled = (selectedChannel != currentEnrolledChannel);
            }
        }

        private async void EnrollButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedChannel < 0 || selectedChannel > 3)
                return;

            // 确认对话框
            var confirmDialog = new ConfirmEnrollDialog(GetChannelName(selectedChannel))
            {
                XamlRoot = this.XamlRoot
            };

            if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            //显示全屏遮罩
            var main = (MainWindow)App.MainWindow;
            main.ShowOverlay("正在注册到选定频道...");

            EnrollButton.IsEnabled = false;
            UnenrollButton.IsEnabled = false;
            ChannelSelection.IsEnabled = false;

            try
            {
                //同步执行 PowerShell（等待完成）
                var needReboot = await InsiderEnrollService.EnrollToChannel(selectedChannel);

                //刷新状态
                await RefreshStatus();

                // 成功对话框
                var successDialog = new EnrollSuccessDialog(needReboot)
                {
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();

                if (needReboot)
                    RebootInfoBar.IsOpen = true;
            }
            catch (UnauthorizedAccessException)
            {
                var errorDialog = new ErrorDialog("需要管理员权限才能修改注册表。请以管理员身份运行此应用。")
                {
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var errorDialog = new ErrorDialog(ex.Message)
                {
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
            finally
            {
                //隐藏全屏遮罩
                main.HideOverlay();

                EnrollButton.IsEnabled = false;
                UnenrollButton.IsEnabled = true;
                ChannelSelection.IsEnabled = true;
            }
        }

        private async void UnenrollButton_Click(object sender, RoutedEventArgs e)
        {
            var confirmDialog = new ConfirmUnenrollDialog()
            {
                XamlRoot = this.XamlRoot
            };

            if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            //显示全屏遮罩
            var main = (MainWindow)App.MainWindow;
            main.ShowOverlay("正在停止 Insider 注册...");

            EnrollButton.IsEnabled = false;
            UnenrollButton.IsEnabled = false;
            ChannelSelection.IsEnabled = false;

            try
            {
                //同步执行 PowerShell（等待执行完毕）
                var needReboot = await InsiderEnrollService.StopInsider();

                //刷新状态
                await RefreshStatus();

                // 成功对话框
                var successDialog = new UnenrollSuccessDialog(needReboot)
                {
                    XamlRoot = this.XamlRoot
                };

                await successDialog.ShowAsync();

                if (needReboot)
                    RebootInfoBar.IsOpen = true;
            }
            catch (Exception ex)
            {
                var errorDialog = new ErrorDialog($"发生错误: {ex.Message}")
                {
                    XamlRoot = this.XamlRoot
                };

                await errorDialog.ShowAsync();
            }
            finally
            {
                //隐藏全屏遮罩
                main.HideOverlay();

                EnrollButton.IsEnabled = true;
                UnenrollButton.IsEnabled = true;
                ChannelSelection.IsEnabled = true;
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshStatus();
        }
        private async void OpenDiagnosticsButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(
                new Uri("ms-settings:privacy-feedback")
            );
        }
        private async void RebootButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ConfirmRebootDialog()
            {
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                await InsiderEnrollService.RebootSystem();
        }

        private string GetChannelName(int channel)
        {
            return channel switch
            {
                0 => "Canary Channel",
                1 => "Dev Channel",
                2 => "Beta Channel",
                3 => "Release Preview Channel",
                _ => "未知"
            };
        }
    }
}