using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace MusicPlayer.Core
{
    public class PlayList : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        private string name;

        public string Name
        {
            get { return this.name; }
            set
            {
                if (this.name != value)
                {
                    this.name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Name)));
                }
            }
        }


        private Availability availability;

        public Availability Availability
        {
            get { return this.availability; }
            private set
            {
                if (this.availability != value)
                {
                    this.availability = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Availability)));
                }
            }
        }

        public ICommand PlayCommand { get; }



        public Guid Id { get; }

        public ReadOnlyObservableCollection<Song> Songs { get; }
        internal readonly ObservableCollection<Song> songs;

        public PlayList(string name, Guid id)
        {
            this.songs = new ObservableCollection<Song>();
            this.Songs = new ReadOnlyObservableCollection<Song>(this.songs);



            this.songs.CollectionChanged += this.Songs_CollectionChanged;
            this.Availability = this.songs.Select(x => x.Availability).OrderBy(x => (int)x).FirstOrDefault();

            this.Name = name;
            this.Id = id;
        }

        private void Songs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {

            if (e.NewItems != null)
                foreach (Song song in e.NewItems)
                {
                    song.PropertyChanged += this.Song_PropertyChanged;
                }

            if (e.OldItems != null)
                foreach (Song song in e.OldItems)
                {
                    song.PropertyChanged -= this.Song_PropertyChanged;
                }

            this.Availability = this.songs.Select(x => x.Availability).OrderByDescending(x => (int)x).FirstOrDefault();
        }

        private void Song_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.Availability = this.songs.Select(x => x.Availability).OrderByDescending(x => (int)x).FirstOrDefault();
        }
    }
}