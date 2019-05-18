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

namespace MusicPlayer.Viewmodels
{
    public class AlbumCollectionViewmodel : DependencyObject
    {

        public static readonly AlbumCollectionViewmodel Instance = new AlbumCollectionViewmodel();

        //private readonly ILibrary<MediaSource, StorageItemThumbnail> library = LocalLibrary.Instance;

        private readonly ObservableCollection<AlbumViewmodel> albums = new ObservableCollection<AlbumViewmodel>();
        public ReadOnlyObservableCollection<AlbumViewmodel> Albums { get; }

        private readonly GroupedObservableCollection<char, AlbumViewmodel> alphabetGrouped;
        public ReadOnlyObservableCollection<SortedGroup<char, AlbumViewmodel>> AlphabetGrouped { get; }

        private AlbumCollectionViewmodel()
        {
            this.Albums = new ReadOnlyObservableCollection<AlbumViewmodel>(this.albums);

            this.alphabetGrouped = new GroupedObservableCollection<char, AlbumViewmodel>(x =>
            {

                var c = x.Name.FirstOrDefault();
                if (c >= 'a' && c <= 'z'
                    || c >= 'A' && c <= 'Z'
                    )
                    return char.ToUpper(c);

                if (c >= '0' && c <= '9')
                    return '#';

                return '';
            }, Comparer<char>.Default, new AlbumViewmodelComparer());
            this.AlphabetGrouped = new ReadOnlyObservableCollection<SortedGroup<char, AlbumViewmodel>>(this.alphabetGrouped);

            this.InitilizeAsync();
        }

        public void Add(AlbumViewmodel model)
        {
            this.alphabetGrouped.Add(model);
            this.albums.Add(model);
        }
        public void Remove(AlbumViewmodel model)
        {
            this.alphabetGrouped.Remove(model);
            this.albums.Remove(model);
        }

        private async void InitilizeAsync()
        {
            MusicStore.AlbumCollectionChanged += this.MusicStore_AlbumCollectionChanged;

            foreach (var item in await MusicStore.GetAlbums())
            {
                this.Add(new AlbumViewmodel(item, LibraryRegistry<MediaSource, StorageItemThumbnail>.Get(item.LibraryProvider)));
            }

            await LocalLibrary.Instance.Update(default);

        }

