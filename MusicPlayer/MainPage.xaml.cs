using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        //private MediaPlayer player;

        public AlbumCollectionViewmodel Albums => this.DataContext as AlbumCollectionViewmodel;

        public MainPage()
        {
            this.InitializeComponent();

            //this.player = new MediaPlayer();

            this.InitMusicLibraryAsync();



            //this.renderTarget.RenderAsync(toRender);
        }


        public async Task InitMusicLibraryAsync()
        {
            //var files = await this.GetFilesAsync(KnownFolders.MusicLibrary);
            ////files.Select(x=>x.Properties.GetMusicPropertiesAsync())
            //var p = await files.First().Properties.GetMusicPropertiesAsync();

            //var current = files.First(x=>x.Name.EndsWith(".mp3"));
            //this.player.Source = MediaSource.CreateFromStorageFile(current);
            ////this.player.Play();
            //this.player.AutoPlay = true;
        }

        private async void ToRender_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            var root = args.ItemContainer.ContentTemplateRoot as FrameworkElement;
            var image = root.FindName("cover") as Image;
            var vm = args.Item as AlbumViewmodel;
            if (args.Phase == 0)
            {
                var oldCancel = root.Tag as CancellationTokenSource;
                if (oldCancel != null)
                {
                    oldCancel.Cancel();
                    oldCancel.Dispose();
                }
                var cancel = new CancellationTokenSource();
                args.RegisterUpdateCallback(this.ToRender_ContainerContentChanging);
                root.Tag = cancel;
                image.Opacity = 0;
            }
            else if (args.Phase == 1)
            {
                var cancel = root.Tag as CancellationTokenSource;
                var imageSource = await vm.LoadCoverAsync(cancel.Token);
                if (!cancel.IsCancellationRequested)
                {
                    image.Source = imageSource;
                    image.Opacity = 1;
                }

            }

            args.Handled = true;

        }
    }
}
