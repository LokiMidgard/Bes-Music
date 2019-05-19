using Microsoft.EntityFrameworkCore;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
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
        public static MusicStore Instance { get; } = new MusicStore();

        public IEnumerable<string> Genres { get; private set; }
        public IEnumerable<string> Interpreters { get; private set; }
        public IEnumerable<string> Composers { get; private set; }
        public IEnumerable<(string providerId, string imageId)> LibraryImages { get; private set; }

        private void UpdateProperties()
        {
            var oldGenres = this.Genres;
            var oldInterpreters = this.Interpreters;
            var oldComposers = this.Composers;
            var oldLibraryImages = this.LibraryImages;

            this.Genres = this.Albums.SelectMany(x => x.Genres).Distinct().OrderBy(x => x).ToArray();
            this.Interpreters = this.Albums.SelectMany(x => x.Interpreters).Distinct().OrderBy(x => x).ToArray();
            this.Composers = this.Albums.SelectMany(x => x.Composers).Distinct().OrderBy(x => x).ToArray();
            this.LibraryImages = this.Albums.SelectMany(x => x.LibraryImages).Distinct().OrderBy(x => x.providerId).ThenBy(x => x.imageId).ToArray();

            if (!(oldGenres?.SequenceEqual(this.Genres) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Genres)));

            if (!(oldInterpreters?.SequenceEqual(this.Interpreters) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Interpreters)));

            if (!(oldComposers?.SequenceEqual(this.Composers) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Composers)));

            if (!(oldLibraryImages?.SequenceEqual(this.LibraryImages) ?? false))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.LibraryImages)));

        }


        private MusicStore()
        {
            this.albums = new ObservableCollection<Album>();
            this.Albums = new ReadOnlyObservableCollection<Album>(this.albums);
        }


        public void Initilize(Func<Func<Task>, Task> invokeOnUi)
        {
            if (this.invoke != null)
                return;
            this.invoke = invokeOnUi;
            this.initilisation.Set();
        }



        public event PropertyChangedEventHandler PropertyChanged;

        public ReadOnlyObservableCollection<Album> Albums { get; }
        private readonly ObservableCollection<Album> albums;
        private Func<Func<Task>, Task> invoke;

        public Task<Song> AddSong(string provider, string mediaId,
              string albumInterpret = default,
            string albumName = default,
            ICollection<string> composers = default,
            int discNumber = default,
            TimeSpan duration = default,
            ICollection<string> genres = default,
            ICollection<string> interpreters = default,
            string libraryImageId = default,
            string title = default,
            int track = default,
            uint year = default,
            CancellationToken cancelToken=default)
        {
            return this.RunOnUIThread(() =>
            {
                var song = new Song(provider, mediaId, this);
                if (albumInterpret != default)
                    song.AlbumInterpret = albumInterpret;
                if (albumName != default)
                    song.AlbumName = albumName;
                if (composers != default)
                    song.Composers = composers;
                if (discNumber != default)
                    song.DiscNumber = discNumber;
                if (duration != default)
                    song.Duration = duration;
                if (genres != default)
                    song.Genres = genres;
                if (interpreters != default)
                    song.Interpreters = interpreters;
                if (libraryImageId != default)
                    song.LibraryImageId = libraryImageId;
                if (title != default)
                    song.Title = title;
                if (track != default)
                    song.Track = track;
                if (year != default)
                    song.Year = year;
                this.AddSong(song);
                return song;
            });
        }

        public Task UpdateSong(Song song,
            string albumInterpret = default,
            string albumName = default,
            ICollection<string> composers = default,
            int discNumber = default,
            TimeSpan duration = default,
            ICollection<string> genres = default,
            ICollection<string> interpreters = default,
            string libraryImageId = default,
            string title = default,
            int track = default,
            uint year = default
            )
        {
            return this.RunOnUIThread(() =>
            {
                if (albumInterpret != default)
                    song.AlbumInterpret = albumInterpret;
                if (albumName != default)
                    song.AlbumName = albumName;
                if (composers != default)
                    song.Composers = composers;
                if (discNumber != default)
                    song.DiscNumber = discNumber;
                if (duration != default)
                    song.Duration = duration;
                if (genres != default)
                    song.Genres = genres;
                if (interpreters != default)
                    song.Interpreters = interpreters;
                if (libraryImageId != default)
                    song.LibraryImageId = libraryImageId;
                if (title != default)
                    song.Title = title;
                if (track != default)
                    song.Track = track;
                if (year != default)
                    song.Year = year;
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
                album = new Album(song.AlbumName, song.AlbumInterpret, this);
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

        internal Album(string title, string albumInterpret, MusicStore musicStore)
        {
            this.Title = title ?? throw new ArgumentNullException(nameof(title));
            this.AlbumInterpret = albumInterpret ?? throw new ArgumentNullException(nameof(albumInterpret));
            this.MusicStore = musicStore;
            this.songs = new ObservableCollection<SongGroup>();
            this.Songs = new ReadOnlyObservableCollection<SongGroup>(this.songs);
        }

        public string Title { get; }
        public string AlbumInterpret { get; }
        public MusicStore MusicStore { get; }
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
                songGroup = new SongGroup(this, song.Title, song.Track, song.DiscNumber, this.MusicStore);
                songGroup.PropertyChanged += this.SongGroup_PropertyChanged;
                (songGroup.Songs as INotifyCollectionChanged).CollectionChanged += this.Album_CollectionChanged;
                this.songs.Insert(insertionIndex, songGroup);
            }
            else
            {
                songGroup = this.songs[insertionIndex];
            }

            songGroup.AddSong(song);
        }

        private void Album_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var changedSongGroup = this.songs.First(x => x.Songs == sender);
            if (changedSongGroup.Songs.Count == 0)
            {
                this.songs.Remove(changedSongGroup);
                changedSongGroup.PropertyChanged -= this.SongGroup_PropertyChanged;
                (changedSongGroup.Songs as INotifyCollectionChanged).CollectionChanged -= this.Album_CollectionChanged;
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
        public MusicStore MusicStore { get; }

        private readonly ObservableCollection<Song> songs;

        public event PropertyChangedEventHandler PropertyChanged;

        internal SongGroup(Album album, string title, int track, int discNumber, MusicStore musicStore)
        {
            this.songs = new ObservableCollection<Song>();
            this.Songs = new ReadOnlyObservableCollection<Song>(this.songs);
            this.Album = album;
            this.Title = title;
            this.Track = track;
            this.DiscNumber = discNumber;
            this.MusicStore = musicStore;
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
                    this.MusicStore.AddSong(sender as Song);
                    break;
                default:
                    break;
            }
        }

        private void RemoveSong(Song song)
        {
            this.songs.Remove(song);
            song.PropertyChanged -= this.Song_PropertyChanged;
            this.UpdateProperties();
        }

        private void UpdateProperties()
        {
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

        public IEnumerable<string> Genres { get; private set; }
        public IEnumerable<string> Interpreters { get; private set; }
        public IEnumerable<string> Composers { get; private set; }
        public IEnumerable<(string providerId, string imageId)> LibraryImages { get; private set; }

    }

    public class Song : INotifyPropertyChanged
    {
        private uint _year;
        private ICollection<string> _genres = Array.Empty<string>();
        private ICollection<string> _composers = Array.Empty<string>();
        private ICollection<string> _interpreters = Array.Empty<string>();
        private TimeSpan _duration;
        private string _libraryImageId;
        private string _albumName = string.Empty;
        private string _albumInterpret = string.Empty;
        private string _title= string.Empty;
        private int _track;
        private int _discNumber;

        internal Song(string libraryProvider, string mediaId, MusicStore musicStore)
        {
            this.LibraryProvider = libraryProvider ?? throw new ArgumentNullException(nameof(libraryProvider));
            this.MediaId = mediaId ?? throw new ArgumentNullException(nameof(mediaId));
            this.MusicStore = musicStore;
        }

        public string LibraryProvider { get; }
        public string MediaId { get; }
        public MusicStore MusicStore { get; }

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

        public ICollection<string> Interpreters
        {
            get => this._interpreters; internal set
            {
                if (!(this?._interpreters.SequenceEqual(value) ?? false))
                {
                    this._interpreters = value;
                    this.FireNotifyChanged();
                }
            }
        }
        public ICollection<string> Composers
        {
            get => this._composers; internal set
            {
                if (!(this._composers?.SequenceEqual(value) ?? false))
                {
                    this._composers = value;
                    this.FireNotifyChanged();
                }
            }
        }
        public ICollection<string> Genres
        {
            get => this._genres; internal set
            {
                if (!(this._genres?.SequenceEqual(value) ?? false))
                {
                    this._genres = value;
                    this.FireNotifyChanged();
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

        public event PropertyChangedEventHandler PropertyChanged;


    }


    internal sealed class MusicStoreDatabase : DbContext
    {
        internal DbSet<MusicStoreDatabase.Song> songs { get; set; }
        internal DbSet<MusicStoreDatabase.ArtistSong> artists { get; set; }
        internal DbSet<MusicStoreDatabase.GenreSong> genres { get; set; }

        public IQueryable<string> CoverIds(ILibrary library) => this.songs.Where(x => x.LibraryProvider == library.Id).Select(x => x.LibraryImageId);


        public static event EventHandler<SongCollectionChangedEventArgs> SongCollectionChanged;
        public static event EventHandler<AlbumCollectionChangedEventArgs> AlbumCollectionChanged;

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
                await this.Database.EnsureDeletedAsync(cancellationToken);
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
                .HasKey(s => new { s.AlbumName, s.AlbumInterpret, s.Name, s.Track, s.DiscNumber, s.LibraryProvider });


            modelBuilder.Entity<GenreSong>()
                .HasKey(x => new { x.SongTitle, x.AlbumInterpret, x.AlbumName, x.Track, x.DiscNumber, x.LibraryProvider, x.GenreName });


            modelBuilder.Entity<ArtistSong>()
                .HasKey(x => new { x.SongTitle, x.AlbumInterpret, x.AlbumName, x.Track, x.DiscNumber, x.LibraryProvider, x.ArtistName, x.ArtistType });


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


        public class GenreSong : IEquatable<GenreSong>
        {

            public string AlbumName { get; set; }
            public string AlbumInterpret { get; set; }
            public string SongTitle { get; set; }
            public int Track { get; set; }
            public int DiscNumber { get; set; }
            public string LibraryProvider { get; set; }


            public string GenreName { get; set; }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as GenreSong);
            }

            public bool Equals(GenreSong other)
            {
                return other != null &&
                       this.AlbumName == other.AlbumName &&
                       this.AlbumInterpret == other.AlbumInterpret &&
                       this.SongTitle == other.SongTitle &&
                       this.Track == other.Track &&
                       this.DiscNumber == other.DiscNumber &&
                       this.LibraryProvider == other.LibraryProvider &&
                       this.GenreName == other.GenreName;
            }

            public override int GetHashCode()
            {
                var hashCode = 603161187;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.AlbumName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.AlbumInterpret);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.SongTitle);
                hashCode = hashCode * -1521134295 + this.Track.GetHashCode();
                hashCode = hashCode * -1521134295 + this.DiscNumber.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.LibraryProvider);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.GenreName);
                return hashCode;
            }

            public static bool operator ==(GenreSong left, GenreSong right)
            {
                return EqualityComparer<GenreSong>.Default.Equals(left, right);
            }

            public static bool operator !=(GenreSong left, GenreSong right)
            {
                return !(left == right);
            }
        }

        public enum ArtistType
        {
            Interpret,
            Composer
        }

        public class ArtistSong : IEquatable<ArtistSong>
        {
            public ArtistType ArtistType { get; set; }

            public string AlbumName { get; set; }
            public string AlbumInterpret { get; set; }
            public string SongTitle { get; set; }
            public int Track { get; set; }
            public int DiscNumber { get; set; }
            public string LibraryProvider { get; set; }


            public string ArtistName { get; set; }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as ArtistSong);
            }

            public bool Equals(ArtistSong other)
            {
                return other != null &&
                       this.ArtistType == other.ArtistType &&
                       this.AlbumName == other.AlbumName &&
                       this.AlbumInterpret == other.AlbumInterpret &&
                       this.SongTitle == other.SongTitle &&
                       this.Track == other.Track &&
                       this.DiscNumber == other.DiscNumber &&
                       this.LibraryProvider == other.LibraryProvider &&
                       this.ArtistName == other.ArtistName;
            }

            public override int GetHashCode()
            {
                var hashCode = -45014965;
                hashCode = hashCode * -1521134295 + this.ArtistType.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.AlbumName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.AlbumInterpret);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.SongTitle);
                hashCode = hashCode * -1521134295 + this.Track.GetHashCode();
                hashCode = hashCode * -1521134295 + this.DiscNumber.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.LibraryProvider);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.ArtistName);
                return hashCode;
            }

            public static bool operator ==(ArtistSong left, ArtistSong right)
            {
                return EqualityComparer<ArtistSong>.Default.Equals(left, right);
            }

            public static bool operator !=(ArtistSong left, ArtistSong right)
            {
                return !(left == right);
            }
        }


        [System.Diagnostics.DebuggerDisplay("{Name} - {Track}/{DiscNumber}")]
        public class Song : IEquatable<Song>
        {
            public string AlbumName { get; set; }
            public string AlbumInterpret { get; set; }
            public string Name { get; set; }
            public int Track { get; set; }
            public int DiscNumber { get; set; }
            public string LibraryProvider { get; set; }

            public string LibraryImageId { get; set; }
            public string LibraryMediaId { get; set; }
            public TimeSpan Duration { get; set; }

            public HashSet<ArtistSong> ArtistSong { get; set; } = new HashSet<ArtistSong>();
            public HashSet<GenreSong> GenreSong { get; set; } = new HashSet<GenreSong>();


            public ArtistSong GenerateArtist(string artist, ArtistType type)
            {
                return new ArtistSong()
                {
                    AlbumName = this.AlbumName,
                    AlbumInterpret = this.AlbumInterpret,
                    ArtistName = artist,
                    Track = this.Track,
                    DiscNumber = this.DiscNumber,
                    LibraryProvider = this.LibraryProvider,
                    SongTitle = this.Name,
                    ArtistType = type
                };
            }
            public GenreSong GenerateGenre(string genre)
            {
                return new GenreSong()
                {
                    AlbumName = this.AlbumName,
                    AlbumInterpret = this.AlbumInterpret,
                    GenreName = genre,
                    Track = this.Track,
                    DiscNumber = this.DiscNumber,
                    LibraryProvider = this.LibraryProvider,
                    SongTitle = this.Name
                };
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as Song);
            }

            public bool Equals(Song other)
            {
                return other != null &&
                       this.AlbumName == other.AlbumName &&
                       this.AlbumInterpret == other.AlbumInterpret &&
                       this.Name == other.Name &&
                       this.Track == other.Track &&
                       this.DiscNumber == other.DiscNumber &&
                       this.LibraryProvider == other.LibraryProvider;
            }

            public override int GetHashCode()
            {
                var hashCode = -424849820;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.AlbumName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.AlbumInterpret);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Name);
                hashCode = hashCode * -1521134295 + this.Track.GetHashCode();
                hashCode = hashCode * -1521134295 + this.DiscNumber.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.LibraryProvider);
                return hashCode;
            }

            public uint Year { get; set; }

            public static bool operator ==(Song left, Song right)
            {
                return EqualityComparer<Song>.Default.Equals(left, right);
            }

            public static bool operator !=(Song left, Song right)
            {
                return !(left == right);
            }
        }

    }
}
