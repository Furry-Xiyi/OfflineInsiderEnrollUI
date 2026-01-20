using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.WindowsAppSDK.Runtime;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;

namespace OfflineInsiderEnrollUI
{
    public sealed partial class SettingsPage : Page
    {
        private async Task OpenExternalLinkAsync(string url)
        {
            var dialog = new FeedbackDialog
            {
                XamlRoot = MainWindow.Instance.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
            }
        }

        private bool _isInitializing = true;
        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();
            _isInitializing = false;
            // 获取包信息
            var package = Package.Current;
            var version = package.Id.Version;

            // 版本号
            AppVersion.Text = $"v{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            // 发布者显示名（PublisherDisplayName）
            string publisher = package.PublisherDisplayName;
            // 版权行
            int year = DateTime.Now.Year;
            var loader = ResourceLoader.GetForViewIndependentUse();
            string rights = loader.GetString("Copyright_AllRightsReserved");

            CopyrightText.Text =
    CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "zh" => $"©{year} {publisher}。{rights}",
        _ => $"©{year} {publisher}. {rights}"
    };
        }

        private void LoadSettings()
        {
            // 加载主题设置
            var theme = SettingsHelper.GetTheme();
            switch (theme)
            {
                case "System":
                    ThemeSystem.IsChecked = true;
                    break;
                case "Light":
                    ThemeLight.IsChecked = true;
                    break;
                case "Dark":
                    ThemeDark.IsChecked = true;
                    break;
                default:
                    ThemeSystem.IsChecked = true;
                    break;
            }

            // 加载材质设置
            var material = SettingsHelper.GetMaterial();
            switch (material)
            {
                case "Acrylic":
                    MaterialAcrylic.IsChecked = true;
                    break;
                case "Mica":
                    MaterialMica.IsChecked = true;
                    break;
                case "MicaAlt":
                    MaterialMicaAlt.IsChecked = true;
                    break;
                default:
                    MaterialMica.IsChecked = true;
                    break;
            }

            // 设置版本号
            try
            {
                var version = Package.Current.Id.Version;
                AppVersion.Text = $"版本 {version.Major}.{version.Minor}.{version.Build}";
            }
            catch
            {
                AppVersion.Text = "版本 1.0.0";
            }
        }

        private void Theme_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing)
                return; // 阻止初始化时触发

            if (sender is RadioButton rb && rb.Tag != null)
            {
                string theme = rb.Tag.ToString();
                SettingsHelper.SetTheme(theme);

                App.MainWindowInstance?.ApplyTheme(theme);
            }
        }

        private void Material_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing)
                return;

            if (sender is RadioButton rb && rb.Tag != null)
            {
                string material = rb.Tag.ToString();
                SettingsHelper.SetMaterial(material);

                App.MainWindowInstance?.ApplyBackdrop(material);
            }
        }

        private async void SendFeedback_Click(object sender, RoutedEventArgs e)
        {
            await OpenExternalLinkAsync("https://github.com/Furry-Xiyi/OfflineInsiderEnrollUI");
        }
        private async void JoinQQ_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            await OpenExternalLinkAsync("https://qm.qq.com/q/dh2KHPYM9y");
        }
        private async void JoinTelegram_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            await OpenExternalLinkAsync("https://t.me/Clash_WinUI");
        }
        private async void OpenOfflineInsiderEnroll_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            await OpenExternalLinkAsync("https://github.com/abbodi1406/offlineinsiderenroll");
        }
    }
}