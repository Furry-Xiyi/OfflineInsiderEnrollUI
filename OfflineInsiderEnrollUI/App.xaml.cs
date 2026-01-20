using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OfflineInsiderEnrollUI
{
    public partial class App : Application
    {
        public static Window MainWindow { get; private set; }
        public static MainWindow MainWindowInstance { get; private set; }

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // ========== 单实例检查 ==========
            // 直接查找或注册主实例
            var mainInstance = AppInstance.FindOrRegisterForKey("MAIN");
            var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

            // 如果不是当前实例（说明已经有一个实例在运行）
            if (!mainInstance.IsCurrent)
            {
                // 重定向激活到主实例
                var task = mainInstance.RedirectActivationToAsync(activationArgs);
                task.AsTask().Wait();

                // 退出当前实例
                Process.GetCurrentProcess().Kill();
                return;
            }

            // 是主实例，注册激活事件（用于后续的实例激活）
            mainInstance.Activated += OnActivated;

            // ========== 创建窗口 ==========
            var win = new MainWindow();
            MainWindowInstance = win;
            MainWindow = win;

            // 立即显示启动画面
            win.ShowSplashOverlay();

            // 激活窗口(秒开)
            win.Activate();

            // 异步初始化其他内容
            _ = InitializeAppAsync();
        }

        private void OnActivated(object sender, AppActivationArguments e)
        {
            // 当有新实例尝试启动时,激活主窗口
            MainWindowInstance?.DispatcherQueue.TryEnqueue(() =>
            {
                MainWindowInstance.BringToFront();
            });
        }

        private async Task InitializeAppAsync()
        {
            // 最小延迟,仅用于过渡
            await Task.Delay(1500);

            // 隐藏启动画面
            MainWindowInstance?.HideSplashOverlay();
        }
    }
}