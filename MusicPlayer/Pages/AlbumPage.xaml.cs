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
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
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
            await App.Current.MediaplayerViewmodel.AddSong(song.Songs.First());



            //        public Task<MediaSource> GetMediaSource(CancellationToken cancel) => LibraryRegistry<MediaSource, StorageItemThumbnail>.Get(this.item.LibraryProvider).GetMediaSource(this.item.LibraryMediaId, cancel);
            //        public Task<StorageItemThumbnail> GetCover(int size, CancellationToken cancel) => LibraryRegistry<MediaSource, StorageItemThumbnail>.Get(this.item.LibraryProvider).GetImage(this.item.LibraryImageId, size, cancel);


        }

        private void List_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private async void Cover_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var colorThief = new ColorThiefDotNet.ColorThief();
                if (this.cover.Source is BitmapImage imageSource)
                {
                    using (var stream = File.Open(imageSource.UriSour‌​ce.AbsolutePath, FileMode.Open))
                    {

                        //var random = RandomAccessStreamReference.CreateFromUri(imageSource.UriSour‌​ce.AbsolutePath);

                        //using (IRandomAccessStream stream = await random.OpenReadAsync())
                        {
                            var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
                            var x = await colorThief.GetColor(decoder);
                            this.headerBackground.Background = new SolidColorBrush(new Windows.UI.Color()
                            {
                                //A = x.Color.A,
                                A = 100,
                                R = x.Color.R,
                                G = x.Color.G,
                                B = x.Color.B
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {

            }

            //var encoder = Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(();
            //encoder.Frames.Add(BitmapFrame.Create(image));
            //encoder.Save(fileStream);

            //imageSource.
            //var decoed = Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(imageSource);
            //var x = colorThief.GetColor(imageSource);

        }

        private void ItemsStackPanel_BringIntoViewRequested(UIElement sender, BringIntoViewRequestedEventArgs args)
        {
            if (args.TargetRect.Height <= Helpers.ConstantsHelper.PlayListHeightField)
            {
                var t = args.TargetRect;
                t = new Rect(t.X, t.Y, t.Width, t.Height + Helpers.ConstantsHelper.PlayListHeightField);
                args.TargetRect = t;
            }
        }

        private void ItemsStackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsEventPresent("Windows.UI.Xaml.UIElement", nameof(this.PreviewKeyDown)))
            {
                var panel = sender as UIElement;
                panel.BringIntoViewRequested += this.ItemsStackPanel_BringIntoViewRequested;
            }

        }
    }
}
