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


        public ReadOnlyObservableCollection<PlayList> PlayList => MusicStore.Instance.PlayLists;

        public ICommand PlayCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public PlayListViewmodel()
        {
            MusicStore.Instance.PropertyChanged += this.Instance_PropertyChanged;

            this.PlayCommand = new DelegateCommand<PlayList>(async (song) =>
            {
                await MediaplayerViewmodel.Instance.ResetSongs(song.Songs.ToImmutableArray(), null);
            });

        }

        private void Instance_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MusicStore.Instance.PlayLists))
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.PlayList)));
        }
    }
}
