using Microsoft.Graph;
using Microsoft.Toolkit.Services.MicrosoftGraph;
using MusicPlayer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MusicPlayer
{
    internal class OneDriveLibrary : ILibrary<MediaSource, Uri>
    {
        public string Id => "OneDrive";
        public static OneDriveLibrary Instance { get; } = new OneDriveLibrary();

        private OneDriveLibrary()
        {
            LibraryRegistry.Register(this);
        }

        public async Task<Uri> GetImage(string id, int size, CancellationToken cancellationToken)
        {
            if (id == string.Empty)
                return null;
            try
            {
                var coverFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("cover", Windows.Storage.CreationCollisionOption.OpenIfExists);
                var storageItem = await coverFolder.GetFileAsync(id);
                return new Uri(storageItem.Path);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public async Task<MediaSource> GetMediaSource(string id, CancellationToken cancellationToken)
        {
            var mediaFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("media", Windows.Storage.CreationCollisionOption.OpenIfExists);
            var file = await mediaFolder.GetFileAsync(id);
            var mediaSource = MediaSource.CreateFromStorageFile(file);
            return mediaSource;
        }
        private readonly SemaphoreSlim imageSemaphore = new SemaphoreSlim(1, 1);

        public async Task Update(CancellationToken token)
        {
            if (!MicrosoftGraphService.Instance.IsAuthenticated)
                if (!await MicrosoftGraphService.Instance.TryLoginAsync())
                    throw new NotAuthenticatedException();

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var lastToken = localSettings.Values["OneDriveDeltaToken"] as string;

            IDriveItemDeltaRequest deltaRequest;
            if (lastToken != null)
            {
                deltaRequest = new DriveItemDeltaRequestBuilder(lastToken, MicrosoftGraphService.Instance.GraphProvider.Me.Client).Request();
            }
            else
            {
                deltaRequest = MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Special.MusicFolder().Delta().Request().Select("name,audio,id,deleted,content");
            }
            var audiosAddTask = new List<Task>();
            IDriveItemDeltaCollectionPage lastResponse = null;
            while (deltaRequest != null)
            {
                var response = await deltaRequest.GetAsync();

                audiosAddTask.Add(UpdateAudo(response.Where(x => x.Audio != null)));

                if (response.Any())
                    deltaRequest = response.NextPageRequest;
                else
                    deltaRequest = null;

                lastResponse = response;
            }
            var nextUpdate = lastResponse.AdditionalData["@odata.deltaLink"];

            var result = deltaRequest;

            localSettings.Values["OneDriveDeltaToken"] = nextUpdate.ToString();

            await Task.WhenAll(audiosAddTask);

            async Task UpdateAudo(IEnumerable<DriveItem> toAdd)
            {
                var mediaFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("media", Windows.Storage.CreationCollisionOption.OpenIfExists);
                var coverFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("cover", Windows.Storage.CreationCollisionOption.OpenIfExists);

                await Task.WhenAll(toAdd.Select(driveItem => Task.Run(async () =>
                {

                    if (driveItem.Deleted != null)
                    {
                        //TODO Delete from database and storage
                        var mediaFile = await mediaFolder.CreateFileAsync(driveItem.Id, Windows.Storage.CreationCollisionOption.OpenIfExists);
                        await mediaFile.DeleteAsync(Windows.Storage.StorageDeleteOption.PermanentDelete);
                    }
                    else
                    {
                        var downloadMuediaFileTask = Task.Run(async () =>
                        {
                            await this.imageSemaphore.WaitAsync();
                            try
                            {
                                var extension = Path.GetExtension(driveItem.Name);

                                var temporaryFile = await mediaFolder.CreateFileAsync(Path.ChangeExtension(Guid.NewGuid().ToString(), extension));

                                var content = await MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Items[driveItem.Id].Content.Request().GetAsync();

                                using (var destinationStream = await temporaryFile.OpenTransactedWriteAsync())
                                using (var sourceStream = content)
                                {
                                    await sourceStream.CopyToAsync(destinationStream.Stream.AsStream());
                                    await destinationStream.CommitAsync();
                                }
                                string[] composers;
                                string[] performers;
                                string[] genres;
                                uint year;
                                string joinedAlbumArtists;
                                string album;
                                string title;
                                int track;
                                int disc;
                                TimeSpan duration;
                                using (var stream = await temporaryFile.OpenReadAsync())
                                {
                                    var abstracStream = new TagLib.StreamFileAbstraction(temporaryFile.Name, stream.AsStream(), null);



                                    using (var tagFile = TagLib.File.Create(abstracStream))
                                    {

                                        var tag = tagFile.Tag;

                                        composers = tag.Composers;
                                        performers = tag.Performers;
                                        genres = tag.Genres;
                                        year = tag.Year;
                                        joinedAlbumArtists = tag.JoinedAlbumArtists;
                                        album = tag.Album;
                                        title = tag.Title;
                                        track = (int)tag.Track;
                                        disc = (int)tag.Disc;

                                    }
                                }
                                var musicProperties = await temporaryFile.Properties.GetMusicPropertiesAsync();
                                duration = musicProperties.Duration;

                                var mediaFile = await mediaFolder.CreateFileAsync(driveItem.Id, Windows.Storage.CreationCollisionOption.OpenIfExists);
                                await temporaryFile.MoveAndReplaceAsync(mediaFile);

                                return (composers, performers, genres, year, joinedAlbumArtists, album, title, track, disc, duration);
                            }
                            catch (Exception e)
                            {

                                throw;
                            }
                            finally
                            {
                                this.imageSemaphore.Release();

                            }
                        });
                        var downloadCover = Task.Run(async () =>
                               {
                                   await this.imageSemaphore.WaitAsync();
                                   try
                                   {
                                       const string thumbnailSize = "large";
                                       //const string thumbnailSize = "c300x300_Crop";
                                       var thumbnails = await MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Items[driveItem.Id].Thumbnails.Request().GetAsync();
                                       if (thumbnails.Count == 0)
                                           return false;
                                       var thumbnail = thumbnails[0].Large;
                                       var respiones = await MicrosoftGraphService.Instance.GraphProvider.Me.Client.HttpProvider.SendAsync(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, thumbnail.Url));
                                       //var thumbnail = await ["0"].Request().Select(thumbnailSize).GetAsync();

                                       var temporaryFile = await coverFolder.CreateFileAsync(driveItem.Id, Windows.Storage.CreationCollisionOption.OpenIfExists);
                                       using (var destinationStream = await temporaryFile.OpenTransactedWriteAsync())
                                       using (var sourceStream = respiones.Content)
                                       {
                                           await sourceStream.CopyToAsync(destinationStream.Stream.AsStream());
                                           await destinationStream.CommitAsync();
                                       }
                                       return true;
                                   }
                                   catch (Exception e)
                                   {

                                       throw;
                                   }
                                   finally
                                   {
                                       this.imageSemaphore.Release();

                                   }
                               });
                        await Task.WhenAll(
                            downloadMuediaFileTask,
                            downloadCover);

                        var mediaData = await downloadMuediaFileTask;

                        var hasCover = await downloadCover;

                        var song = await MusicStore.Instance.AddSong(this.Id, driveItem.Id,
                                    duration: mediaData.duration,
                                    composers: mediaData.composers,
                                    interpreters: mediaData.performers,
                                    genres: mediaData.genres,
                                    year: mediaData.year,
                                    libraryImageId: hasCover ? driveItem.Id : string.Empty,
                                    albumInterpret: mediaData.joinedAlbumArtists,
                                    albumName: mediaData.album,
                                    title: mediaData.title,
                                    track: mediaData.track,
                                    discNumber: mediaData.disc,
                                    cancelToken: token);



                    }

                })));
            }



        }

    }
    public static class GraphExtension
    {
        public static IDriveItemRequestBuilder MusicFolder(this IDriveSpecialCollectionRequestBuilder requestBuilder)
        {
            return new DriveItemRequestBuilder(requestBuilder.AppendSegmentToRequestUrl("music"), requestBuilder.Client);
        }

    }

}
