using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;

namespace OfflineInsiderEnrollUI.Dialogs
{
    public sealed partial class EnrollSuccessDialog : ContentDialog
    {
        public EnrollSuccessDialog(bool needReboot)
        {
            InitializeComponent();

            var loader = new ResourceLoader();
            string text = needReboot
                ? loader.GetString("EnrollSuccessDialog.Content.NeedReboot")
                : loader.GetString("EnrollSuccessDialog.Content.NoReboot");

            Content = new TextBlock
            {
                Text = text,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                MaxWidth = 400
            };
        }
    }
}