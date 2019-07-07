using MusicPlayer.Core;
using MusicPlayer.Viewmodels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MusicPlayer.Controls
{
    public sealed partial class PlayListControl : UserControl
    {
        public PlayListControl()
        {
            this.InitializeComponent();
        }

        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is PlayList playList)
            {
                await MediaplayerViewmodel.Instance.ClearSongs();
                foreach (var song in playList.Songs)
                    await MediaplayerViewmodel.Instance.AddSong(song);
                await MediaplayerViewmodel.Instance.Play();
            }
        }
    }
}