        private void MusicStore_AlbumCollectionChanged(object sender, AlbumCollectionChangedEventArgs e)
        {
            if (e.Action.HasFlag(AlbumChanges.Added))
            {
                _ = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    var albume = await MusicStore.GetAlbum(e.AlbumName, e.AlbumInterpret, e.ProviderId);
                    this.Add(new AlbumViewmodel(albume, LibraryRegistry<MediaSource, StorageItemThumbnail>.Get(albume.LibraryProvider)));
                });
            }
        }
    }


    public class GroupedObservableCollection<TKey, TValue> : ObservableCollection<SortedGroup<TKey, TValue>>
    {
        private readonly Func<TValue, TKey> keySelector;
        private readonly IComparer<SortedGroup<TKey, TValue>> groupComparer;
        private readonly IComparer<TValue> valueComparer;
        private readonly Dictionary<TKey, SortedGroup<TKey, TValue>> keyLookup = new Dictionary<TKey, SortedGroup<TKey, TValue>>();

        public GroupedObservableCollection(Func<TValue, TKey> keySelector, IComparer<TKey> groupComparer, IComparer<TValue> valueComparer)
        {
            this.keySelector = keySelector;
            this.groupComparer = new GroupComparer(groupComparer);
            this.valueComparer = valueComparer;
        }

        public void Add(TValue value)
        {
            var key = this.keySelector(value);
            SortedGroup<TKey, TValue> group;
            if (this.keyLookup.ContainsKey(key))
                group = this.keyLookup[key];
            else
            {
                group = new SortedGroup<TKey, TValue>(key, this.valueComparer);
                this.keyLookup.Add(key, group);
                var index = this.BinarySearch(group, this.groupComparer);
                if (index < 0)
                    index = ~index;
                this.Insert(index, group);
            }

            group.Add(value);
        }

        public void Remove(TValue value)
        {
            var key = this.keySelector(value);
            if (this.keyLookup.ContainsKey(key))
            {
                var group = this.keyLookup[key];
                group.Remove(value);
                if (!group.HasItems)
                {
                    this.Remove(group);
                    this.keyLookup.Remove(key);
                }
            }
        }

        private class GroupComparer : IComparer<SortedGroup<TKey, TValue>>
        {
            private readonly IComparer<TKey> groupComparer;

            public GroupComparer(IComparer<TKey> groupComparer) => this.groupComparer = groupComparer;

            public int Compare(SortedGroup<TKey, TValue> x, SortedGroup<TKey, TValue> y) => this.groupComparer.Compare(x.Key, y.Key);
        }

    }

    public class SortedGroup<TKey, TValue> : ObservableCollection<TValue>, IGrouping<TKey, TValue>
    {
        /// <summary>
        /// The Group Title
        /// </summary>
        public TKey Key
        {
            get;
        }

        private readonly IComparer<TValue> comparer;

        /// <summary>
        /// Constructor ensure that a Group Title is included
        /// </summary>
        /// <param name="key">string to be used as the Group Title</param>
        public SortedGroup(TKey key, IComparer<TValue> comparer)
        {
            this.Key = key;
            this.comparer = comparer;
        }

        /// <summary>
        /// Returns true if the group has a count more than zero
        /// </summary>
        public bool HasItems
        {
            get
            {
                return (this.Count != 0);
            }

        }

        public new void Add(TValue value)
        {
            var index = this.BinarySearch(value, this.comparer);
            if (index < 0)
                index = ~index;
            this.Insert(index, value);
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



        public IEnumerable<string> Interprets
        {
            get { return (IEnumerable<string>)this.GetValue(InterpretsProperty); }
            set { this.SetValue(InterpretsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Interprets.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InterpretsProperty =
            DependencyProperty.Register("Interprets", typeof(IEnumerable<string>), typeof(AlbumViewmodel), new PropertyMetadata(new string[0]));




        public IEnumerable<SongViewmodel> Songs
        {
            get { return (IEnumerable<SongViewmodel>)this.GetValue(SongsProperty); }
            set { this.SetValue(SongsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Songs.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SongsProperty =
            DependencyProperty.Register("Songs", typeof(IEnumerable<SongViewmodel>), typeof(AlbumViewmodel), new PropertyMetadata(new SongViewmodel[0]));



        private WeakReference<BitmapImage> cover;

        public async Task<ImageSource> LoadCoverAsync(CancellationToken cancellationToken)
        {
            if (this.cover != null && this.cover.TryGetTarget(out var target))
                return target;
            string id;
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
                    this.cover = new WeakReference<BitmapImage>(bitmapImage);
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
            this.Initilize();
        }

        private void Initilize()
        {

            this.Name = this.item.Title;

            this.Interprets = this.item.Interpreters;

            this.Songs = this.item.Songs.Select(x => new SongViewmodel(x, this.library, this)).ToArray();

        }

        private async void MusicStore_AlbumCollectionChanged(object sender, AlbumCollectionChangedEventArgs e)
        {
            if (e.AlbumName.Equals(this.item.Title) && e.AlbumInterpret == this.item.AlbumInterpret && e.ProviderId == this.item.LibraryProvider)
            {
                var albume = await MusicStore.GetAlbum(e.AlbumName, e.AlbumInterpret, e.ProviderId);
                if (e.Action.HasFlag(AlbumChanges.ImageUpdated))
                {
                    _ = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        this.item = albume;
                        this.Initilize();
                    });
                }
                if (e.Action.HasFlag(AlbumChanges.SongsUpdated))
                {
                    _ = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        this.item = albume;
                        this.Initilize();
                    });
                }
            }
        }
    }
}
