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

        public static MainWindow Instance { get; private set; }
        private IntPtr _hwnd;
        private IntPtr _oldWndProc;
        private WndProcDelegate _newWndProc;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            InitializeAppWindow();      // 合并窗口初始化
            InitializeAppearance();     // Mica / Acrylic
            HookMinWindowSize();        // 最小窗口尺寸

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            Activated += MainWindow_Activated;
            Closed += MainWindow_Closed;

            ContentFrame.Navigate(typeof(HomePage));
            NavView.SelectedItem = HomeItem;
        }
        private void InitializeAppWindow()
        {
            _hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
            m_appWindow = AppWindow.GetFromWindowId(windowId);

            // 本地化标题
            var rm = new Microsoft.Windows.ApplicationModel.Resources.ResourceManager();
            m_appWindow.Title = rm.MainResourceMap.GetValue("Resources/AppTitle/Text").ValueAsString;

            // 图标
            m_appWindow.SetIcon("Assets/AppIcon.ico");

            // 初始大小
            m_appWindow.Resize(new SizeInt32(580, 945));

            // 自定义标题栏按钮
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = m_appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;

                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                // 失焦时按钮灰化（官方行为）
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonInactiveForegroundColor = Color.FromArgb(255, 120, 120, 120);
            }
        }
        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            bool isActive = args.WindowActivationState != WindowActivationState.Deactivated;

            // 你的标题栏文本控件
            AppTitle.Opacity = isActive ? 1.0 : 0.6;

            // Mica 输入状态
            if (m_configurationSource != null)
                m_configurationSource.IsInputActive = isActive;
        }

        private void HookMinWindowSize()
        {
            _newWndProc = CustomWndProc;
            _oldWndProc = SetWindowLongPtr(_hwnd, -4,
                Marshal.GetFunctionPointerForDelegate(_newWndProc));
        }

        private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            const int WM_GETMINMAXINFO = 0x0024;

            if (msg == WM_GETMINMAXINFO)
            {
                var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

                mmi.ptMinTrackSize.x = 580;
                mmi.ptMinTrackSize.y = 700;

                Marshal.StructureToPtr(mmi, lParam, fDeleteOld: false);
                return IntPtr.Zero;
            }

            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newProc);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
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

            // 让 Overlay 全屏覆盖
            SplashOverlay.HorizontalAlignment = HorizontalAlignment.Stretch;
            SplashOverlay.VerticalAlignment = VerticalAlignment.Stretch;

            // 使用应用背景色（与你另一个应用一致）
            SplashOverlay.Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];

            // DPI 选择 scale 资源
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            uint dpi = GetDpiForWindow(hwnd);
            string scaleSuffix = dpi >= 288 ? "400" : dpi >= 192 ? "200" : "100";
            string imagePath = $"Assets/SplashScreen.scale-{scaleSuffix}.png";

            // Splash 图片拉伸方式（与你另一个应用一致）
            SplashImage.Stretch = Stretch.Uniform;

            // 直接加载 ms-appx 资源（不再使用 try/catch）
            SplashImage.Source = new BitmapImage(new Uri($"ms-appx:///{imagePath}"));
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