using MusicPlayer.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Viewmodels
{
    public class PlayListViewmodel
    {
        public ReadOnlyObservableCollection<PlayList> PlayList => MusicStore.Instance.PlayLists;
    }
}
