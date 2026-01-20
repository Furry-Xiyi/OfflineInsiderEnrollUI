using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.UI;
using WinRT;
using WinRT.Interop;

namespace OfflineInsiderEnrollUI
{
    public sealed partial class MainWindow : Window
    {
        private AppWindow m_appWindow;
        private MicaController m_micaController;
        private DesktopAcrylicController m_acrylicController;
        private SystemBackdropConfiguration m_configurationSource;

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);
        public static MainWindow Instance { get; private set; }
        private IntPtr m_windowHandle;
        public MainWindow()
        {
            this.InitializeComponent();
            Instance = this;
            // 初始化窗口
            InitializeWindow();

            // 初始化背景和主题（在显示前）
            InitializeAppearance();
            // 注册关闭事件
            this.Closed += MainWindow_Closed;
            // 导航到首页
            ContentFrame.Navigate(typeof(HomePage));
            NavView.SelectedItem = HomeItem;
        }

        private void InitializeWindow()
        {
            // 获取窗口句柄和 AppWindow
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            m_appWindow = AppWindow.GetFromWindowId(windowId);

            // 设置窗口标题和图标
            m_appWindow.Title = "Offline Insider Enroll";
            m_appWindow.SetIcon("Assets/AppIcon.ico");

            // 设置窗口大小
            m_appWindow.Resize(new SizeInt32(1000, 700));

            // 自定义标题栏
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // 配置标题栏颜色
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = m_appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            }
        }
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            // 清理所有资源，防止 Win32 异常
            CleanupBackdrop();
        }
        private void InitializeAppearance()
        {
            // 应用主题
            var theme = SettingsHelper.GetTheme();
            ApplyTheme(theme, false);

            // 应用背景材质
            var material = SettingsHelper.GetMaterial();
            ApplyBackdrop(material);
        }

        public void ApplyTheme(string themeSetting, bool updateTitleBar = true)
        {
            ElementTheme theme = themeSetting switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            // 应用到根元素
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }

            // 更新 SystemBackdrop 配置
            if (m_configurationSource != null)
            {
                UpdateBackdropTheme();
            }

            // 更新标题栏
            if (updateTitleBar)
            {
                UpdateTitleBarColors();
            }
        }
        public void ShowOverlay(string message = "正在处理...")
        {
            OverlayText.Text = message;
            OverlayPanel.Visibility = Visibility.Visible;
        }

        public void HideOverlay()
        {
            OverlayPanel.Visibility = Visibility.Collapsed;
        }

        public void ApplyBackdrop(string materialSetting)
        {
            // 清理现有控制器
            CleanupBackdrop();

            // 创建配置
            m_configurationSource = new SystemBackdropConfiguration();
            UpdateBackdropTheme();

            // 根据设置应用材质
            if (materialSetting == "Acrylic" && DesktopAcrylicController.IsSupported())
            {
                m_acrylicController = new DesktopAcrylicController();
                m_acrylicController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);
            }
            else if (MicaController.IsSupported())
            {
                m_micaController = new MicaController
                {
                    Kind = materialSetting == "MicaAlt" ? MicaKind.BaseAlt : MicaKind.Base
                };
                m_micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_micaController.SetSystemBackdropConfiguration(m_configurationSource);
            }

            // 更新标题栏以匹配新材质
            UpdateTitleBarColors();
        }

        private void UpdateBackdropTheme()
        {
            if (m_configurationSource == null)
                return;

            var rootElement = this.Content as FrameworkElement;
            if (rootElement != null)
            {
                m_configurationSource.Theme = rootElement.ActualTheme switch
                {
                    ElementTheme.Dark => SystemBackdropTheme.Dark,
                    ElementTheme.Light => SystemBackdropTheme.Light,
                    _ => SystemBackdropTheme.Default
                };
            }
        }

        private void UpdateTitleBarColors()
        {
            if (!AppWindowTitleBar.IsCustomizationSupported())
                return;

            var titleBar = m_appWindow.TitleBar;
            var rootElement = this.Content as FrameworkElement;
            var isDark = rootElement?.ActualTheme == ElementTheme.Dark;

            if (isDark)
            {
                // 深色主题
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF);
                titleBar.ButtonPressedForegroundColor = Colors.White;
                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(0x0A, 0xFF, 0xFF, 0xFF);
                titleBar.ButtonInactiveForegroundColor = Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF);
            }
            else
            {
                // 浅色主题
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonHoverForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x14, 0x00, 0x00, 0x00);
                titleBar.ButtonPressedForegroundColor = Colors.Black;
                titleBar.ButtonPressedBackgroundColor = Color.FromArgb(0x0A, 0x00, 0x00, 0x00);
                titleBar.ButtonInactiveForegroundColor = Color.FromArgb(0x99, 0x00, 0x00, 0x00);
            }
        }
        private void NavView_SelectionChanged(NavigationView sender,
                                      NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                NavigateToPage(typeof(SettingsPage));
                return;
            }

            if (args.SelectedItem is NavigationViewItem item)
            {
                switch (item.Tag)
                {
                    case "Home":
                        NavigateToPage(typeof(HomePage));
                        break;
                }
            }
        }

        private void NavigateToPage(Type pageType)
        {
            // 如果当前页面就是要导航的页面，则不导航
            if (ContentFrame.CurrentSourcePageType == pageType)
                return;

            ContentFrame.Navigate(pageType);
        }
        public void BringToFront()
        {
            // 使用 AppWindow 的原生方法
            var presenter = m_appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                // 恢复最小化
                if (presenter.State == OverlappedPresenterState.Minimized)
                {
                    presenter.Restore();
                }

                // 移到最前面
                presenter.IsAlwaysOnTop = true;
                presenter.IsAlwaysOnTop = false;
            }

            // 激活窗口
            this.Activate();
        }
        private void CleanupBackdrop()
        {
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }

            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }

            m_configurationSource = null;
        }

        // ========== 启动画面 ==========
        public void ShowSplashOverlay()
        {
            SplashOverlay.Visibility = Visibility.Visible;
            SplashOverlay.Opacity = 1;

            var hwnd = WindowNative.GetWindowHandle(this);
            uint dpi = GetDpiForWindow(hwnd);
            string scaleSuffix = dpi >= 288 ? "400" : dpi >= 192 ? "200" : "100";
            string imagePath = $"Assets/SplashScreen.scale-{scaleSuffix}.png";

            try
            {
                SplashImage.Source = new BitmapImage(new Uri($"ms-appx:///{imagePath}"));
            }
            catch
            {
                try
                {
                    SplashImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/SplashScreen.png"));
                }
                catch { }
            }
        }

        public void HideSplashOverlay()
        {
            var visual = ElementCompositionPreview.GetElementVisual(SplashOverlay);
            var compositor = visual.Compositor;

            var fadeAnimation = compositor.CreateScalarKeyFrameAnimation();
            fadeAnimation.InsertKeyFrame(1f, 0f);
            fadeAnimation.Duration = TimeSpan.FromMilliseconds(200);

            var batch = compositor.CreateScopedBatch(Microsoft.UI.Composition.CompositionBatchTypes.Animation);
            visual.StartAnimation(nameof(visual.Opacity), fadeAnimation);
            batch.End();

            batch.Completed += (s, e) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    SplashOverlay.Visibility = Visibility.Collapsed;
                    visual.Opacity = 0f;
                });
            };
        }
    }

    // 设置辅助类
    public static class SettingsHelper
    {
        private static Windows.Storage.ApplicationDataContainer localSettings =
            Windows.Storage.ApplicationData.Current.LocalSettings;

        public static string GetTheme()
        {
            try
            {
                if (localSettings.Values.ContainsKey("Theme"))
                    return localSettings.Values["Theme"].ToString();
            }
            catch { }
            return "System";
        }

        public static void SetTheme(string theme)
        {
            try
            {
                localSettings.Values["Theme"] = theme;
            }
            catch { }
        }

        public static string GetMaterial()
        {
            try
            {
                if (localSettings.Values.ContainsKey("Material"))
                    return localSettings.Values["Material"].ToString();
            }
            catch { }
            return "Mica";
        }

        public static void SetMaterial(string material)
        {
            try
            {
                localSettings.Values["Material"] = material;
            }
            catch { }
        }
    }
}