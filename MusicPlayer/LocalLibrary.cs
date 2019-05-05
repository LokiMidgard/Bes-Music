using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MusicPlayer.Core;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace MusicPlayer
{
    public class LocalLibrary : ILibrary<MediaSource, StorageItemThumbnail>
    {
        public string Id => "LOCAL";

        public static readonly string[] SUPPORTED_EXTENSIONS = new[] { ".mp3" };
        private readonly SemaphoreSlim imageSemaphore = new SemaphoreSlim(2, 2);
        public async Task<StorageItemThumbnail> GetImage(string id, int size, CancellationToken cancellationToken)
        {

            await this.imageSemaphore.WaitAsync();
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;
                var file = await StorageFile.GetFileFromPathAsync(id);
                var thumbnail = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.MusicView, (uint)size, ThumbnailOptions.UseCurrentScale);
                if (thumbnail.Type == ThumbnailType.Image && !cancellationToken.IsCancellationRequested)
                    return thumbnail;
            }
            finally
            {
                this.imageSemaphore.Release();
            }

            return null;

        }

        public static LocalLibrary Instance { get; } = new LocalLibrary();

        private LocalLibrary()
        {
            LibraryRegistry.Register(this);
        }

        public async Task<MediaSource> GetMediaSource(string id, CancellationToken cancel)
        {
            var file = await StorageFile.GetFileFromPathAsync(id);
            var mediaSource = MediaSource.CreateFromStorageFile(file);
            //using (var context = await MusicStore.CreateContextAsync(cancel))
            //{
            //    var song = await context.Songs.ToAsyncEnumerable().Where(x => x.LibraryMediaId == id).First();
            //}

            return mediaSource;
        }

        public async Task Update(CancellationToken token)
        {
            using (var store = await MusicStore.CreateContextAsync(token))
            {

                var query = new FileQuery(KnownFolders.MusicLibrary);
                await query.ToAsyncEnumerable().ForEachAsync(async arg =>
                {
                    var (file, deffer) = arg;
                    try
                    {
                        if (!SUPPORTED_EXTENSIONS.Contains(Path.GetExtension(file.Name)))
                        {
                            deffer.Complete();
                            return;
                        }

                        var properties = await file.Properties.GetMusicPropertiesAsync();
                        var discNumberPropertie = await properties.RetrievePropertiesAsync(new[] { "System.Music.DiscNumber" }).AsTask().ContinueWith(x => x.Result.SingleOrDefault());
                        var artistString = properties.Artist ?? properties.AlbumArtist ?? "";
                        var discNumber = discNumberPropertie.Value as int?;
                        var componosts = (await Task.WhenAll(properties.Composers.Select(x => store.GetOrCreateArtist(x, token)))).ToList();
                        var title = properties.Title;
                        var artist = (await Task.WhenAll(artistString.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => store.GetOrCreateArtist(x.Trim(), token)))).ToList();
                        var genres = (await Task.WhenAll(properties.Genre.Select(x => store.GetOrCreateGenre(x, token)))).ToList();
                        var s = new Song()
                        {
                            LibraryProvider = this.Id,
                            AlbumName = properties.Album,
                            DiscNumber = discNumber ?? 0,
                            Duration = properties.Duration,
                            Name = title,
                            Track = (int)properties.TrackNumber,
                            Year = properties.Year,
                            LibraryMediaId = file.Path,

                        };
                        s.AddArtists(artist, ArtistType.Interpret);
                        s.AddArtists(componosts, ArtistType.Composer);
                        s.AddGenres(genres);

                        var album = await store.AddSong(s, this, deffer.CancelToken);
                        if (album != null)
                        {
                            // lets look if we have already set an image on another song
                            var imagePath = album.Songs.Where(x => x.LibraryImageId != null).Select(x => x.LibraryImageId).FirstOrDefault() ?? file.Path;
                            s.LibraryImageId = imagePath;
                            await store.SaveChangesAsync(token);
                         
                        }
                        deffer.Complete();
                    }
                    catch (Exception e)
                    {
                        deffer.Complete(e);
                    }
                }, token);
            }
        }
    }


}

