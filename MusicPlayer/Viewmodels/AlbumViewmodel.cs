using MusicPlayer.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            MusicStore.Instance.Initilize(this.RunOnDispatcher);
            this.Albums = new ReadOnlyObservableCollection<AlbumViewmodel>(this.albums);

            this.alphabetGrouped = new GroupedObservableCollection<char, AlbumViewmodel>(x =>
            {

                var c = x.Model.Title.FirstOrDefault();
                if (c == default)
                {
                    return 'Ø';
                }
                if (c >= 'a' && c <= 'z'
                    || c >= 'A' && c <= 'Z'
                    )
                    return char.ToUpper(c);

                if (c >= '0' && c <= '9')
                    return '#';

                return '';
            }, Comparer<char>.Default, new AlbumViewmodelComparer());
            this.AlphabetGrouped = new ReadOnlyObservableCollection<SortedGroup<char, AlbumViewmodel>>(this.alphabetGrouped);

            

            MusicStore.Instance.PropertyChanged += this.Instance_PropertyChanged;

        }

        private void Instance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MusicStore.Albums))
                this.UpdateCollection();
        }

        private void UpdateCollection()
        {
            foreach (var item in MusicStore.Instance.Albums)
            {
                this.Add(item);
            }

                    (MusicStore.Instance.Albums as INotifyCollectionChanged).CollectionChanged += this.AlbumCollectionViewmodel_CollectionChanged1;
            InitAsync();
        }

        private static async void InitAsync()
        {
        }

        private void AlbumCollectionViewmodel_CollectionChanged1(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (var item in e.NewItems.OfType<Album>())
                    this.Add(item);
            if (e.OldItems != null)
                foreach (var item in e.OldItems.OfType<Album>())
                    this.Remove(item);
        }

        private Task RunOnDispatcher(Func<Task> f)
        {
            if (this.Dispatcher.HasThreadAccess)
                return f();
            return this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => f()).AsTask();
        }

        private void Add(Album model)
        {
            var vm = new AlbumViewmodel(model);
            this.alphabetGrouped.Add(vm);
            this.albums.Add(vm);
        }
        private void Remove(Album model)
        {
            // We have identety over model!
            var vm = new AlbumViewmodel(model);
            this.alphabetGrouped.Remove(vm);
            this.albums.Remove(vm);
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

    public class AlbumViewmodel : DependencyObject, IEquatable<AlbumViewmodel>
    {



        public Album Model { get; }



        private WeakReference<BitmapImage> cover;

        public async Task<ImageSource> LoadCoverAsync(CancellationToken cancellationToken)
        {
            if (this.cover != null && this.cover.TryGetTarget(out var target))
                return target;

            var stream = await this.Model.GetCover(300, cancellationToken);

            if (stream is null)
                return null;

            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(await stream.OpenReadAsync());
            this.cover = new WeakReference<BitmapImage>(bitmapImage);
            return bitmapImage;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as AlbumViewmodel);
        }

        public bool Equals(AlbumViewmodel other)
        {
            return other != null &&
                   EqualityComparer<Album>.Default.Equals(this.Model, other.Model);
        }

        public override int GetHashCode()
        {
            return -623947254 + EqualityComparer<Album>.Default.GetHashCode(this.Model);
        }

        public AlbumViewmodel(Album item)
        {
            this.Model = item ?? throw new ArgumentNullException(nameof(item));
        }

        public static bool operator ==(AlbumViewmodel viewmodel1, AlbumViewmodel viewmodel2)
        {
            return EqualityComparer<AlbumViewmodel>.Default.Equals(viewmodel1, viewmodel2);
        }

        public static bool operator !=(AlbumViewmodel viewmodel1, AlbumViewmodel viewmodel2)
        {
            return !(viewmodel1 == viewmodel2);
        }
    }
}
