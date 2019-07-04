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
using Windows.Foundation;
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



        //public async Task<Playlist> CreatePlaylist(string name)
        //{
        //    if (!MicrosoftGraphService.Instance.IsAuthenticated)
        //        if (!await MicrosoftGraphService.Instance.TryLoginAsync())
        //            throw new NotAuthenticatedException();


        //    var driveItem = new DriveItem
        //    {
        //        Name = name,
        //        Folder = new Folder
        //        {
        //        },
        //    };
        //    driveItem.AdditionalData.Add("@microsoft.graph.conflictBehavior", "fail");

        //    DriveItem playlistFolder;
        //    try
        //    {
        //        playlistFolder = await MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Special.AppRoot.ItemWithPath("Playlists").Request().Select(x => x.Id).GetAsync();

        //    }
        //    catch (Exception e)
        //    {

        //        throw;
        //    }
        //    var result = await MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Items[playlistFolder.Id].Children
        //        .Request()
        //        .Select(x => x.Id)
        //        .AddAsync(driveItem);

        //    var playlist = new Playlist(result.Id, name);

        //    //MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Items[""].CreateUploadSession().Request(new Option[] { new HeaderOption("if-match", ) }).CreateUploadSession().Request().
        //}

        //public class Playlist
        //{
        //    public Playlist(string id, string name)
        //    {
        //        this.Id = id;
        //        this.Name = name;
        //    }

        //    public string Id { get; }
        //    public string Name { get; }

        //    public IEnumerable<Song> Songs { get; }
        //}

        public async Task SelectFolder()
        {
            throw new NotSupportedException("This actually is not Working right now :/");
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            
            var result = await picker.PickSingleFolderAsync();

            // first clear the old data. We don't want to leave unused stuff
            await this.ClearData();

            var cover = await result.CreateFolderAsync("cover", Windows.Storage.CreationCollisionOption.OpenIfExists);
            var media = await result.CreateFolderAsync("media", Windows.Storage.CreationCollisionOption.OpenIfExists);
            var playlist = await result.CreateFolderAsync("playlist", Windows.Storage.CreationCollisionOption.OpenIfExists);

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["cover"] = cover.Path;
            localSettings.Values["media"] = media.Path;
            localSettings.Values["playlist"] = playlist.Path;



        }


        public async Task ClearData()
        {

            var toDelete = MusicStore.Instance.Albums.SelectMany(x => x.Songs).SelectMany(x => x.Songs).Where(x => x.LibraryProvider == this.Id);

            await MusicStore.Instance.RemoveSong(toDelete);
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values.Remove("OneDriveDeltaToken");
            var coverFolder = await GetDataStoreFolder("cover");
            var mediaFolder = await  GetDataStoreFolder("media");
            await mediaFolder.DeleteAsync(Windows.Storage.StorageDeleteOption.PermanentDelete);
            await coverFolder.DeleteAsync(Windows.Storage.StorageDeleteOption.PermanentDelete);
            localSettings.DeleteContainer("cData");

        }

        private static IAsyncOperation<Windows.Storage.StorageFolder> GetDataStoreFolder(string name)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(name))
                return Windows.Storage.StorageFolder.GetFolderFromPathAsync(localSettings.Values[name].ToString());
            else
                return  Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync(name, Windows.Storage.CreationCollisionOption.OpenIfExists);
        }

        public async Task<Uri> GetImage(string id, int size, CancellationToken cancellationToken)
        {
            if (id == string.Empty)
                return null;
            try
            {
                var coverFolder = await  GetDataStoreFolder("cover");
                var storageItem = await coverFolder.GetFileAsync(id);
                return new Uri(storageItem.Path);
            }
            catch (FileNotFoundException)
            {

                var mediaFolder = await  GetDataStoreFolder("media");
                var storageItem = await mediaFolder.GetFileAsync(id);


                using (var stream = await storageItem.OpenReadAsync())
                {
                    var abstracStream = new TagLib.StreamFileAbstraction(storageItem.Name + ".mp3", stream.AsStream(), null);



                    using (var tagFile = TagLib.File.Create(abstracStream))
                    {

                        var tag = tagFile.Tag;

                        //composers = tag.Composers;
                        //performers = tag.Performers;
                        //genres = tag.Genres;
                        //year = tag.Year;
                        //joinedAlbumArtists = tag.JoinedAlbumArtists;
                        //album = tag.Album;
                        //title = tag.Title;
                        //track = (int)tag.Track;
                        //disc = (int)tag.Disc;

                        var picture = tag.Pictures.FirstOrDefault(x => x.Type == TagLib.PictureType.FrontCover) ?? tag.Pictures.FirstOrDefault();
                    }
                }







                System.Diagnostics.Debug.WriteLine(id);
                return null;
            }
        }

        public async Task<MediaSource> GetMediaSource(string id, CancellationToken cancellationToken)
        {
            var mediaFolder = await  GetDataStoreFolder("media");
            var file = await mediaFolder.GetFileAsync(id);
            var mediaSource = MediaSource.CreateFromStorageFile(file);
            return mediaSource;
        }
        private readonly SemaphoreSlim imageSemaphore = new SemaphoreSlim(10, 10);

        public async Task Update(CancellationToken token)
        {
            if (!MicrosoftGraphService.Instance.IsAuthenticated)
                if (!await MicrosoftGraphService.Instance.TryLoginAsync())
                    throw new NotAuthenticatedException();

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var lastToken = localSettings.Values["OneDriveDeltaToken"] as string;
            var container = localSettings.CreateContainer("cData", Windows.Storage.ApplicationDataCreateDisposition.Always);

            //MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Special.AppRoot.CreateUploadSession(new DriveItemUploadableProperties() ).Request()..PostAsync()



            IDriveItemDeltaRequest deltaRequest;
            if (lastToken != null)
            {
                deltaRequest = new DriveItemDeltaRequestBuilder(lastToken, MicrosoftGraphService.Instance.GraphProvider).Request();
            }
            else
            {
                //deltaRequest = MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Special.MusicFolder().Delta().Request().Select("name,audio,id,deleted,cTag,size");
                deltaRequest = new DriveItemDeltaRequestBuilder("https://graph.microsoft.com/v1.0/me/drive/items/634529B99B5BB82C!17006/delta", MicrosoftGraphService.Instance.GraphProvider).Request().Select("name,audio,id,deleted,cTag,size");
            }
            var audiosAddTask = new List<Task>();
            IDriveItemDeltaCollectionPage lastResponse = null;
            while (deltaRequest != null)
            {
                var response = await deltaRequest.GetAsync();
                //[driveItem.Id] = driveItem.CTag;

                var toAdd = response.Where(x => x.Audio != null && !(container.Values.ContainsKey(x.Id) && container.Values[x.Id]?.ToString() == x.CTag));
                audiosAddTask.Add(UpdateAudo(toAdd));

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
                var mediaFolder = await  GetDataStoreFolder("media");
                var coverFolder = await  GetDataStoreFolder("cover");

                await Task.WhenAll(toAdd.Select(driveItem => Task.Run(async () =>
                {

                    if (driveItem.Deleted != null)
                    {
                        try
                        {
                            var mediaFile = await mediaFolder.CreateFileAsync(driveItem.Id, Windows.Storage.CreationCollisionOption.OpenIfExists);
                            await mediaFile.DeleteAsync(Windows.Storage.StorageDeleteOption.PermanentDelete);

                        }
                        catch (Exception)
                        {

                        }
                        try
                        {
                            var coverFile = await coverFolder.CreateFileAsync(driveItem.Id, Windows.Storage.CreationCollisionOption.OpenIfExists);
                            await coverFile.DeleteAsync(Windows.Storage.StorageDeleteOption.PermanentDelete);
                        }
                        catch (Exception)
                        {


                        }
                        container.Values.Remove(driveItem.Id);

                        var song = MusicStore.Instance.GetSongByMediaId(this.Id, driveItem.Id);
                        if (song != null)
                            await MusicStore.Instance.RemoveSong(song);

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
                                TagLib.IPicture picture;
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

                                        picture = tag.Pictures.FirstOrDefault(x => x.Type == TagLib.PictureType.FrontCover) ?? tag.Pictures.FirstOrDefault();
                                    }
                                }
                                var musicProperties = await temporaryFile.Properties.GetMusicPropertiesAsync();
                                duration = musicProperties.Duration;

                                var mediaFile = await mediaFolder.CreateFileAsync(driveItem.Id, Windows.Storage.CreationCollisionOption.OpenIfExists);
                                await temporaryFile.MoveAndReplaceAsync(mediaFile);


                                return (composers, performers, genres, year, joinedAlbumArtists, album, title, track, disc, duration, picture);
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
                        //var downloadCover = Task.Run(async () =>
                        //       {
                        //           await this.imageSemaphore.WaitAsync();
                        //           try
                        //           {
                        //               const string thumbnailSize = "large";
                        //               //const string thumbnailSize = "c300x300_Crop";
                        //               var thumbnails = await MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Items[driveItem.Id].Thumbnails.Request().GetAsync();
                        //               if (thumbnails.Count == 0)
                        //                   return false;
                        //               var thumbnail = thumbnails[0].Large;
                        //               var respiones = await MicrosoftGraphService.Instance.GraphProvider.Me.Client.HttpProvider.SendAsync(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, thumbnail.Url));
                        //               //var thumbnail = await ["0"].Request().Select(thumbnailSize).GetAsync();

                        //               var temporaryFile = await coverFolder.CreateFileAsync(driveItem.Id, Windows.Storage.CreationCollisionOption.OpenIfExists);
                        //               using (var destinationStream = await temporaryFile.OpenTransactedWriteAsync())
                        //               using (var sourceStream = respiones.Content)
                        //               {
                        //                   await sourceStream.CopyToAsync(destinationStream.Stream.AsStream());
                        //                   await destinationStream.CommitAsync();
                        //               }
                        //               return true;
                        //           }
                        //           catch (Exception e)
                        //           {

                        //               throw;
                        //           }
                        //           finally
                        //           {
                        //               this.imageSemaphore.Release();

                        //           }
                        //       });
                        //await Task.WhenAll(
                        //    downloadMuediaFileTask,
                        //    downloadCover);

                        var mediaData = await downloadMuediaFileTask;

                        //var hasCover = await downloadCover;

                        //if (!hasCover)
                        //{
                        //}

                        if (mediaData.picture != null)
                        {
                            var temporaryFile = await coverFolder.CreateFileAsync(driveItem.Id, Windows.Storage.CreationCollisionOption.OpenIfExists);
                            using (var destinationStream = await temporaryFile.OpenTransactedWriteAsync())
                            using (var sourceStream = new MemoryStream(mediaData.picture.Data.Data))
                            {
                                await sourceStream.CopyToAsync(destinationStream.Stream.AsStream());
                                await destinationStream.CommitAsync();
                            }
                        }




                        var song = await MusicStore.Instance.AddSong(this.Id, driveItem.Id,
                                    duration: mediaData.duration,
                                    composers: mediaData.composers,
                                    interpreters: mediaData.performers,
                                    genres: mediaData.genres,
                                    year: mediaData.year,
                                    libraryImageId: mediaData.picture != null ? driveItem.Id : string.Empty,
                                    albumInterpret: mediaData.joinedAlbumArtists,
                                    albumName: mediaData.album,
                                    title: mediaData.title,
                                    track: mediaData.track,
                                    discNumber: mediaData.disc,
                                    cancelToken: token);

                        container.Values[driveItem.Id] = driveItem.CTag;


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
