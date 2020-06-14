using Microsoft.EntityFrameworkCore;

using Nito.AsyncEx;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MusicPlayer.Core
{
    public class MusicStore : INotifyPropertyChanged
    {
        private readonly AsyncManualResetEvent initilisation = new AsyncManualResetEvent(false);
        private readonly AsyncManualResetEvent databaseLoad = new AsyncManualResetEvent(false);
        public static MusicStore Instance { get; } = new MusicStore();

        private readonly Dictionary<(string provider, string mediaId), Song> songLoockup = new Dictionary<(string provider, string mediaId), Song>();
        private readonly Dictionary<Guid, PlayList> playListLoockup = new Dictionary<Guid, PlayList>();

        public IEnumerable<string> Genres { get; private set; }
        public IEnumerable<string> Interpreters { get; private set; }
        public IEnumerable<string> Composers { get; private set; }
        public IEnumerable<(string providerId, string imageId)> LibraryImages { get; private set; }

        public bool IsInitilized => this.databaseLoad.IsSet;
        public Task WhenInitilized => this.initilisation.WaitAsync();

        private void UpdateProperties()
        {

            if (!this.IsInitilized)
                return;
            var oldGenres = this.Genres;
            var oldInterpreters = this.Interpreters;
            var oldComposers = this.Composers;
            var oldLibraryImages = this.LibraryImages;

            this.Genres = this.albums.SelectMany(x => x.Genres).Distinct().OrderBy(x => x).ToArray();
            this.Interpreters = this.albums.SelectMany(x => x.Interpreters).Distinct().OrderBy(x => x).ToArray();
            this.Composers = this.albums.SelectMany(x => x.Composers).Distinct().OrderBy(x => x).ToArray();
            this.LibraryImages = this.albums.SelectMany(x => x.LibraryImages).Distinct().OrderBy(x => x.providerId).ThenBy(x => x.imageId).ToArray();

            if (!(oldGenres?.SequenceEqual(this.Genres) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Genres)));

            if (!(oldInterpreters?.SequenceEqual(this.Interpreters) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Interpreters)));

            if (!(oldComposers?.SequenceEqual(this.Composers) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Composers)));

            if (!(oldLibraryImages?.SequenceEqual(this.LibraryImages) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.LibraryImages)));

        }


        private void InitilizeProperties()
        {

            foreach (var album in this.albums)
                album.InitilizeProperties();

            this.Genres = this.albums.SelectMany(x => x.Genres).Distinct().OrderBy(x => x).ToArray();
            this.Interpreters = this.albums.SelectMany(x => x.Interpreters).Distinct().OrderBy(x => x).ToArray();
            this.Composers = this.albums.SelectMany(x => x.Composers).Distinct().OrderBy(x => x).ToArray();
            this.LibraryImages = this.albums.SelectMany(x => x.LibraryImages).Distinct().OrderBy(x => x.providerId).ThenBy(x => x.imageId).ToArray();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Genres)));

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Interpreters)));

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Composers)));

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.LibraryImages)));

        }


        private MusicStore()
        {
            this.albums = new ObservableCollection<Album>();
            this.playLists = new ObservableCollection<PlayList>();
        }


        public async Task SetUITask(Func<Func<Task>, Task> invokeOnUi)
        {
            if (this.invoke != null)
                return;
            this.invoke = invokeOnUi;
            this.initilisation.Set();

            await this.databaseLoad.WaitAsync();
            await this.RunOnUIThread(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Albums)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.PlayLists)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsInitilized)));
            });
        }

        private Lazy<Task> initTask;
        public Task Init(CancellationToken cancellationToken = default)
        {
            var lazy = new Lazy<Task>(Work);
            var original = Interlocked.CompareExchange(ref this.initTask, lazy, null);
            if (original != null)
                return original.Value;
            return lazy.Value;

            Task Work()
            {
                return Task.Run(async () =>
                {
                    using (var context = await MusicStoreDatabase.CreateContextAsync(cancellationToken))
                    {
                        foreach (var song in await context.songs.ToArrayAsync())
                        {
                            song.TransferFromDatabase();
                            this.songLoockup.Add((song.LibraryProvider, song.MediaId), song);
                            this.AddSong(song);
                        }
                        foreach (var playListEntry in await context.playListEntrys.ToArrayAsync())
                        {
                            PlayList playList;
                            if (this.playListLoockup.ContainsKey(playListEntry.PlayListId))
                                playList = this.playListLoockup[playListEntry.PlayListId];
                            else
                            {
                                var nameEnttry = await context.PlayListName.FirstOrDefaultAsync(x => x.PlayListId == playListEntry.PlayListId);
                                playList = new PlayList(nameEnttry?.Name ?? string.Empty, nameEnttry.PlayListId);
                                this.playLists.Add(playList);
                                this.playListLoockup.Add(playList.Id, playList);

                            }
                            if (this.songLoockup.ContainsKey((playListEntry.LibraryProvider, playListEntry.MediaId)))
                                playList.songs.Add(this.songLoockup[(playListEntry.LibraryProvider, playListEntry.MediaId)]);
                        }
                    }
                    this.Albums = new ReadOnlyObservableCollection<Album>(this.albums);
                    this.PlayLists = new ReadOnlyObservableCollection<PlayList>(this.playLists);
                    this.InitilizeProperties();
                    this.databaseLoad.Set();





                });
            }

        }


        public event PropertyChangedEventHandler PropertyChanged;

        public ReadOnlyObservableCollection<Album> Albums { get; private set; }
        private readonly ObservableCollection<Album> albums;
        public ReadOnlyObservableCollection<PlayList> PlayLists { get; private set; }
        private readonly ObservableCollection<PlayList> playLists;


        private Func<Func<Task>, Task> invoke;



        public Task RemoveSong(Song song, CancellationToken cancelToken = default)
        {
            return this.RunOnUIThread(async () =>
            {
                using (var context = await MusicStoreDatabase.CreateContextAsync(cancelToken))
                {
                    this.RemoveSong(song, context);
                    await context.SaveChangesAsync();
                }
            });
        }

        public Task RemoveSong(IEnumerable<Song> songs, CancellationToken cancelToken = default)
        {
            songs = songs.ToArray();
            return this.RunOnUIThread(async () =>
            {
                using (var context = await MusicStoreDatabase.CreateContextAsync(cancelToken))
                {
                    foreach (var song in songs)
                        this.RemoveSong(song, context);
                    await context.SaveChangesAsync();
                }
            });
        }
        private void RemoveSong(Song song, MusicStoreDatabase context)
        {
            var insertionPoint = this.albums.BinarySearch(song, s => (Title: s.AlbumName, s.AlbumInterpret), a => (a.Title, a.AlbumInterpret), (x, y) =>
            {
                var result = x.Title.CompareTo(y.Title);
                if (result != 0)
                    return result;
                return x.AlbumInterpret.CompareTo(y.AlbumInterpret);
            });

            Album album;
            if (insertionPoint >= 0)
            {
                album = this.albums[insertionPoint];
                album.RemoveSong(song);
            }
            context.Remove(song);

        }

        public Song GetSongByMediaId(string providerId, string songId)
        {
            if (this.songLoockup.ContainsKey((providerId, songId)))
                return this.songLoockup[(providerId, songId)];
            return null;
        }

        public Task<PlayList> CreatePlaylist(string name, CancellationToken cancelToken = default)
        {
            return this.CreatePlaylist(name, Guid.NewGuid(), cancelToken);
        }
        public Task<PlayList> CreatePlaylist(string name, Guid id, CancellationToken cancelToken = default)
        {
            var newPlayList = new PlayList(name, id);

            return this.RunOnUIThread(async () =>
            {
                await this.databaseLoad.WaitAsync();
                using (var context = await MusicStoreDatabase.CreateContextAsync(cancelToken))
                {
                    var playlistEntry = new PlayListNameEntry(newPlayList.Name, newPlayList.Id);
                    await context.AddAsync(playlistEntry, cancelToken);
                    await context.SaveChangesAsync(cancelToken);
                    this.playLists.Add(newPlayList);
                }
                return newPlayList;
            });
        }

        public Task RenamePlaylist(PlayList playlist, string name, CancellationToken cancelToken = default)
        {

            return this.RunOnUIThread(async () =>
            {
                await this.databaseLoad.WaitAsync();
                using (var context = await MusicStoreDatabase.CreateContextAsync(cancelToken))
                {
                    var toRename = await context.PlayListName.FirstOrDefaultAsync(x => x.PlayListId == playlist.Id, cancelToken);
                    if (toRename is null)
                        throw new ArgumentOutOfRangeException(nameof(playlist), "The Provided Playlist is not found");

                    context.Attach(toRename);
                    toRename.Name = name;
                    await context.SaveChangesAsync(cancelToken);
                    playlist.Name = name;
                }
            });
        }

        public Task AddPlaylistSong(PlayList playlist, Song song, CancellationToken cancelToken = default)
        {

            return this.RunOnUIThread(async () =>
            {
                await this.databaseLoad.WaitAsync();
                using (var context = await MusicStoreDatabase.CreateContextAsync(cancelToken))
                {
                    var newEntry = new PlayListEntry(song.LibraryProvider, song.MediaId, playlist.Id);

                    await context.AddAsync(newEntry, cancelToken);
                    await context.SaveChangesAsync(cancelToken);

                    playlist.songs.Add(song);
                }
            });
        }
        public Task RemovePlaylistSong(PlayList playlist, Song song, CancellationToken cancelToken = default)
        {

            return this.RunOnUIThread(async () =>
            {
                await this.databaseLoad.WaitAsync();
                using (var context = await MusicStoreDatabase.CreateContextAsync(cancelToken))
                {
                    var newEntry = await context.playListEntrys.FirstOrDefaultAsync(x => x.LibraryProvider == song.LibraryProvider && x.MediaId == song.MediaId && x.PlayListId == playlist.Id, cancelToken);
                    if (newEntry is null)
                        return;
                    context.Remove(newEntry);
                    await context.SaveChangesAsync(cancelToken);
                    playlist.songs.Remove(song);
                }
            });
        }

        public Task RemovePlaylist(PlayList playlist, CancellationToken cancelToken = default)
        {

            return this.RunOnUIThread(async () =>
            {
                await this.databaseLoad.WaitAsync();
                using (var context = await MusicStoreDatabase.CreateContextAsync(cancelToken))
                {
                    context.playListEntrys.RemoveRange(await context.playListEntrys.AsQueryable().Where(x => x.PlayListId == playlist.Id).ToArrayAsync(cancelToken));
                    context.PlayListName.Remove(await context.PlayListName.FirstAsync(x => x.PlayListId == playlist.Id, cancelToken));
                    await context.SaveChangesAsync(cancelToken);
                    this.playLists.Remove(playlist);
                }
            });
        }

        public Task<Song> AddSong(string provider, string mediaId,
            string albumInterpret = default,
            string albumName = default,
            IEnumerable<string> composers = default,
            int discNumber = default,
            TimeSpan duration = default,
            IEnumerable<string> genres = default,
            IEnumerable<string> interpreters = default,
            string libraryImageId = default,
            string title = default,
            int track = default,
            uint year = default,
            CancellationToken cancelToken = default)
        {
            return this.RunOnUIThread(async () =>
            {
                await this.databaseLoad.WaitAsync();
                Song song;
                using (var context = await MusicStoreDatabase.CreateContextAsync(cancelToken))
                {
                    bool newSong;

                    if (this.songLoockup.ContainsKey((provider, mediaId)))
                    {
                        song = this.songLoockup[(provider, mediaId)];
                        context.Attach(song);
                        newSong = false;
                    }
                    else
                    {
                        song = new Song(provider, mediaId);
                        this.songLoockup.Add((provider, mediaId), song);
                        newSong = true;
                    }

                    if (albumInterpret != default)
                        song.AlbumInterpret = albumInterpret;
                    if (albumName != default)
                        song.AlbumName = albumName;
                    if (composers != default)
                        song.Composers = composers.ToImmutableSortedSet();
                    if (discNumber != default)
                        song.DiscNumber = discNumber;
                    if (duration != default)
                        song.Duration = duration;
                    if (genres != default)
                        song.Genres = genres.ToImmutableSortedSet();
                    if (interpreters != default)
                        song.Interpreters = interpreters.ToImmutableSortedSet();
                    if (libraryImageId != default)
                        song.LibraryImageId = libraryImageId;
                    if (title != default)
                        song.Title = title;
                    if (track != default)
                        song.Track = track;
                    if (year != default)
                        song.Year = year;

                    if (newSong)
                        context.Add(song);

                    await context.SaveChangesAsync();
                    this.AddSong(song);
                }
                return song;
            });
        }

        public Task UpdateSong(Song song,
            string albumInterpret = default,
            string albumName = default,
            IEnumerable<string> composers = default,
            int discNumber = default,
            TimeSpan duration = default,
            IEnumerable<string> genres = default,
            IEnumerable<string> interpreters = default,
            string libraryImageId = default,
            string title = default,
            int track = default,
            uint year = default,
            CancellationToken cancellationToken = default
            )
        {
            return this.RunOnUIThread(async () =>
            {
                await this.databaseLoad.WaitAsync();
                using (var context = await MusicStoreDatabase.CreateContextAsync(cancellationToken))
                {

                    if (albumInterpret != default)
                        song.AlbumInterpret = albumInterpret;
                    if (albumName != default)
                        song.AlbumName = albumName;
                    if (composers != default)
                        song.Composers = composers.ToImmutableSortedSet();
                    if (discNumber != default)
                        song.DiscNumber = discNumber;
                    if (duration != default)
                        song.Duration = duration;
                    if (genres != default)
                        song.Genres = genres.ToImmutableSortedSet();
                    if (interpreters != default)
                        song.Interpreters = interpreters.ToImmutableSortedSet();
                    if (libraryImageId != default)
                        song.LibraryImageId = libraryImageId;
                    if (title != default)
                        song.Title = title;
                    if (track != default)
                        song.Track = track;
                    if (year != default)
                        song.Year = year;


                    await context.SaveChangesAsync();
                }
            });
        }

        private async Task RunOnUIThread(Func<Task> func)
        {
            await this.initilisation.WaitAsync();

            await this.invoke(() =>
            {
                return func();
            });
        }

        private async Task<T> RunOnUIThread<T>(Func<Task<T>> func)
        {
            await this.initilisation.WaitAsync();
            var taskCompletionSource = new TaskCompletionSource<T>();
            await this.invoke(async () =>
            {
                try
                {
                    var result = await func();
                    taskCompletionSource.SetResult(result);

                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            });
            return await taskCompletionSource.Task;
        }

        private async Task RunOnUIThread(Action func)
        {
            await this.initilisation.WaitAsync();
            await this.invoke(() =>
            {
                func();
                return Task.CompletedTask;
            });
        }
        private async Task<T> RunOnUIThread<T>(Func<T> func)
        {
            await this.initilisation.WaitAsync();

            var taskCompletionSource = new TaskCompletionSource<T>();
            await this.invoke(() =>
            {
                try
                {
                    var result = func();
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
                return Task.CompletedTask;
            });
            return await taskCompletionSource.Task;
        }



        internal void AddSong(Song song)
        {
            var insertionPoint = this.albums.BinarySearch(song, s => (Title: s.AlbumName, s.AlbumInterpret), a => (a.Title, a.AlbumInterpret), (x, y) =>
            {
                var result = x.Title.CompareTo(y.Title);
                if (result != 0)
                    return result;
                return x.AlbumInterpret.CompareTo(y.AlbumInterpret);
            });

            Album album;
            if (insertionPoint < 0)
            {
                insertionPoint = ~insertionPoint;
                album = new Album(song.AlbumName, song.AlbumInterpret);
                album.PropertyChanged += this.Album_PropertyChanged;
                (album.Songs as INotifyCollectionChanged).CollectionChanged += this.MusicStore_CollectionChanged;

                this.albums.Insert(insertionPoint, album);
            }
            else
                album = this.albums[insertionPoint];

            album.AddSong(song);
        }

        private void MusicStore_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var changedSongGroup = this.albums.First(x => x.Songs == sender);
            if (changedSongGroup.Songs.Count == 0)
            {
                this.albums.Remove(changedSongGroup);
                changedSongGroup.PropertyChanged -= this.Album_PropertyChanged;
                (changedSongGroup.Songs as INotifyCollectionChanged).CollectionChanged -= this.MusicStore_CollectionChanged;
            }
        }

        private void Album_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Album.Genres):
                case nameof(Album.Interpreters):
                case nameof(Album.Composers):
                case nameof(Album.LibraryImages):
                    this.UpdateProperties();
                    break;
                default:
                    break;
            }
        }
    }

    public class Album : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        internal Album(string title, string albumInterpret)
        {
            this.Title = title ?? throw new ArgumentNullException(nameof(title));
            this.AlbumInterpret = albumInterpret ?? throw new ArgumentNullException(nameof(albumInterpret));

            this.songs = new ObservableCollection<SongGroup>();
            this.Songs = new ReadOnlyObservableCollection<SongGroup>(this.songs);
        }

        public string Title { get; }
        public string AlbumInterpret { get; }
        public ReadOnlyObservableCollection<SongGroup> Songs { get; }
        private readonly ObservableCollection<SongGroup> songs;

        internal void AddSong(Song song)
        {
            if (song.AlbumInterpret != this.AlbumInterpret
                || song.AlbumName != this.Title)
                throw new ArgumentException("Song must match the Properties of this SongGroup", nameof(song));

            var insertionIndex = this.songs.BinarySearch(song, s => (s.DiscNumber, s.Track, s.Title), s => (s.DiscNumber, s.Track, s.Title), (x, y) =>
            {
                var result = x.DiscNumber.CompareTo(y.DiscNumber);
                if (result != 0)
                    return result;
                result = x.Track.CompareTo(y.Track);
                if (result != 0)
                    return result;
                return x.Title.CompareTo(y.Title);
            });
            SongGroup songGroup;
            if (insertionIndex < 0)
            {
                insertionIndex = ~insertionIndex;
                songGroup = new SongGroup(this, song.Title, song.Track, song.DiscNumber);
                songGroup.PropertyChanged += this.SongGroup_PropertyChanged;
                (songGroup.Songs as INotifyCollectionChanged).CollectionChanged += this.Songroup_CollectionChanged;
                this.songs.Insert(insertionIndex, songGroup);
            }
            else
            {
                songGroup = this.songs[insertionIndex];
            }

            songGroup.AddSong(song);
        }

        internal void RemoveSong(Song song)
        {
            if (song.AlbumInterpret != this.AlbumInterpret
                || song.AlbumName != this.Title)
                throw new ArgumentException("Song must match the Properties of this SongGroup", nameof(song));

            var insertionIndex = this.songs.BinarySearch(song, s => (s.DiscNumber, s.Track, s.Title), s => (s.DiscNumber, s.Track, s.Title), (x, y) =>
            {
                var result = x.DiscNumber.CompareTo(y.DiscNumber);
                if (result != 0)
                    return result;
                result = x.Track.CompareTo(y.Track);
                if (result != 0)
                    return result;
                return x.Title.CompareTo(y.Title);
            });
            SongGroup songGroup;
            if (insertionIndex >= 0)
            {
                songGroup = this.songs[insertionIndex];
                songGroup.RemoveSong(song);
            }

        }

        private void Songroup_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var changedSongGroup = this.songs.First(x => x.Songs == sender);
            if (changedSongGroup.Songs.Count == 0)
            {
                this.songs.Remove(changedSongGroup);
                changedSongGroup.PropertyChanged -= this.SongGroup_PropertyChanged;
                (changedSongGroup.Songs as INotifyCollectionChanged).CollectionChanged -= this.Songroup_CollectionChanged;
            }
        }

        private void SongGroup_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SongGroup.Genres):
                case nameof(SongGroup.Interpreters):
                case nameof(SongGroup.Composers):
                case nameof(SongGroup.LibraryImages):
                    this.UpdateProperties();
                    break;
                default:
                    break;
            }
        }

        private void UpdateProperties()
        {

            if (!MusicStore.Instance.IsInitilized)
                return;

            var oldGenres = this.Genres;
            var oldInterpreters = this.Interpreters;
            var oldComposers = this.Composers;
            var oldLibraryImages = this.LibraryImages;

            this.Genres = this.Songs.SelectMany(x => x.Genres).Distinct().OrderBy(x => x).ToArray();
            this.Interpreters = this.Songs.SelectMany(x => x.Interpreters).Distinct().OrderBy(x => x).ToArray();
            this.Composers = this.Songs.SelectMany(x => x.Composers).Distinct().OrderBy(x => x).ToArray();
            this.LibraryImages = this.Songs.SelectMany(x => x.LibraryImages).Distinct().OrderBy(x => x.providerId).ThenBy(x => x.imageId).ToArray();

            if (!(oldGenres?.SequenceEqual(this.Genres) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Genres)));

            if (!(oldInterpreters?.SequenceEqual(this.Interpreters) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Interpreters)));

            if (!(oldComposers?.SequenceEqual(this.Composers) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Composers)));

            if (!(oldLibraryImages?.SequenceEqual(this.LibraryImages) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.LibraryImages)));

        }

        internal void InitilizeProperties()
        {
            foreach (var song in this.songs)
                song.InitilizeProperties();

            this.Genres = this.Songs.SelectMany(x => x.Genres).Distinct().OrderBy(x => x).ToArray();
            this.Interpreters = this.Songs.SelectMany(x => x.Interpreters).Distinct().OrderBy(x => x).ToArray();
            this.Composers = this.Songs.SelectMany(x => x.Composers).Distinct().OrderBy(x => x).ToArray();
            this.LibraryImages = this.Songs.SelectMany(x => x.LibraryImages).Distinct().OrderBy(x => x.providerId).ThenBy(x => x.imageId).ToArray();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Genres)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Interpreters)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Composers)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.LibraryImages)));
        }

        public IEnumerable<string> Genres { get; private set; }
        public IEnumerable<string> Interpreters { get; private set; }
        public IEnumerable<string> Composers { get; private set; }
        public IEnumerable<(string providerId, string imageId)> LibraryImages { get; private set; }

    }

    public class SongGroup : INotifyPropertyChanged
    {
        public ReadOnlyObservableCollection<Song> Songs { get; }
        public Album Album { get; }
        public string Title { get; }
        public int Track { get; }
        public int DiscNumber { get; }

        private readonly ObservableCollection<Song> songs;

        public event PropertyChangedEventHandler PropertyChanged;

        public Song PreferedSong => songs.FirstOrDefault();

        internal SongGroup(Album album, string title, int track, int discNumber)
        {
            this.songs = new ObservableCollection<Song>();
            this.Songs = new ReadOnlyObservableCollection<Song>(this.songs);
            this.Album = album;
            this.Title = title;
            this.Track = track;
            this.DiscNumber = discNumber;
        }

        internal void AddSong(Song song)
        {
            if (song.AlbumInterpret != this.Album.AlbumInterpret
                || song.AlbumName != this.Album.Title
                || song.Title != this.Title
                || song.Track != this.Track
                || song.DiscNumber != this.DiscNumber)
                throw new ArgumentException("Song must match the Properties of this SongGroup", nameof(song));


            var insertionIndex = this.songs.BinarySearch(song, s => s.LibraryProvider, (x, y) =>
            {
                var result = x.CompareTo(y);
                return result;
            });

            if (insertionIndex < 0)
                insertionIndex = ~insertionIndex;

            this.songs.Insert(insertionIndex, song);

            song.PropertyChanged += this.Song_PropertyChanged;

            this.UpdateProperties();
        }

        private void Song_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Song.Genres):
                case nameof(Song.Interpreters):
                case nameof(Song.Composers):
                case nameof(Song.LibraryImageId):
                    this.UpdateProperties();
                    break;
                case nameof(Song.AlbumInterpret):
                case nameof(Song.AlbumName):
                case nameof(Song.Title):
                case nameof(Song.Track):
                case nameof(Song.DiscNumber):
                    this.RemoveSong(sender as Song);
                    MusicStore.Instance.AddSong(sender as Song);
                    break;
                default:
                    break;
            }
        }

        internal void RemoveSong(Song song)
        {
            this.songs.Remove(song);
            song.PropertyChanged -= this.Song_PropertyChanged;
            this.UpdateProperties();
        }

        private void UpdateProperties()
        {
            if (!MusicStore.Instance.IsInitilized)
                return;

            var oldGenres = this.Genres;
            var oldInterpreters = this.Interpreters;
            var oldComposers = this.Composers;
            var oldLibraryImages = this.LibraryImages;

            this.Genres = this.Songs.SelectMany(x => x.Genres).Distinct().OrderBy(x => x).ToArray();
            this.Interpreters = this.Songs.SelectMany(x => x.Interpreters).Distinct().OrderBy(x => x).ToArray();
            this.Composers = this.Songs.SelectMany(x => x.Composers).Distinct().OrderBy(x => x).ToArray();
            this.LibraryImages = this.Songs.Select(x => (x.LibraryProvider, x.LibraryImageId)).Distinct().OrderBy(x => x.LibraryProvider).ThenBy(x => x.LibraryImageId).ToArray();

            if (!(oldGenres?.SequenceEqual(this.Genres) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Genres)));

            if (!(oldInterpreters?.SequenceEqual(this.Interpreters) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Interpreters)));

            if (!(oldComposers?.SequenceEqual(this.Composers) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Composers)));

            if (!(oldLibraryImages?.SequenceEqual(this.LibraryImages) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.LibraryImages)));

        }

        internal void InitilizeProperties()
        {
            this.Genres = this.Songs.SelectMany(x => x.Genres).Distinct().OrderBy(x => x).ToArray();
            this.Interpreters = this.Songs.SelectMany(x => x.Interpreters).Distinct().OrderBy(x => x).ToArray();
            this.Composers = this.Songs.SelectMany(x => x.Composers).Distinct().OrderBy(x => x).ToArray();
            this.LibraryImages = this.Songs.Select(x => (x.LibraryProvider, x.LibraryImageId)).Distinct().OrderBy(x => x.LibraryProvider).ThenBy(x => x.LibraryImageId).ToArray();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Genres)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Interpreters)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Composers)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.LibraryImages)));
        }

        public IEnumerable<string> Genres { get; private set; }
        public IEnumerable<string> Interpreters { get; private set; }
        public IEnumerable<string> Composers { get; private set; }
        public IEnumerable<(string providerId, string imageId)> LibraryImages { get; private set; }

    }

    public class PlayListNameEntry : IEquatable<PlayListNameEntry>
    {
        private PlayListNameEntry()
        {

        }
        public PlayListNameEntry(string name, Guid playListId)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.PlayListId = playListId;
        }

        public string Name { get; internal set; }
        public Guid PlayListId { get; private set; }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as PlayListNameEntry);
        }

        public bool Equals(PlayListNameEntry other)
        {
            return other != null &&
                   this.Name == other.Name &&
                   this.PlayListId.Equals(other.PlayListId);
        }

        public override int GetHashCode()
        {
            var hashCode = 300557564;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<Guid>.Default.GetHashCode(this.PlayListId);
            return hashCode;
        }

        public static bool operator ==(PlayListNameEntry left, PlayListNameEntry right)
        {
            return EqualityComparer<PlayListNameEntry>.Default.Equals(left, right);
        }

        public static bool operator !=(PlayListNameEntry left, PlayListNameEntry right)
        {
            return !(left == right);
        }
    }
    public class PlayListEntry : IEquatable<PlayListEntry>
    {
        private PlayListEntry()
        {

        }
        public PlayListEntry(string libraryProvider, string mediaId, Guid playListId)
        {
            this.LibraryProvider = libraryProvider ?? throw new ArgumentNullException(nameof(libraryProvider));
            this.MediaId = mediaId ?? throw new ArgumentNullException(nameof(mediaId));
            this.PlayListId = playListId;
        }

        public string LibraryProvider { get; private set; }
        public string MediaId { get; private set; }
        public Guid PlayListId { get; private set; }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as PlayListEntry);
        }

        public bool Equals(PlayListEntry other)
        {
            return other != null &&
                   this.LibraryProvider == other.LibraryProvider &&
                   this.MediaId == other.MediaId &&
                   this.PlayListId.Equals(other.PlayListId);
        }

        public override int GetHashCode()
        {
            var hashCode = 282779457;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.LibraryProvider);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.MediaId);
            hashCode = hashCode * -1521134295 + EqualityComparer<Guid>.Default.GetHashCode(this.PlayListId);
            return hashCode;
        }

        public static bool operator ==(PlayListEntry left, PlayListEntry right)
        {
            return EqualityComparer<PlayListEntry>.Default.Equals(left, right);
        }

        public static bool operator !=(PlayListEntry left, PlayListEntry right)
        {
            return !(left == right);
        }
    }

    public class Song : INotifyPropertyChanged, IEquatable<Song>
    {
        [NotMapped]
        private uint _year;
        [NotMapped]
        private ImmutableSortedSet<string> _genres = ImmutableSortedSet<string>.Empty;
        [NotMapped]
        private ImmutableSortedSet<string> _composers = ImmutableSortedSet<string>.Empty;
        [NotMapped]
        private ImmutableSortedSet<string> _interpreters = ImmutableSortedSet<string>.Empty;
        [NotMapped]
        private TimeSpan _duration;
        [NotMapped]
        private string _libraryImageId;
        [NotMapped]
        private string _albumName = string.Empty;
        [NotMapped]
        private string _albumInterpret = string.Empty;
        [NotMapped]
        private string _title = string.Empty;
        [NotMapped]
        private int _track;
        [NotMapped]
        private int _discNumber;


        internal string InterpreterSong { get; set; }
        internal string ComposerSong { get; set; }
        internal string GenreSong { get; set; }

        internal void TransferFromDatabase()
        {

            this._interpreters = this.Decode(this.InterpreterSong);
            this._composers = this.Decode(this.ComposerSong);
            this._genres = this.Decode(this.GenreSong);
        }

        private ImmutableSortedSet<string> Decode(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
                return ImmutableSortedSet<string>.Empty;
            return encoded.Split('<').Select(x => x.Replace("&lt;", "<").Replace("&amp;", "&")).ToImmutableSortedSet();
        }

        private string Encode(ImmutableSortedSet<string> set)
        {
            return string.Join("<", set.Select(x => x.Replace("&", "&amp;").Replace("<", "&lt;")));
        }

        private Song()
        {

        }

        internal Song(string libraryProvider, string mediaId)
        {
            this.LibraryProvider = libraryProvider ?? throw new ArgumentNullException(nameof(libraryProvider));
            this.MediaId = mediaId ?? throw new ArgumentNullException(nameof(mediaId));
        }

        public string LibraryProvider { get; private set; }
        public string MediaId { get; private set; }

        public string AlbumName
        {
            get => this._albumName; internal set
            {
                if (value is null)
                    throw new ArgumentNullException();
                if (this._albumName != value)
                {
                    this._albumName = value;
                    this.FireNotifyChanged();
                }
            }
        }
        public string AlbumInterpret
        {
            get => this._albumInterpret; internal set
            {
                if (value is null)
                    throw new ArgumentNullException();
                if (this._albumInterpret != value)
                {
                    this._albumInterpret = value;
                    this.FireNotifyChanged();
                }
            }
        }
        public string Title
        {
            get => this._title; internal set
            {
                if (value is null)
                    throw new ArgumentNullException();
                if (this._title != value)
                {
                    this._title = value;
                    this.FireNotifyChanged();
                }
            }
        }
        public int Track
        {
            get => this._track; internal set
            {
                if (this._track != value)
                {
                    this._track = value;
                    this.FireNotifyChanged();
                }
            }
        }
        public int DiscNumber
        {
            get => this._discNumber; internal set
            {
                if (this._discNumber != value)
                {
                    this._discNumber = value;
                    this.FireNotifyChanged();
                }
            }
        }

        public string LibraryImageId
        {
            get => this._libraryImageId;
            internal set
            {
                if (this._libraryImageId != value)
                {
                    this._libraryImageId = value;
                    this.FireNotifyChanged();
                }
            }
        }

        public TimeSpan Duration
        {
            get => this._duration; internal set
            {
                if (this._duration != value)
                {
                    this._duration = value;
                    this.FireNotifyChanged();
                }
            }
        }

        [NotMapped]
        public ImmutableSortedSet<string> Interpreters
        {
            get => this._interpreters; internal set
            {
                if (!(this?._interpreters.SequenceEqual(value) ?? false))
                {
                    this._interpreters = value;
                    this.InterpreterSong = this.Encode(value);
                    this.FireNotifyChanged();
                    this.FireNotifyChanged(nameof(this.InterpreterSong));
                }
            }
        }
        [NotMapped]
        public ImmutableSortedSet<string> Composers
        {
            get => this._composers; internal set
            {

                if (!(this._composers?.SequenceEqual(value) ?? false))
                {
                    this._composers = value;
                    this.ComposerSong = this.Encode(value);
                    this.FireNotifyChanged();
                    this.FireNotifyChanged(nameof(this.ComposerSong));
                }
            }
        }
        [NotMapped]
        public ImmutableSortedSet<string> Genres
        {
            get => this._genres; internal set
            {

                if (!(this._genres?.SequenceEqual(value) ?? false))
                {
                    this._genres = value;
                    this.GenreSong = this.Encode(value);
                    this.FireNotifyChanged();
                    this.FireNotifyChanged(nameof(this.GenreSong));
                }
            }
        }
        public uint Year
        {
            get => this._year;
            internal set
            {
                if (this._year != value)
                {
                    this._year = value;
                    this.FireNotifyChanged();
                }
            }
        }



        internal object[] PrimaryKeys => new object[] { this.LibraryProvider, this.MediaId };

        private void FireNotifyChanged([CallerMemberName] string propretyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propretyName));
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Song);
        }

        public bool Equals(Song other)
        {
            return other != null &&
                   this.LibraryProvider == other.LibraryProvider &&
                   this.MediaId == other.MediaId;
        }

        public override int GetHashCode()
        {
            var hashCode = -1793927485;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.LibraryProvider);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.MediaId);
            return hashCode;
        }




        public event PropertyChangedEventHandler PropertyChanged;

        public static bool operator ==(Song song1, Song song2)
        {
            return EqualityComparer<Song>.Default.Equals(song1, song2);
        }

        public static bool operator !=(Song song1, Song song2)
        {
            return !(song1 == song2);
        }
    }


    internal sealed class MusicStoreDatabase : DbContext
    {
        internal DbSet<Song> songs { get; set; }
        internal DbSet<PlayListEntry> playListEntrys { get; set; }
        internal DbSet<PlayListNameEntry> PlayListName { get; set; }

        public IQueryable<string> CoverIds(ILibrary library) => this.songs.Where(x => x.LibraryProvider == library.Id).Select(x => x.LibraryImageId);


        private MusicStoreDatabase()
        {

        }

        public static async Task<MusicStoreDatabase> CreateContextAsync(CancellationToken cancellationToken)
        {
            var store = await Task.Run(() => new MusicStoreDatabase(), cancellationToken); // Konstructor call takes forever :/
            await store.Check(cancellationToken);
            return store;
        }

        private bool first;
        private async Task Check(CancellationToken cancellationToken)
        {
            if (!this.first)
            {
                this.first = true;
                // await this.Database.EnsureDeletedAsync(cancellationToken);
            }

            await this.Database.EnsureCreatedAsync(cancellationToken);
        }

        public MusicStoreDatabase(DbContextOptions options) : base(options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlite("Data Source=music.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Song>()
                .HasKey(s => new { s.MediaId, s.LibraryProvider });

            modelBuilder.Entity<PlayListEntry>()
                            .HasKey(s => new { s.MediaId, s.LibraryProvider, s.PlayListId });

            modelBuilder.Entity<PlayListNameEntry>()
                            .HasKey(s => new { s.Name, s.PlayListId });

            modelBuilder.Entity<Song>().Property(x => x.GenreSong);
            modelBuilder.Entity<Song>().Property(x => x.InterpreterSong);
            modelBuilder.Entity<Song>().Property(x => x.ComposerSong);
        }




        //public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        //{
        //    var result = await base.SaveChangesAsync(cancellationToken);
        //    foreach (var a in this.notifyChanges)
        //        a();
        //    this.notifyChanges.Clear();
        //    return result;
        //}

        //public override void Dispose()
        //{
        //    base.Dispose();

        //    this.genreSemaphore.Dispose();
        //    this.artistSemaphore.Dispose();

        //}



    }
}
