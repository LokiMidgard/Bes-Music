using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MusicPlayer.Core
{
    public sealed class MusicStore : DbContext
    {
        private DbSet<Song> songs { get; set; }
        private DbSet<Album> albums { get; set; }
        private DbSet<Artist> artists { get; set; }
        private DbSet<Genre> genre { get; set; }

        public IQueryable<Song> Songs => this.songs.AsNoTracking();
        public IQueryable<Album> Albums => this.albums.Include(c => c.Songs).AsNoTracking();
        public IQueryable<Artist> Artists => this.artists.AsNoTracking();
        public IQueryable<Genre> Genre => this.genre.AsNoTracking();

        public IQueryable<string> CoverIds(ILibrary library) => this.songs.Where(x => x.LibraryProvider == library.Id).Select(x => x.LibraryImageId);

        private readonly List<Action> notifyChanges = new List<Action>();

        public static event EventHandler<SongCollectionChangedEventArgs> SongCollectionChanged;
        public static event EventHandler<AlbumCollectionChangedEventArgs> AlbumCollectionChanged;

        private MusicStore()
        {

        }

        public static async Task<MusicStore> CreateContextAsync(CancellationToken cancellationToken)
        {
            var store = new MusicStore();
            await store.Check(cancellationToken);
            return store;
        }

        private bool first;
        private async Task Check(CancellationToken cancellationToken)
        {
            if (!this.first)
            {
                this.first = true;
                //await this.Database.EnsureDeletedAsync(cancellationToken);
            }
            await this.Database.EnsureCreatedAsync(cancellationToken);
        }

        public MusicStore(DbContextOptions options) : base(options)
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

            modelBuilder.Entity<Song>().HasKey(s => new { s.Name, s.AlbumName, s.Track, s.DiscNumber, s.LibraryProvider });
            modelBuilder.Entity<Album>()
                .HasMany(c => c.Songs)
                .WithOne().HasForeignKey(a => new { a.AlbumName, a.LibraryProvider });
            modelBuilder.Entity<Album>()
                .HasKey(a => new { a.Name, a.LibraryProvider });
            modelBuilder.Entity<Artist>().HasKey(a => a.Name);
            modelBuilder.Entity<Genre>().HasKey(g => g.Name);
        }

        /// <summary>
        /// Adds the Song to the correct album. If Album does not exists, it is created.
        /// </summary>
        /// <param name="song"></param>
        /// <param name="library"></param>
        /// <returns>The album the Song was added to or <c>null</c> if the song was already peresent</returns>
        public async Task<Album> AddSong<TMediaType, TImageType>(Song song, ILibrary<TMediaType, TImageType> library, CancellationToken cancellationToken)
        {
            var album = await this.albums.Include(c => c.Songs).FirstOrDefaultAsync(x => x.Name == song.AlbumName, cancellationToken);

            AlbumChanges albumAction;
            if (album == null)
            {
                album = new Album()
                {
                    Name = song.AlbumName,
                    Songs = new List<Song>(),
                    LibraryProvider = library.Id
                };
                await this.albums.AddAsync(album, cancellationToken);
                albumAction = AlbumChanges.Added;

            }
            else
                albumAction = AlbumChanges.SongsUpdated;

            if (album.Songs.Contains(song))
            {
                return null;
            }

            album.Songs.Add(song);
            await this.songs.AddAsync(song, cancellationToken);

            var albumEventArgs = new AlbumCollectionChangedEventArgs() { Album = album, Action = albumAction };
            var songEventArgs = new SongCollectionChangedEventArgs() { Song = song, Action = CollectionAction.Added };
            this.notifyChanges.Add(() => SongCollectionChanged?.Invoke(null /*we dont want to leak the context reference.*/, songEventArgs));
            this.notifyChanges.Add(() => AlbumCollectionChanged?.Invoke(null /*we dont want to leak the context reference.*/, albumEventArgs));

            return album;
        }

        private SemaphoreSlim artistSemaphore = new SemaphoreSlim(1, 1);
        private Dictionary<string, Artist> artistCache = new Dictionary<string, Artist>();
        public async Task<Artist> GetOrCreateArtist(string name, CancellationToken cancellationToken)
        {
            try
            {
                await this.artistSemaphore.WaitAsync(cancellationToken);
                if (this.artistCache.ContainsKey(name))
                    return this.artistCache[name];

                var artist = await this.artists.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
                if (artist == null)
                {
                    artist = new Artist() { Name = name };
                    await this.artists.AddAsync(artist, cancellationToken);
                    this.artistCache.Add(name, artist);
                }
                return artist;
            }
            finally
            {
                this.artistSemaphore.Release();
            }
        }

        private SemaphoreSlim genreSemaphore = new SemaphoreSlim(1, 1);
        private Dictionary<string, Genre> genreCache = new Dictionary<string, Genre>();
        public async Task<Genre> GetOrCreateGenre(string name, CancellationToken cancellationToken)
        {

            try
            {
                await this.genreSemaphore.WaitAsync(cancellationToken);
                if (this.genreCache.ContainsKey(name))
                    return this.genreCache[name];
                var artist = await this.genre.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
                if (artist == null)
                {
                    artist = new Genre() { Name = name };
                    await this.genre.AddAsync(artist, cancellationToken);
                    this.genreCache.Add(name, artist);
                }
                return artist;
            }
            finally
            {
                this.genreSemaphore.Release();
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            foreach (var a in this.notifyChanges)
                a();
            this.notifyChanges.Clear();
            return result;
        }

        public override void Dispose()
        {
            base.Dispose();

            this.genreSemaphore.Dispose();
            this.artistSemaphore.Dispose();

        }
    }


    [System.Diagnostics.DebuggerDisplay("Artist: {Name}")]
    public class Artist : IEquatable<Artist>
    {
        public string Name { get; set; }


        public override bool Equals(object obj)
        {
            return this.Equals(obj as Artist);
        }

        public bool Equals(Artist other)
        {
            return other != null &&
                   this.Name == other.Name;
        }

        public override int GetHashCode()
        {
            return 539060726 + EqualityComparer<string>.Default.GetHashCode(this.Name);
        }

        public static bool operator ==(Artist artist1, Artist artist2)
        {
            return EqualityComparer<Artist>.Default.Equals(artist1, artist2);
        }

        public static bool operator !=(Artist artist1, Artist artist2)
        {
            return !(artist1 == artist2);
        }
    }

    [System.Diagnostics.DebuggerDisplay("Genre: {Name}")]
    public class Genre : IEquatable<Genre>
    {
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Genre);
        }

        public bool Equals(Genre other)
        {
            return other != null &&
                   this.Name == other.Name;
        }

        public override int GetHashCode()
        {
            return 539060726 + EqualityComparer<string>.Default.GetHashCode(this.Name);
        }

        public static bool operator ==(Genre genre1, Genre genre2)
        {
            return EqualityComparer<Genre>.Default.Equals(genre1, genre2);
        }

        public static bool operator !=(Genre genre1, Genre genre2)
        {
            return !(genre1 == genre2);
        }
    }

    [System.Diagnostics.DebuggerDisplay("{Name} - {Track}/{DiscNumber}")]
    public class Song : IEquatable<Song>
    {
        public string AlbumName { get; set; }
        public string Name { get; set; }

        public string LibraryImageId { get; set; }
        public string LibraryMediaId { get; set; }
        public TimeSpan Duration { get; set; }
        public int Track { get; set; }
        public int DiscNumber { get; set; }
        public List<Artist> Artist { get; set; }
        public List<Artist> Composers { get; set; }
        public List<Genre> Genre { get; set; }
        public uint Year { get; set; }
        public string LibraryProvider { get; set; }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Song);
        }

        public bool Equals(Song other)
        {
            return other != null &&
                   this.AlbumName == other.AlbumName &&
                   this.Name == other.Name &&
                   this.Track == other.Track &&
                   EqualityComparer<int?>.Default.Equals(this.DiscNumber, other.DiscNumber) &&
                   this.LibraryProvider == other.LibraryProvider;
        }

        public override int GetHashCode()
        {
            var hashCode = 1563735115;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.AlbumName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Name);
            hashCode = hashCode * -1521134295 + this.Track.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<int?>.Default.GetHashCode(this.DiscNumber);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.LibraryProvider);
            return hashCode;
        }

        public static bool operator ==(Song song1, Song song2)
        {
            return EqualityComparer<Song>.Default.Equals(song1, song2);
        }

        public static bool operator !=(Song song1, Song song2)
        {
            return !(song1 == song2);
        }
    }

    [System.Diagnostics.DebuggerDisplay("Album: {Name}")]
    public class Album : IEquatable<Album>
    {
        public string Name { get; set; }

        public string LibraryProvider { get; set; }

        public List<Song> Songs { get; set; } = new List<Song>();

        [NotMapped]
        public IEnumerable<Artist> Artists => this.Songs.SelectMany(x => x.Artist).Distinct();
        [NotMapped]
        public IEnumerable<Artist> Composers => this.Songs.SelectMany(x => x.Composers).Distinct();
        [NotMapped]
        public IEnumerable<string> LibraryImages => this.Songs.Select(x => x.LibraryImageId).Distinct();


        public override bool Equals(object obj)
        {
            return this.Equals(obj as Album);
        }

        public bool Equals(Album other)
        {
            return other != null &&
                   this.Name == other.Name &&
                   this.LibraryProvider == other.LibraryProvider;
        }

        public override int GetHashCode()
        {
            var hashCode = 920117603;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.LibraryProvider);
            return hashCode;
        }

        public static bool operator ==(Album album1, Album album2)
        {
            return EqualityComparer<Album>.Default.Equals(album1, album2);
        }

        public static bool operator !=(Album album1, Album album2)
        {
            return !(album1 == album2);
        }
    }
}
