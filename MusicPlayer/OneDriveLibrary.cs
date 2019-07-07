using Microsoft.Graph;
using Microsoft.Toolkit.Services.MicrosoftGraph;
using MusicPlayer.Controls;
using MusicPlayer.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Media.Core;
using Windows.Storage;
using Windows.UI.Xaml;

namespace MusicPlayer
{
    public enum StorageLocation
    {
        Unspecified,
        AppData,
        Music
    }


    public class OneDriveLibrary : DependencyObject, ILibrary<MediaSource, Uri>
    {
        private enum StorageType
        {
            Unspecified,
            Playlist,
            Cover,
            Media,
            Root
        }

        public string Id => "OneDrive";
        public static OneDriveLibrary instance;
        public static OneDriveLibrary Instance => instance ?? (instance = new OneDriveLibrary());



        private OneDriveLibrary()
        {

            this.PeekDataCommand = new DelegateCommand(async () => await this.Update(default), () => !this.IsLoading);
            this.DownloadDataCommand = new DelegateCommand(async () => await this.UpdateAudo(this.OneDriveWork, default), () => !this.IsLoading && this.OneDriveWork != null);


            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(LOCAL_SETTINGS_STORAGE_LOCATION))
                this.StorageLocation = (StorageLocation)localSettings.Values[LOCAL_SETTINGS_STORAGE_LOCATION];
            else
                this.StorageLocation = StorageLocation.AppData;


            LibraryRegistry.Register(this);
        }



        public OneDriveWork OneDriveWork
        {
            get => (OneDriveWork)this.GetValue(oneDriveWorkProperty);
            set => this.SetValue(oneDriveWorkProperty, value);
        }

        // Using a DependencyProperty as the backing store for oneDriveWork.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty oneDriveWorkProperty =
            DependencyProperty.Register("OneDriveWork", typeof(OneDriveWork), typeof(OneDriveLibrary), new PropertyMetadata(null, WorkChanged));

