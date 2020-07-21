using MusicPlayer.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.UI.Xaml;

namespace MusicPlayer.Viewmodels
{
    public class AlbumCollectionViewmodel : DependencyObject, IDisposable
    {


        //private readonly ILibrary<MediaSource, StorageItemThumbnail> library = LocalLibrary.Instance;

        private readonly ObservableCollection<AlbumViewmodel> albums = new ObservableCollection<AlbumViewmodel>();
        public ReadOnlyObservableCollection<AlbumViewmodel> Albums { get; }

        private readonly GroupedObservableCollection<char, AlbumViewmodel> alphabetGrouped;
        private bool disposedValue;

        public ReadOnlyObservableCollection<SortedGroup<char, AlbumViewmodel>> AlphabetGrouped { get; }

        private MusicStore musicStore;

        public AlbumCollectionViewmodel()
        {
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



            this.musicStore = App.Current.MusicStore;
            this.musicStore.PropertyChanged += this.Instance_PropertyChanged;

        }

        private void Instance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MusicStore.Albums))
                this.UpdateCollection();
        }

        private void UpdateCollection()
        {
            foreach (var item in App.Current.MusicStore.Albums)
            {
                this.Add(item);
            }

            (this.musicStore.Albums as INotifyCollectionChanged).CollectionChanged += this.AlbumCollectionViewmodel_CollectionChanged1;
            this.InitAsync();
        }

        private void InitAsync()
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

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.musicStore.PropertyChanged -= this.Instance_PropertyChanged;
                    (this.musicStore.Albums as INotifyCollectionChanged).CollectionChanged -= this.AlbumCollectionViewmodel_CollectionChanged1;
                }
                this.musicStore = null;
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AlbumCollectionViewmodel()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
