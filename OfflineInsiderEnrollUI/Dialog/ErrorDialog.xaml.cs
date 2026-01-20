using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;

namespace OfflineInsiderEnrollUI.Dialogs
{
    public sealed partial class ErrorDialog : ContentDialog
    {
        public ErrorDialog(string message)
        {
            InitializeComponent();

            var loader = new ResourceLoader();
            string template = loader.GetString("ErrorDialog.Content");

            Content = new TextBlock
            {
                Text = string.Format(template, message),
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                MaxWidth = 400
            };
        }
    }
}