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

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // 单实例检查
            var currentInstance = AppInstance.GetCurrent();
            var activationArgs = currentInstance.GetActivatedEventArgs();
            var mainInstance = AppInstance.FindOrRegisterForKey("OfflineInsiderEnroll-Main");

            // 如果不是主实例，重定向激活并退出
            if (!mainInstance.IsCurrent)
            {
                await mainInstance.RedirectActivationToAsync(activationArgs);
                Process.GetCurrentProcess().Kill();
                return;
            }

            // 注册激活事件处理（用于已有实例时的聚焦）
            mainInstance.Activated += OnAppActivated;

            // 创建主窗口
            var win = new MainWindow();
            MainWindowInstance = win;
            MainWindow = win;

            // 立即显示启动画面（在 Activate 之前）
            win.ShowSplashOverlay();

            // 激活窗口（秒开）
            win.Activate();

            // 异步初始化其他内容
            _ = InitializeAppAsync();
        }

        private void OnAppActivated(object sender, AppActivationArguments e)
        {
            // 当尝试打开第二个实例时，聚焦主窗口
            if (MainWindowInstance != null)
            {
                MainWindowInstance.DispatcherQueue.TryEnqueue(() =>
                {
                    MainWindowInstance.BringToFront();
                });
            }
        }

        private async Task InitializeAppAsync()
        {
            // 最小延迟，仅用于过渡
            await Task.Delay(1500);

            // 隐藏启动画面
            MainWindowInstance?.HideSplashOverlay();
        }
    }
}