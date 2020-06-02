using MusicPlayer.Controls;
using MusicPlayer.Core;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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

        private readonly System.Threading.SemaphoreSlim semaphore = new System.Threading.SemaphoreSlim(1, 1);

        public int CurrentPlayingIndex
        {
            get { return (int)this.GetValue(CurrentPlayingIndexProperty); }
            set { this.SetValue(CurrentPlayingIndexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentPlayingIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentPlayingIndexProperty =
            DependencyProperty.Register("CurrentPlayingIndex", typeof(int), typeof(MediaplayerViewmodel), new PropertyMetadata(-1, CurrentPlayingIndexChanged));

        private async void CurrentPlayingIndexChanged(int newIndex)
        {
            //using (await this.semaphore.Lock())
            {
                if (newIndex > -1)
                {
                    var newItem = this.CurrentPlaylist[newIndex];
                    this.transportControls.CurrentMediaPlaybackItem = newItem.MediaPlaybackItem;
                }
                else
                {
                    this.transportControls.CurrentMediaPlaybackItem = null;
                }
            }
        }
        private static async void CurrentPlayingIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = (MediaplayerViewmodel)d;
            //using (await me.semaphore.Lock())
            {
                var newIndex = (int)e.NewValue;
                //me.CurrentPlayingIndexChanged(newIndex);
            }
        }

        private MediaplayerViewmodel(TransportControls transportControls)
        {
            this.transportControls = transportControls;

            this.mediaPlaybackList = new MediaPlaybackList();
            //this.singleRepeatPlaylist = new MediaPlaybackList();
            //this._mediaPlaybackList.CurrentItemChanged += this._mediaPlaybackList_CurrentItemChanged;
            this.transportControls.PlayList = this.mediaPlaybackList;

            this.currentPlaylist = new ObservableCollection<PlayingSong>();
            this.CurrentPlaylist = new ReadOnlyObservableCollection<PlayingSong>(this.currentPlaylist);

            transportControls.RegisterPropertyChangedCallback(TransportControls.IsShuffledProperty, async (sender, e) =>
            {
                //using (await this.semaphore.Lock())
                this.ResetSorting();
            });

            transportControls.RegisterPropertyChangedCallback(TransportControls.CurrentMediaPlaybackItemProperty, (sender, e) => this.RefresCurrentIndex());

        }

        private async void RefresCurrentIndex()
        {
            //using (await this.semaphore.Lock())
            {
                var currentItem = this.transportControls.CurrentMediaPlaybackItem;
                int index;
                if (currentItem is null)
                    index = -1;
                else
                {
                    var viewmodel = this.playbackItemLookup[currentItem];
                    index = this.CurrentPlaylist.IndexOf(viewmodel);
                }
                this.CurrentPlayingIndex = index;
            }
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
            var currentItem = this.transportControls.CurrentMediaPlaybackItem;
            this.currentPlaylist.Clear();

            foreach (var item in
                this.mediaPlaybackList.ShuffleEnabled
                ? this.mediaPlaybackList.ShuffledItems
                : this.mediaPlaybackList.Items as IEnumerable<MediaPlaybackItem>)
                this.currentPlaylist.Add(this.playbackItemLookup[item]);

            var newIndex = this.currentPlaylist.Select((value, index) => (value, index)).First(x => x.value.MediaPlaybackItem.Equals(currentItem)).index;

            this.CurrentPlayingIndex = newIndex;
        }


        public async Task Play()
        {
            if (!this.Dispatcher.HasThreadAccess)
            {
                var completionSource = new TaskCompletionSource<object>();
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await this.Play();
                    completionSource.SetResult(null);
                });
                await completionSource.Task;
            }
            else
                using (await this.semaphore.Lock())
                    this.PlayInternal();
        }

        private void PlayInternal()
        {
            this.transportControls.IsPlaying = true;
        }

        public async Task ClearSongs()
        {
            if (!this.Dispatcher.HasThreadAccess)
            {
                var completionSource = new TaskCompletionSource<object>();
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await this.ClearSongs();
                    completionSource.SetResult(null);
                });
                await completionSource.Task;
            }
            else
            {
                using (await this.semaphore.Lock())
                {
                    this.ClearSongsInternal();
                }
            }
        }

        private void ClearSongsInternal()
        {
            this.mediaPlaybackList.Items.Clear();
            this.playbackItemLookup.Clear();
            foreach (var item in this.mediaItemLookup)
                item.Value.Clear();
            this.currentPlaylist.Clear();
            this.mediaItemLookup.Clear();
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
            else
            {
                using (await this.semaphore.Lock())
                {
                    this.mediaPlaybackList.Items.Remove(song.MediaPlaybackItem);
                    this.playbackItemLookup.Remove(song.MediaPlaybackItem);
                    var list = this.mediaItemLookup[song.Song];
                    this.currentPlaylist.Remove(song);
                    list.Remove(song);
                    if (list.Count == 0)
                        this.mediaItemLookup.Remove(song.Song);
                }
            }
        }

        private CancellationTokenSource resetCancelation = new CancellationTokenSource();
        public async Task ResetSongs(ImmutableArray<Song> songs, Song startingSong = null)
        {
            if (!this.Dispatcher.HasThreadAccess)
            {
                var completionSource = new TaskCompletionSource<object>();
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    var result = this.ResetSongs(songs);
                    completionSource.SetResult(result);
                });
                await completionSource.Task;
            }
            else
            {
                var currentCancelation = new CancellationTokenSource();

                var oldCancelation = Interlocked.Exchange(ref this.resetCancelation, currentCancelation);
                oldCancelation.Cancel();
                oldCancelation.Dispose();

                using (await this.semaphore.Lock())
                {
                    this.ClearSongsInternal();

                    bool first = true;
                    foreach (var s in songs)
                    {
                        if (currentCancelation.IsCancellationRequested)
                            break;
                        await this.AddSongInternal(s);
                        if (startingSong == null && first)
                        {
                            first = false;
                            await Task.Delay(1000); // I HATE THIS ThERE MUST BE A BETTER WAY!!!
                            this.PlayInternal();
                        }
                    }
                    if (!currentCancelation.IsCancellationRequested)
                    {
                        if (startingSong != null)
                        {
                            var newIndex = this.CurrentPlaylist.Select((value, index) => (value, index)).FirstOrDefault(x => x.value.Song == startingSong).index;
                            //this.CurrentPlayingIndex = newIndex;
                            this.transportControls.CurrentMediaPlaybackItem = this.CurrentPlaylist[newIndex].MediaPlaybackItem;
                            await Task.Delay(3); // I HATE THIS ThERE MUST BE A BETTER WAY!!!
                            this.PlayInternal();
                        }
                        else if (!this.transportControls.IsPlaying)
                        {
                            this.PlayInternal();
                        }
                    }
                }
            }
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
            using (await this.semaphore.Lock())
            {
                return await this.AddSongInternal(song);
            }
        }

        private async Task<PlayingSong> AddSongInternal(Song song)
        {
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
                    media = await LibraryRegistry<MediaSource, Uri>.Get(song.LibraryProvider).GetMediaSource(song.MediaId, default);
                else
                    oldProperties = oldMedia.GetDisplayProperties();
            }
            else
            {
                list = new List<PlayingSong>();
                this.mediaItemLookup.Add(song, list);
                media = await LibraryRegistry<MediaSource, Uri>.Get(song.LibraryProvider).GetMediaSource(song.MediaId, default);
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
                indexAdded = this.mediaPlaybackList.Items.Count - 1;

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

    internal static class SemaphoreExtensions
    {
        public static async Task<IDisposable> Lock(this System.Threading.SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            return new LockRelease(semaphore);
        }

        private sealed class LockRelease : IDisposable
        {
            private readonly SemaphoreSlim semaphore;
            private bool disposedValue = false; // To detect redundant calls

            public LockRelease(SemaphoreSlim semaphore)
            {
                this.semaphore = semaphore;
            }

            #region IDisposable Support
            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                if (!this.disposedValue)
                {
                    this.semaphore.Release();
                    this.disposedValue = true;
                }
            }
            #endregion

        }
    }
}
