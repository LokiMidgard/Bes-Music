using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MusicPlayer.Core;
using Windows.Media.Core;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;

namespace MusicPlayer.Viewmodels
{
    public class SongViewmodel : DependencyObject
    {



        public int Discnumber
        {
            get { return (int)this.GetValue(DiscnumberProperty); }
            set { this.SetValue(DiscnumberProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Discnumber.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DiscnumberProperty =
            DependencyProperty.Register("Discnumber", typeof(int), typeof(SongViewmodel), new PropertyMetadata(1));



        public string Title
        {
            get { return (string)this.GetValue(TitleProperty); }
            set { this.SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(SongViewmodel), new PropertyMetadata(String.Empty));



        public int Track
        {
            get { return (int)this.GetValue(TrackProperty); }
            set { this.SetValue(TrackProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Track.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TrackProperty =
            DependencyProperty.Register("Track", typeof(int), typeof(SongViewmodel), new PropertyMetadata(0));



        public TimeSpan Duration
        {
            get { return (TimeSpan)this.GetValue(DurationProperty); }
            set { this.SetValue(DurationProperty, value); }
        }

        public AlbumViewmodel AlbumViewmodel { get; }

        internal Task Play()
        {
            return App.Shell.PlaySong(this);
        }

        // Using a DependencyProperty as the backing store for timeSpan.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(TimeSpan), typeof(SongViewmodel), new PropertyMetadata(TimeSpan.Zero));





        public IReadOnlyList<string> Interprets
        {
            get { return (IReadOnlyList<string>)GetValue(InterpretsProperty); }
            set { SetValue(InterpretsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Artists.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InterpretsProperty =
            DependencyProperty.Register("Interprets", typeof(IReadOnlyList<string>), typeof(SongViewmodel), new PropertyMetadata(null));



        public IReadOnlyList<string> Composers
        {
            get { return (IReadOnlyList<string>)GetValue(ComposersProperty); }
            set { SetValue(ComposersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Composers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ComposersProperty =
            DependencyProperty.Register("Composers", typeof(IReadOnlyList<string>), typeof(SongViewmodel), new PropertyMetadata(null));



        public IReadOnlyList<string> Genres
        {
            get { return (IReadOnlyList<string>)GetValue(GenresProperty); }
            set { SetValue(GenresProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Genres.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GenresProperty =
            DependencyProperty.Register("Genres", typeof(IReadOnlyList<string>), typeof(SongViewmodel), new PropertyMetadata(null));



        public int Year
        {
            get { return (int)GetValue(YearProperty); }
            set { SetValue(YearProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Year.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty YearProperty =
            DependencyProperty.Register("Year", typeof(int), typeof(SongViewmodel), new PropertyMetadata(0));





        private Song item;
        private ILibrary<MediaSource, StorageItemThumbnail> library;

        public SongViewmodel(Song x, ILibrary<MediaSource, StorageItemThumbnail> library, AlbumViewmodel albumViewmodel)
        {
            this.item = x;
            this.library = library;
            this.AlbumViewmodel = albumViewmodel;
            MusicStore.SongCollectionChanged += this.MusicStore_SongCollectionChanged;
            this.Initilize();
        }

        private void Initilize()
        {
            this.Discnumber = this.item.DiscNumber;
            this.Title = this.item.Name;
            this.Track = this.item.Track;
            this.Duration = this.item.Duration;
            this.Interprets = this.item.Interprets.Select(x => x.Name).ToList().AsReadOnly();
            this.Composers = this.item.Composers.Select(x => x.Name).ToList().AsReadOnly();
            this.Genres = this.item.Genre.Select(x => x.Name).ToList().AsReadOnly();
            this.Year = (int)this.item.Year;
        }

        public Task<MediaSource> GetMediaSource(CancellationToken cancel) => LibraryRegistry<MediaSource, StorageItemThumbnail>.Get(this.item.LibraryProvider).GetMediaSource(this.item.LibraryMediaId, cancel);
        public Task<StorageItemThumbnail> GetCover(int size, CancellationToken cancel) => LibraryRegistry<MediaSource, StorageItemThumbnail>.Get(this.item.LibraryProvider).GetImage(this.item.LibraryImageId, size, cancel);

        private void MusicStore_SongCollectionChanged(object sender, SongCollectionChangedEventArgs e)
        {
            if (e.Song.Equals(this.item))
            {

            }
        }
    }
}