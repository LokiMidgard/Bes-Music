using MusicPlayer.Controls;
using MusicPlayer.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;

namespace MusicPlayer.Viewmodels
{
    public class MediaPlayerAccessor : INotifyPropertyChanged
    {
        public MediaplayerViewmodel Instance => MediaplayerViewmodel.Instance;

        public event PropertyChangedEventHandler PropertyChanged;

        public MediaPlayerAccessor()
        {
            this.Init();
        }

        private async void Init()
        {
            if (MediaplayerViewmodel.Initilized.IsCompleted)
                return;
            await MediaplayerViewmodel.Initilized;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Instance)));
        }
    }

    public class MediaplayerViewmodel : DependencyObject
    {
        public static MediaplayerViewmodel Instance { get; private set; }
        public static Task Initilized { get; }
        private static readonly TaskCompletionSource<object> initilized;
        static MediaplayerViewmodel()
        {
            initilized = new TaskCompletionSource<object>();
            Initilized = initilized.Task;
        }


        private readonly TransportControls transportControls;
        private readonly MediaPlaybackList mediaPlaybackList;
        //private readonly MediaPlaybackList singleRepeatPlaylist;
        private readonly Dictionary<Song, List<PlayingSong>> mediaItemLookup = new Dictionary<Song, List<PlayingSong>>();
        private readonly Dictionary<MediaPlaybackItem, PlayingSong> playbackItemLookup = new Dictionary<MediaPlaybackItem, PlayingSong>();

        public ReadOnlyObservableCollection<PlayingSong> CurrentPlaylist { get; }
        private readonly ObservableCollection<PlayingSong> currentPlaylist;



        public int CurrentPlayingIndex
        {
            get { return (int)GetValue(CurrentPlayingIndexProperty); }
            set { SetValue(CurrentPlayingIndexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentPlayingIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentPlayingIndexProperty =
            DependencyProperty.Register("CurrentPlayingIndex", typeof(int), typeof(MediaplayerViewmodel), new PropertyMetadata(-1));







        private MediaplayerViewmodel(TransportControls transportControls)
        {
            this.transportControls = transportControls;

            this.mediaPlaybackList = new MediaPlaybackList();
            //this.singleRepeatPlaylist = new MediaPlaybackList();
            //this._mediaPlaybackList.CurrentItemChanged += this._mediaPlaybackList_CurrentItemChanged;
            this.transportControls.PlayList = this.mediaPlaybackList;

            this.currentPlaylist = new ObservableCollection<PlayingSong>();
            this.CurrentPlaylist = new ReadOnlyObservableCollection<PlayingSong>(this.currentPlaylist);

            transportControls.RegisterPropertyChangedCallback(TransportControls.IsShuffledProperty, (sender, e) =>
            {
                this.ResetSorting();
            });

            transportControls.RegisterPropertyChangedCallback(TransportControls.CurrentMediaPlaybackItemProperty, (sender, e) => this.RefresCurrentIndex());

        }

        private void RefresCurrentIndex()
        {
            var currentItem = this.transportControls.CurrentMediaPlaybackItem;
            var viewmodel = this.playbackItemLookup[currentItem];
            var index = this.CurrentPlaylist.IndexOf(viewmodel);
            this.CurrentPlayingIndex = index;
        }

        public static void Init(TransportControls transportControls)
        {
            if (Instance != null)
                throw new InvalidOperationException("Already Initilized");

            Instance = new MediaplayerViewmodel(transportControls);
            initilized.SetResult(null);
        }

        private void ResetSorting()
        {
            this.currentPlaylist.Clear();

            foreach (var item in this.mediaPlaybackList.ShuffledItems)
                this.currentPlaylist.Add(this.playbackItemLookup[item]);
            this.RefresCurrentIndex();
        }

        public async Task RemoveSong(PlayingSong song)
        {
            if (!this.Dispatcher.HasThreadAccess)
            {
                var completionSource = new TaskCompletionSource<object>();
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await this.RemoveSong(song);
                    completionSource.SetResult(null);
                });
                await completionSource.Task;
            }
            this.mediaPlaybackList.Items.Remove(song.MediaPlaybackItem);
            this.playbackItemLookup.Remove(song.MediaPlaybackItem);
            var list = this.mediaItemLookup[song.Song];
            this.currentPlaylist.Remove(song);
            list.Remove(song);
            if (list.Count == 0)
                this.mediaItemLookup.Remove(song.Song);
        }

        public async Task<PlayingSong> AddSong(Song song)
        {
            if (!this.Dispatcher.HasThreadAccess)
            {
                var completionSource = new TaskCompletionSource<Task<PlayingSong>>();
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    var result = this.AddSong(song);
                    completionSource.SetResult(result);
                });
                return await completionSource.Task.Unwrap();
            }

            Task getCover;
            MediaSource media;
            List<PlayingSong> list;
            MediaItemDisplayProperties oldProperties = null;
            if (this.mediaItemLookup.ContainsKey(song))
            {
                list = this.mediaItemLookup[song];
                var oldMedia = list.FirstOrDefault()?.MediaPlaybackItem;
                media = oldMedia?.Source;
                if (media is null) // Async could add list before mediasource is added.
                    media = await LibraryRegistry<MediaSource, StorageItemThumbnail>.Get(song.LibraryProvider).GetMediaSource(song.MediaId, default);
                else
                    oldProperties = oldMedia.GetDisplayProperties();
            }
            else
            {
                list = new List<PlayingSong>();
                this.mediaItemLookup.Add(song, list);
                media = await LibraryRegistry<MediaSource, StorageItemThumbnail>.Get(song.LibraryProvider).GetMediaSource(song.MediaId, default);
            }

            var mediaItem = new MediaPlaybackItem(media);
            var viewModel = new PlayingSong(mediaItem, song);
            this.playbackItemLookup.Add(mediaItem, viewModel);
            list.Add(viewModel);

            if (oldProperties is null)
            {
                var displayProperties = mediaItem.GetDisplayProperties();
                displayProperties.Type = Windows.Media.MediaPlaybackType.Music;
                displayProperties.MusicProperties.AlbumTitle = song.AlbumName;
                displayProperties.MusicProperties.TrackNumber = (uint)song.Track;
                displayProperties.MusicProperties.Title = song.Title;

                displayProperties.MusicProperties.Genres.Clear();
                foreach (var genre in song.Genres)
                    displayProperties.MusicProperties.Genres.Add(genre);

                displayProperties.MusicProperties.Artist = string.Join(", ", song.Interpreters);

                mediaItem.ApplyDisplayProperties(displayProperties);

                var coverTask = song.GetCover(300, default);
                getCover = coverTask.ContinueWith(t =>
                {
                    var coverStreamReferance = t.Result;
                    displayProperties.Thumbnail = coverStreamReferance;
                    mediaItem.ApplyDisplayProperties(displayProperties);
                });
            }
            else
                getCover = null;

            this.mediaPlaybackList.Items.Add(mediaItem);

            int indexAdded;
            if (this.mediaPlaybackList.ShuffleEnabled)
                indexAdded = this.mediaPlaybackList.ShuffledItems.Select((value, index) => (value, index)).First(x => x.value == mediaItem).index;
            else
                indexAdded = this.mediaPlaybackList.Items.Count-1;

            this.currentPlaylist.Insert(indexAdded, viewModel);

            if (getCover != null)
                await getCover;
            return viewModel;
        }
    }

    public class PlayingSong
    {
        public PlayingSong(MediaPlaybackItem mediaPlaybackItem, Song song)
        {
            this.MediaPlaybackItem = mediaPlaybackItem;
            this.Song = song;
        }

        public MediaPlaybackItem MediaPlaybackItem { get; }
        public Song Song { get; }
    }
}
