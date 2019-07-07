using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

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

        public Guid Id { get; }

        public ReadOnlyObservableCollection<Song> Songs { get; }
        internal readonly ObservableCollection<Song> songs;

        public PlayList(string name, Guid id)
        {
            songs = new ObservableCollection<Song>();
            Songs = new ReadOnlyObservableCollection<Song>(songs);
            this.Name = name;
            this.Id = id;
        }

    }
}