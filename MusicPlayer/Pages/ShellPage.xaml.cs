using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using MusicPlayer.Core;
using MusicPlayer.Viewmodels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace MusicPlayer.Pages
{


    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class ShellPage : Page
    {
        private readonly MediaPlaybackList _mediaPlaybackList;

        public ShellViewModel ViewModel { get; } = new ShellViewModel();

        public ShellPage()
        {
            this.InitializeComponent();
            this.DataContext = this.ViewModel;

            this._mediaPlaybackList = new MediaPlaybackList();
            this._mediaPlaybackList.CurrentItemChanged += this._mediaPlaybackList_CurrentItemChanged;
            IList<KeyboardAccelerator> keyboardAccelerators;
            try
            {
                keyboardAccelerators = this.KeyboardAccelerators;

            }
            catch (InvalidCastException)
            {

                keyboardAccelerators = new List<KeyboardAccelerator>();
            }
            this.ViewModel.Initialize(this.shellFrame, null, keyboardAccelerators);

        }

        private void _mediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {

        }

        public async Task PlaySong(Song song)
        {
            var media = await LocalLibrary.Instance.GetMediaSource(song.LibraryMediaId);
            var mediaItem = new MediaPlaybackItem(media);

            var displayProperties = mediaItem.GetDisplayProperties();
            displayProperties.Type = Windows.Media.MediaPlaybackType.Music;
            displayProperties.MusicProperties.AlbumTitle = song.AlbumName;
            displayProperties.MusicProperties.TrackNumber = (uint)song.Track;
            displayProperties.MusicProperties.Title = song.Name;

            displayProperties.MusicProperties.Genres.Clear();
            foreach (var genre in song.Genre.Select(x => x.Name))
                displayProperties.MusicProperties.Genres.Add(genre);

            displayProperties.MusicProperties.Artist = string.Join(", ", song.Artist.Select(x => x.Name));

            mediaItem.ApplyDisplayProperties(displayProperties);

            this._mediaPlaybackList.Items.Add(mediaItem);

            if (this._mediaPlaybackList.Items.Count > 0)
                this.mediaPlayer.Source = this._mediaPlaybackList;

            var storageItemThumbnail = await LocalLibrary.Instance.GetImageRetryAsync(song.LibraryImageId, 300, default);
            displayProperties.Thumbnail = RandomAccessStreamReference.CreateFromStream(storageItemThumbnail);
            mediaItem.ApplyDisplayProperties(displayProperties);

        }


    }
}
