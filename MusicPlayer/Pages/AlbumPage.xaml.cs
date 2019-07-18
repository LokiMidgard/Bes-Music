using MusicPlayer.Core;
using MusicPlayer.Viewmodels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Storage.FileProperties;
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
    public sealed partial class AlbumPage : Page
    {
        public AlbumPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var album = e.Parameter as Viewmodels.AlbumViewmodel;
            this.DataContext = album;
            var cover = ConnectedAnimationService.GetForCurrentView().GetAnimation("forwardAnimationCover");
            if (cover != null)
                cover.TryStart(this.cover);
            var name = ConnectedAnimationService.GetForCurrentView().GetAnimation("forwardAnimationName");
            if (name != null)
                name.TryStart(this.name);

            await this.SetCover(album);
        }

        private async System.Threading.Tasks.Task SetCover(Viewmodels.AlbumViewmodel album)
        {
            var cover = await album.LoadCoverAsync(default);
            this.cover.Source = cover;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var song = button.Tag as SongGroup;
            await Viewmodels.MediaplayerViewmodel.Instance.AddSong(song.Songs.First());



            //        public Task<MediaSource> GetMediaSource(CancellationToken cancel) => LibraryRegistry<MediaSource, StorageItemThumbnail>.Get(this.item.LibraryProvider).GetMediaSource(this.item.LibraryMediaId, cancel);
            //        public Task<StorageItemThumbnail> GetCover(int size, CancellationToken cancel) => LibraryRegistry<MediaSource, StorageItemThumbnail>.Get(this.item.LibraryProvider).GetImage(this.item.LibraryImageId, size, cancel);


        }

        private void List_ItemClick(object sender, ItemClickEventArgs e)
        {

        }
    }
}
