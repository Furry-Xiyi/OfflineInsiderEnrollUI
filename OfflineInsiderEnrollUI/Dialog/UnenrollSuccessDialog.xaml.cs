using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;

namespace OfflineInsiderEnrollUI.Dialogs
{
    public sealed partial class UnenrollSuccessDialog : ContentDialog
    {
        public UnenrollSuccessDialog(bool needReboot)
        {
            InitializeComponent();

            var loader = new ResourceLoader();
            ContentText.Text = needReboot
                ? loader.GetString("UnenrollSuccessDialog.Content.NeedReboot")
                : loader.GetString("UnenrollSuccessDialog.Content.NoReboot");
        }
    }
}