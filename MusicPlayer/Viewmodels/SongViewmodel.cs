using System;
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

        internal Task Play()
        {
            return App.Shell.PlaySong(this.item);
        }

        // Using a DependencyProperty as the backing store for timeSpan.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(TimeSpan), typeof(SongViewmodel), new PropertyMetadata(TimeSpan.Zero));




        private Song item;
        private ILibrary<MediaSource, StorageItemThumbnail> library;
        private AlbumViewmodel albumViewmodel;

        public SongViewmodel(Song x, ILibrary<MediaSource, StorageItemThumbnail> library, AlbumViewmodel albumViewmodel)
        {
            this.item = x;
            this.library = library;
            this.albumViewmodel = albumViewmodel;
            MusicStore.SongCollectionChanged += this.MusicStore_SongCollectionChanged;
            this.Initilize();
        }

        private void Initilize()
        {
            this.Discnumber = this.item.DiscNumber;
            this.Title = this.item.Name;
            this.Track = this.item.Track;
            this.Duration = this.item.Duration;

        }

        private void MusicStore_SongCollectionChanged(object sender, SongCollectionChangedEventArgs e)
        {
            if (e.Song.Equals(this.item))
            {

            }
        }
    }
}