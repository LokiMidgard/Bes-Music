using MusicPlayer.Controls;
using MusicPlayer.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MusicPlayer.Viewmodels
{
    public class PlayListViewmodel : INotifyPropertyChanged
    {


        public ReadOnlyObservableCollection<PlayList> PlayList => App.Current.MusicStore.PlayLists;

        public ICommand PlayCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public PlayListViewmodel()
        {
            App.Current.MusicStore.PropertyChanged += this.Instance_PropertyChanged;

            this.PlayCommand = new DelegateCommand<PlayList>(async (song) =>
            {
                await App.Current.MediaplayerViewmodel.ResetSongs(song.Songs.ToImmutableArray(), null);
            });

        }

        private void Instance_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(App.Current.MusicStore.PlayLists))
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.PlayList)));
        }
    }
}
