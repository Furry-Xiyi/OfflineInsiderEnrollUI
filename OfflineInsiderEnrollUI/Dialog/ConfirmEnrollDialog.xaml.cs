using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;

namespace OfflineInsiderEnrollUI.Dialogs
{
    public sealed partial class ConfirmEnrollDialog : ContentDialog
    {
        public ConfirmEnrollDialog(string channelName)
        {
            InitializeComponent();

            var loader = new ResourceLoader();
            string template = loader.GetString("ConfirmEnrollDialog.Content");

            Content = new TextBlock
            {
                Text = string.Format(template, channelName),
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                MaxWidth = 400
            };
        }
    }
}