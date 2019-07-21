using MusicPlayer.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Viewmodels
{
    public class PlayListViewmodel : INotifyPropertyChanged
    {


        public ReadOnlyObservableCollection<PlayList> PlayList => MusicStore.Instance.PlayLists;

        public event PropertyChangedEventHandler PropertyChanged;

        public PlayListViewmodel()
        {
            MusicStore.Instance.PropertyChanged += this.Instance_PropertyChanged;
        }

        private void Instance_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MusicStore.Instance.PlayLists))
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.PlayList)));
        }
    }
}
