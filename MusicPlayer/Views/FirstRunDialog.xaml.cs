using MusicPlayer.Pages;
using MusicPlayer.Services;

using System;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MusicPlayer.Views
{
    public sealed partial class FirstRunDialog : ContentDialog
    {
        public FirstRunDialog()
        {
            // TODO WTS: Update the contents of this dialog with any important information you want to show when the app is used for the first time.
            this.RequestedTheme = (Window.Current.Content as FrameworkElement).RequestedTheme;
            this.DefaultButton = ContentDialogButton.Primary;
            this.PrimaryButtonClick += this.PrimaryButtonClicked;

            this.InitializeComponent();
        }

        private void PrimaryButtonClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            OneDriveLibrary.instance.SyncronizeCommand.Execute(null);
        }
    }
}
