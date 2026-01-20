using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;

namespace OfflineInsiderEnrollUI.Dialogs
{
    public sealed partial class ConfirmEnrollDialog : ContentDialog
    {
        private readonly string _channelName;

        public ConfirmEnrollDialog(string channelName)
        {
            InitializeComponent();
            _channelName = channelName;

            this.Loaded += ConfirmEnrollDialog_Loaded;
        }

        private void ConfirmEnrollDialog_Loaded(object sender, RoutedEventArgs e)
        {
            var loader = ResourceLoader.GetForViewIndependentUse();
            string template = loader.GetString("ConfirmEnrollDialog_Content");
            ConfirmEnrollDialog_Content.Text = string.Format(template, _channelName);
        }
    }
}