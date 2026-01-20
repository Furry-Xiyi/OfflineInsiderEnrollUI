using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;

namespace OfflineInsiderEnrollUI.Dialogs
{
    public sealed partial class ErrorDialog : ContentDialog
    {
        public ErrorDialog(string message)
        {
            InitializeComponent();

            var loader = ResourceLoader.GetForViewIndependentUse();
            string template = loader.GetString("ErrorDialog_Content");
            ErrorDialog_Content.Text = string.Format(template, message);
        }
    }
}