        private static void WorkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = (OneDriveLibrary)d;
            ((DelegateCommand)me.DownloadDataCommand).FireCanExecuteChanged();
        }

        public ICommand PeekDataCommand { get; }
        public ICommand DownloadDataCommand { get; }


        public StorageLocation StorageLocation
        {
            get => (StorageLocation)this.GetValue(StorageLocationProperty);
            set => this.SetValue(StorageLocationProperty, value);
        }

        private const string LOCAL_SETTINGS_STORAGE_LOCATION = "StorageLocation";
        private const string ONE_DRIVE_DELTA_TOKEN = "OneDriveDeltaToken";

        // Using a DependencyProperty as the backing store for StorageLocation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StorageLocationProperty =
            DependencyProperty.Register("StorageLocation", typeof(StorageLocation), typeof(OneDriveLibrary), new PropertyMetadata(StorageLocation.Unspecified, StorageLocationChanged));

        private static async void StorageLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = (OneDriveLibrary)d;
            using (me.StartLoad())
            {
                var oldLocation = (StorageLocation)e.OldValue;
                var newLocation = (StorageLocation)e.NewValue;
                if (newLocation == StorageLocation.Unspecified)
                    throw new NotSupportedException("This value is the initial value befor settings are loded. It can't be specified explicitly");
                if (oldLocation == StorageLocation.Unspecified)
                    return; // nothing todo here this only happen whil initilizing

                if (oldLocation != newLocation)
                {
                    var oldFolder = await me.GetDataStoreFolder(StorageType.Root, oldLocation);
                    var newFolder = await me.GetDataStoreFolder(StorageType.Root, newLocation);
                    await newFolder.DeleteAsync(StorageDeleteOption.Default);
                    await Task.Run(() => System.IO.Directory.Move(oldFolder.Path, newFolder.Path));
                    var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    localSettings.Values[LOCAL_SETTINGS_STORAGE_LOCATION] = (int)newLocation;

                }

                if (!Enum.IsDefined(typeof(StorageLocation), newLocation))
                    throw new NotSupportedException("This is not flaggable");
            }
        }



        public bool IsLoading
        {
            get => (bool)this.GetValue(IsLoadingProperty);
            set => this.SetValue(IsLoadingProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsLoading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(OneDriveLibrary), new PropertyMetadata(false, IsLoadingChanged));

        private static void IsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = (OneDriveLibrary)d;
            ((DelegateCommand)me.DownloadDataCommand).FireCanExecuteChanged();
            ((DelegateCommand)me.PeekDataCommand).FireCanExecuteChanged();
        }

        private int loading;




        public long BytesDownloaded
        {
            get => (long)this.GetValue(BytesDownloadedProperty);
            set => this.SetValue(BytesDownloadedProperty, value);
        }

        // Using a DependencyProperty as the backing store for BytesDownloaded.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BytesDownloadedProperty =
            DependencyProperty.Register("BytesDownloaded", typeof(long), typeof(OneDriveLibrary), new PropertyMetadata(0L));



        private IDisposable StartLoad()
        {
            if (this.Dispatcher.HasThreadAccess)
                Increment();
            else
                this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Increment);
            return new DelegateDisposable(() =>
            {
                if (this.Dispatcher.HasThreadAccess)
                    Decrement();
                else
                    this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Decrement);

            });

            void Increment()
            {
                var newValue = Interlocked.Increment(ref this.loading);
                if (newValue == 1)
                    this.IsLoading = true;
            }
            void Decrement()
            {
                var newValue = Interlocked.Decrement(ref this.loading);
                if (newValue == 0)
                    this.IsLoading = false;
            }
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


        public async Task SyncPlaylist()
        {

        }

        public async Task ClearData()
        {
            using (this.StartLoad())
            {
                var toDelete = MusicStore.Instance.Albums.SelectMany(x => x.Songs).SelectMany(x => x.Songs).Where(x => x.LibraryProvider == this.Id);

                await MusicStore.Instance.RemoveSong(toDelete);
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values.Remove(ONE_DRIVE_DELTA_TOKEN);
                var coverFolder = await this.GetDataStoreFolder(StorageType.Cover);
                var mediaFolder = await this.GetDataStoreFolder(StorageType.Media);
                await mediaFolder.DeleteAsync(Windows.Storage.StorageDeleteOption.PermanentDelete);
                await coverFolder.DeleteAsync(Windows.Storage.StorageDeleteOption.PermanentDelete);
                localSettings.DeleteContainer("cData");
            }

        }

        private Task<Windows.Storage.StorageFolder> GetDataStoreFolder(StorageType name)
        {
            return this.GetDataStoreFolder(name, this.StorageLocation);
        }

        private async Task<Windows.Storage.StorageFolder> GetDataStoreFolder(StorageType name, StorageLocation location)
        {
            StorageFolder root;
            switch (location)
            {
                case StorageLocation.AppData:
                    root = Windows.Storage.ApplicationData.Current.LocalFolder;
                    break;
                case StorageLocation.Music:
                    var library = await Windows.Storage.StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
                    root = library.SaveFolder;
                    break;
                default:
                case StorageLocation.Unspecified:
                    throw new NotSupportedException("Call to GetDataStoreFolder only Supported if initilized");
            }
            root = await root.CreateFolderAsync("MusicPlayerDataStore", CreationCollisionOption.OpenIfExists);
            switch (name)
            {
                case StorageType.Playlist:
                    return await root.CreateFolderAsync("Playlist", CreationCollisionOption.OpenIfExists);
                case StorageType.Cover:
                    return await root.CreateFolderAsync("Cover", CreationCollisionOption.OpenIfExists);
                case StorageType.Media:
                    return await root.CreateFolderAsync("Media", CreationCollisionOption.OpenIfExists);
                case StorageType.Root:
                    return root;
                default:
                case StorageType.Unspecified:
                    throw new NotSupportedException();
            }

        }

        public async Task<Uri> GetImage(string id, int size, CancellationToken cancellationToken)
        {
            if (id == string.Empty)
                return null;
            try
            {
                var coverFolder = await this.GetDataStoreFolder(StorageType.Cover);
                var storageItem = await coverFolder.GetFileAsync(id);
                return new Uri(storageItem.Path);
            }
            catch (FileNotFoundException)
            {

                System.Diagnostics.Debug.WriteLine(id);
                return null;
            }
        }

        public async Task<MediaSource> GetMediaSource(string id, CancellationToken cancellationToken)
        {
            var mediaFolder = await this.GetDataStoreFolder(StorageType.Media);
            var file = await mediaFolder.GetFileAsync(id);
            var mediaSource = MediaSource.CreateFromStorageFile(file);
            return mediaSource;
        }
        private readonly SemaphoreSlim imageSemaphore = new SemaphoreSlim(10, 10);

        public async Task Update(CancellationToken token)
        {
            using (this.StartLoad())
            {

                if (!MicrosoftGraphService.Instance.IsAuthenticated)
                    if (!await MicrosoftGraphService.Instance.TryLoginAsync())
                        throw new NotAuthenticatedException();

                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                var lastToken = localSettings.Values[ONE_DRIVE_DELTA_TOKEN] as string;
                var container = localSettings.CreateContainer("cData", Windows.Storage.ApplicationDataCreateDisposition.Always);

                //MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Special.AppRoot.CreateUploadSession(new DriveItemUploadableProperties() ).Request()..PostAsync()



                IDriveItemDeltaRequest deltaRequest;
                if (lastToken != null)
                {
                    deltaRequest = new DriveItemDeltaRequestBuilder(lastToken, MicrosoftGraphService.Instance.GraphProvider).Request();
                }
                else
                {
                    deltaRequest = MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Special.MusicFolder().Delta().Request().Select("name,audio,id,deleted,cTag,size");
                    //deltaRequest = new DriveItemDeltaRequestBuilder("https://graph.microsoft.com/v1.0/me/drive/items/634529B99B5BB82C!17006/delta", MicrosoftGraphService.Instance.GraphProvider).Request().Select("name,audio,id,deleted,cTag,size");
                }
                var downloadProposes = new List<DriveItem>();
                IDriveItemDeltaCollectionPage lastResponse = null;
                while (deltaRequest != null)
                {
                    var response = await deltaRequest.GetAsync();
                    //[driveItem.Id] = driveItem.CTag;

                    var toAdd = response.Where(x => x.Audio != null && !(container.Values.ContainsKey(x.Id) && container.Values[x.Id]?.ToString() == x.CTag));
                    downloadProposes.AddRange(toAdd);

                    if (response.Any())
                        deltaRequest = response.NextPageRequest;
                    else
                        deltaRequest = null;

                    lastResponse = response;
                }
                var nextUpdate = lastResponse.AdditionalData["@odata.deltaLink"]?.ToString();

                var result = deltaRequest;

                this.OneDriveWork = new OneDriveWork(downloadProposes, nextUpdate);
                return;


            }
        }

        private async Task UpdateAudo(OneDriveWork work, CancellationToken token)
        {
            using (this.StartLoad())
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    this.BytesDownloaded = 0;
                });

                if (!MicrosoftGraphService.Instance.IsAuthenticated)
                    if (!await MicrosoftGraphService.Instance.TryLoginAsync())
                        throw new NotAuthenticatedException();

                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                //var lastToken = localSettings.Values[ONE_DRIVE_DELTA_TOKEN] as string;
                var container = localSettings.CreateContainer("cData", Windows.Storage.ApplicationDataCreateDisposition.Always);

                var toAdd = work.DriveItems;
                var mediaFolder = await this.GetDataStoreFolder(StorageType.Media);
                var coverFolder = await this.GetDataStoreFolder(StorageType.Cover);

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
                        var mediaData = await downloadMuediaFileTask;

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

                        await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            this.BytesDownloaded += driveItem.Size ?? 0;
                        });

                    }

                })));

                localSettings.Values[ONE_DRIVE_DELTA_TOKEN] = work.NextRequest;

            }
        }
    }


    public class OneDriveWork
    {
        public ImmutableList<DriveItem> DriveItems { get; }
        public string NextRequest { get; }
        public long DownloadSizeInBytes { get; }

        public OneDriveWork(IEnumerable<DriveItem> driveItems, string nextRequest)
        {
            this.DriveItems = driveItems?.ToImmutableList() ?? throw new ArgumentNullException(nameof(driveItems));
            this.NextRequest = nextRequest ?? throw new ArgumentNullException(nameof(nextRequest));
            this.DownloadSizeInBytes = driveItems.Sum(x => x.Size ?? 0);
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
