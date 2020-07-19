using MusicPlayer.Core;
using MusicPlayer.Viewmodels;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace MusicPlayer.Pages
{


    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class ShellPage : Page
    {

        public ShellViewModel ViewModel { get; } = new ShellViewModel();

        public new Frame Frame => this.shellFrame;

        private bool showPlayUi = true;
        public bool ShowPlayUi
        {
            get
            {
                return this.showPlayUi;
            }
            set
            {
                if (this.showPlayUi == value)
                    return;
                //var rootGrid = (Grid)this.FindName("RootGrid");
                //mediaPlayer.AreTransportControlsEnabled = value;
                VisualStateManager.GoToState(this.TransportControls, value ? "show" : "hide", true);
                this.showPlayUi = value;
            }
        }

        public ShellPage()
        {
            this.InitializeComponent();
            this.DataContext = this.ViewModel;


            MediaplayerViewmodel.Init(this.TransportControls);


            IList<KeyboardAccelerator> keyboardAccelerators;
            try
            {
                keyboardAccelerators = this.KeyboardAccelerators;

            }
            catch (InvalidCastException)
            {

                keyboardAccelerators = new List<KeyboardAccelerator>();
            }
            this.ViewModel.Initialize(this.shellFrame, null, keyboardAccelerators);

            this.Loaded += this.ShellPage_Loaded;
        }

        private async void ShellPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.ShowPlayUi = false;
            _ = OneDriveLibrary.Instance;
            _ = AlbumCollectionViewmodel.Instance;

            var hideLoding = this.LoadingGrid.Resources["HideAnimation"] as Storyboard;
            hideLoding.Completed += (sender2, e2) =>
              {
                  this.LoadingGrid.Visibility = Visibility.Collapsed;
                  this.ProgreessIndecator.IsActive = false;
              };
            await Core.MusicStore.Instance.Init();
            await MusicStore.Instance.SetUITask(this.RunOnDispatcher);

            NetworkViewmodel.Instance.OnError += this.Network_OnError;

            this.ShowPlayUi = true;
            hideLoding.Begin();
        }

        private async void Network_OnError(object sender, EventArgs<Exception> e)
        {

            string caption = null;

            if (sender is DownloadItem downloadItem)
            {
                var albumName = downloadItem.Song?.AlbumName;
                var songTitle = downloadItem.Song?.Title;
                if (albumName is null && songTitle is null)
                {
                    caption = downloadItem.Title;
                    if (caption is null)
                        caption = "a download";

                }
                else
                {
                    if (albumName is null)
                        caption = songTitle;
                    if (songTitle is null)
                        caption = albumName;
                    else
                        caption = $"{albumName} - {songTitle}";
                }
            }

            if (caption is null)
                caption = "There was some problem with the Network.";
            else
            {
                caption = $"There was some probelm with {caption}";
            }

            string message;

            if (e?.Argument?.Message != null)
                message = e.Argument.Message;
            else if (e.Argument != null)
                message = e.Argument.ToString();
            else
                message = "We could not find the error. Sorry this should not happen.";

            await this.RunOnDispatcher(async () =>
            {
                var dialog = new MessageDialog(message, caption)
                {
                    Options = MessageDialogOptions.None
                };
                await dialog.ShowAsync();
            });
        }

        private Task RunOnDispatcher(Func<Task> f)
        {
            if (this.Dispatcher.HasThreadAccess)
                return f();
            return this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => f()).AsTask();
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
