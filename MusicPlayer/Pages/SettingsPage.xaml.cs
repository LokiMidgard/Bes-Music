using Microsoft.Graph;
using Microsoft.Toolkit.Services.MicrosoftGraph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace MusicPlayer.Pages
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private static bool initilized;

        public IEnumerable<StorageLocation> PossiblePlcations { get; } = new StorageLocation[] { StorageLocation.AppData, StorageLocation.Music };

        public SettingsPage()
        {
            this.InitializeComponent();
            if (!initilized)
            {
                initilized = true;

            }

            this.Loaded += this.SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            //const string path = @"C:\Users\patri\AppData\Local\Packages\e54df8f3-1f95-4f15-9ab1-9a28d699ee70_d86wfgk7f981m\LocalState\cover\634529B99B5BB82C!63260";
        }

        private void Login_SignInCompleted(object sender, Microsoft.Toolkit.Uwp.UI.Controls.Graph.SignInEventArgs e)
        {
            //// Set the auth state
            //SetAuthState(true);
            //// Reload the home page
            //RootFrame.Navigate(typeof(HomePage));
        }

        private void Login_SignOutCompleted(object sender, EventArgs e)
        {
            //// Set the auth state
            //SetAuthState(false);
            //// Reload the home page
            //RootFrame.Navigate(typeof(HomePage));
        }


        

        private async void ExceptionHandlerConverter_OnError(Exception exception)
        {
            var dialog = new MessageDialog(exception.Message);
            await dialog.ShowAsync();
        }

        private async System.Threading.Tasks.Task<bool> OneDriveAccessor_OnAskForPermission(string messageText)
        {
            var dialog = new MessageDialog(messageText)
            {
                Options = MessageDialogOptions.AcceptUserInputAfterDelay
            };

            var completionSorce = new TaskCompletionSource<bool>();
            var yesCommand = new UICommand("Yes", cmd => completionSorce.SetResult(true));
            var cancelCommand = new UICommand("Cancel", cmd => completionSorce.SetResult(false));

            dialog.Commands.Add(yesCommand);
            dialog.Commands.Add(cancelCommand);

            dialog.DefaultCommandIndex = 1;
            dialog.CancelCommandIndex = 1;
            await dialog.ShowAsync();

            return await completionSorce.Task;
        }
    }

}
