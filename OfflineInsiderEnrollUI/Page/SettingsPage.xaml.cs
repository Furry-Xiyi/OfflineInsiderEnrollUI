using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.WindowsAppSDK.Runtime;
using System;
using Windows.ApplicationModel;

namespace OfflineInsiderEnrollUI
{
    public sealed partial class SettingsPage : Page
    {
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
            AppVersion.Text = $"版本 {version.Major}.{version.Minor}.{version.Build}";

            // 发布者显示名（PublisherDisplayName）
            string publisher = package.PublisherDisplayName;
            // 版权行
            int year = DateTime.Now.Year;
            CopyrightText.Text =
                $"©{year} {publisher}。保留所有权利";
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
            var dialog = new FeedbackDialog
            {
                XamlRoot = MainWindow.Instance.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await Windows.System.Launcher.LaunchUriAsync(
                    new Uri("https://github.com/Furry-Xiyi/OfflineInsiderEnrollUI")
                );
            }
        }
    }
}