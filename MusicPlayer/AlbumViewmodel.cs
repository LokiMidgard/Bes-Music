using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MusicPlayer.Core;
using Windows.Media.Core;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MusicPlayer
{
    public class AlbumCollectionViewmodel : DependencyObject
    {
        private readonly LocalLibrary library = new LocalLibrary();

        private readonly ObservableCollection<AlbumViewmodel> albums = new ObservableCollection<AlbumViewmodel>();

        public ReadOnlyObservableCollection<AlbumViewmodel> Albums { get; }

        public AlbumCollectionViewmodel()
        {
            this.Albums = new ReadOnlyObservableCollection<AlbumViewmodel>(this.albums);
            _ = this.InitilizeAsync();
        }

        private async Task InitilizeAsync()
        {
            MusicStore.AlbumCollectionChanged += this.MusicStore_AlbumCollectionChanged;
            using (var store = await MusicStore.CreateContextAsync(default))
                foreach (var item in store.Albums)
                    this.albums.Add(new AlbumViewmodel(item, this.library));

            _ = this.library.Update(default);
        }

        private void MusicStore_AlbumCollectionChanged(object sender, AlbumCollectionChangedEventArgs e)
        {
            if (e.Action.HasFlag(AlbumChanges.Added))
            {
                _ = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    this.albums.Add(new AlbumViewmodel(e.Album, this.library));
                });
            }
        }
    }

    public class AlbumViewmodel : DependencyObject
    {



        public string Name
        {
            get { return (string)this.GetValue(NameProperty); }
            set { this.SetValue(NameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Name.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register("Name", typeof(string), typeof(AlbumViewmodel), new PropertyMetadata(null));




        public async Task<ImageSource> LoadCoverAsync(CancellationToken cancellationToken)
        {

            String id;
            if (this.item.LibraryImages.Any())

                id = this.item.LibraryImages.FirstOrDefault();
            else
                id = null;


            if (id != null)
            {
                var thumbnail = await this.library.GetImageRetryAsync(id, 300, cancellationToken);
                if (thumbnail != null)
                {
                    var bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(thumbnail);
                    return bitmapImage;
                }
            }


            return null;

        }


        private Album item;
        private readonly ILibrary<MediaSource, StorageItemThumbnail> library;

        public AlbumViewmodel(Album item, ILibrary<MediaSource, StorageItemThumbnail> library)
        {
            this.item = item;
            this.library = library;
            MusicStore.AlbumCollectionChanged += this.MusicStore_AlbumCollectionChanged;
            _ = this.Initilize();
        }

        private async Task Initilize()
        {

            this.Name = this.item.Name;

        }

        private void MusicStore_AlbumCollectionChanged(object sender, AlbumCollectionChangedEventArgs e)
        {
            if (e.Action.HasFlag(AlbumChanges.ImageUpdated))
            {
                _ = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    this.item = e.Album;
                    await this.Initilize();
                });
            }
        }
    }
}
