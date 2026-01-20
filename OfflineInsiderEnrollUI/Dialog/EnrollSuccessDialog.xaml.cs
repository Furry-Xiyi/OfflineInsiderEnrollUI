using Microsoft.UI.Xaml.Controls;

namespace OfflineInsiderEnrollUI.Dialogs
{
    public sealed partial class EnrollSuccessDialog : ContentDialog
    {
        public EnrollSuccessDialog(bool needReboot)
        {
            InitializeComponent();

            NeedRebootText.Visibility = needReboot
                ? Microsoft.UI.Xaml.Visibility.Visible
                : Microsoft.UI.Xaml.Visibility.Collapsed;

            NoRebootText.Visibility = needReboot
                ? Microsoft.UI.Xaml.Visibility.Collapsed
                : Microsoft.UI.Xaml.Visibility.Visible;
        }
    }
}