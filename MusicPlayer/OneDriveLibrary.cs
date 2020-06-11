using Microsoft.Graph;
using Microsoft.Toolkit.Services.MicrosoftGraph;
using MusicPlayer.Controls;
using MusicPlayer.Core;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Media.Core;
using Windows.Networking.Connectivity;
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


    public partial class OneDriveLibrary : DependencyObject, ILibrary<MediaSource, Uri>
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

        public event Func<string, Task<bool>> OnAskForPermission;

        private OneDriveLibrary()
        {

            this.PeekDataCommand = new DelegateCommand(async () =>
            {
                var _displayRequest = new Windows.System.Display.DisplayRequest();
                try
                {
                    _displayRequest.RequestActive();
                    await this.Update(this.GetCancel());
                }
                //catch (TaskCanceledException) { }
                finally
                {
                    _displayRequest.RequestRelease();
                }
            }, () => !this.IsLoading);
            this.DownloadDataCommand = new DelegateCommand(async () =>
            {
                var _displayRequest = new Windows.System.Display.DisplayRequest();
                try
                {

                    _displayRequest.RequestActive();


                    var connectionCost = NetworkInformation.GetInternetConnectionProfile().GetConnectionCost();
                    if (connectionCost.NetworkCostType != NetworkCostType.Unrestricted)
                    {
                        //Metered Network
                        var converter = new Converters.FileSizeConverter();
                        var answer = this.OnAskForPermission?.Invoke($"Your on an metered connection. This operationcan will download {converter.Convert(this.OneDriveWork.DownloadSizeInBytes, null, null, null)}.");
                        if (answer != null && !(await answer))
                            return;
                    }


                    await this.UpdateAudo(this.OneDriveWork, this.GetCancel());
                }
                //catch (TaskCanceledException) { }
                finally
                {
                    _displayRequest.RequestRelease();
                }
            }, () => !this.IsLoading && this.OneDriveWork != null);
            this.SyncPlaylistCommand = new DelegateCommand(async () =>
            {
                var _displayRequest = new Windows.System.Display.DisplayRequest();
                try
                {
                    _displayRequest.RequestActive();
                    await this.SyncPlaylist(this.GetCancel());
                }
                //catch (TaskCanceledException) { }
                finally
                {
                    _displayRequest.RequestRelease();
                }
            }, () => !this.IsLoading);

            this.ClearDataCommand = new DelegateCommand(async () =>
            {
                var _displayRequest = new Windows.System.Display.DisplayRequest();
                try
                {
                    _displayRequest.RequestActive();
                    var answer = this.OnAskForPermission?.Invoke($"Do you realy want to delete all Data. You need to download it again to hear your music.");
                    if (answer != null && !(await answer))
                        return;
                    await this.ClearData();
                }
                //catch (TaskCanceledException) { }
                finally
                {
                    _displayRequest.RequestRelease();
                }
            }, () => !this.IsLoading);


            this.CancelCommand = new DelegateCommand(() => this.Cancel(), () => !this.GetCancel().IsCancellationRequested);

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
        public ICommand SyncPlaylistCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearDataCommand { get; }


        private CancellationTokenSource internalCancelSource;

        private void Cancel()
        {
            this.internalCancelSource?.Cancel();
            this.internalCancelSource?.Dispose();
        }
        private CancellationToken GetCancel()
        {
            if (this.internalCancelSource is null || this.internalCancelSource.IsCancellationRequested)
            {

                var cancellationTokenSource = new CancellationTokenSource();
                this.internalCancelSource = cancellationTokenSource;
            }
            return this.internalCancelSource.Token;
        }


        public StorageLocation StorageLocation
        {
            get => (StorageLocation)this.GetValue(StorageLocationProperty);
            set => this.SetValue(StorageLocationProperty, value);
        }

        private const string LOCAL_SETTINGS_STORAGE_LOCATION = "StorageLocation";
        private const string ONE_DRIVE_MUSIC_DELTA_TOKEN = "OneDriveDeltaToken";
        private const string ONE_DRIVE_PLAYLIST_DELTA_TOKEN = "OneDrivePlayListDeltaToken";
        private const string LOCAL_SETTINGS_LASTPLAYLIST_STATE = "LOCAL_SETTINGS_LASTPLAYLISTSTATE";

        // Using a DependencyProperty as the backing store for StorageLocation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StorageLocationProperty =
            DependencyProperty.Register("StorageLocation", typeof(StorageLocation), typeof(OneDriveLibrary), new PropertyMetadata(StorageLocation.Unspecified, StorageLocationChanged));
        private bool storageLocationChangeIgnoreAskUser;
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
                    if (!me.storageLocationChangeIgnoreAskUser)
                    {

                        var answer = me.OnAskForPermission?.Invoke($"Do you realy want to move all Data?.");
                        if (answer != null && !(await answer))
                        {
                            me.storageLocationChangeIgnoreAskUser = true;
                            try
                            {
                                me.StorageLocation = oldLocation;
                                return;
                            }
                            finally
                            {
                                me.storageLocationChangeIgnoreAskUser = false;
                            }
                        }

                    }

                    var oldFolder = await me.GetDataStoreFolder(StorageType.Root, oldLocation);
                    var newFolder = await me.GetDataStoreFolder(StorageType.Root, newLocation);
                    if (Path.GetPathRoot(oldFolder.Path) == Path.GetPathRoot(newFolder.Path))
                    {
                        await newFolder.DeleteAsync(StorageDeleteOption.Default);
                        await Task.Run(() => System.IO.Directory.Move(oldFolder.Path, newFolder.Path));
                    }
                    else
                    {
                        // we are on different roots. Move will throw exception tehn :/
                        await Task.Run(() => DirectoryCopy(oldFolder.Path, newFolder.Path));

                        void DirectoryCopy(string sourceBasePath, string destinationBasePath)
                        {
                            if (!System.IO.Directory.Exists(sourceBasePath))
                                throw new DirectoryNotFoundException($"Directory '{sourceBasePath}' not found");

                            var directoriesToProcess = new Queue<(string sourcePath, string destinationPath)>();
                            directoriesToProcess.Enqueue((sourcePath: sourceBasePath, destinationPath: destinationBasePath));
                            while (directoriesToProcess.Any())
                            {
                                (string sourcePath, string destinationPath) = directoriesToProcess.Dequeue();

                                if (!System.IO.Directory.Exists(destinationPath))
                                    System.IO.Directory.CreateDirectory(destinationPath);

                                var sourceDirectoryInfo = new DirectoryInfo(sourcePath);
                                foreach (var sourceFileInfo in sourceDirectoryInfo.EnumerateFiles())
                                    sourceFileInfo.CopyTo(Path.Combine(destinationPath, sourceFileInfo.Name), true);

                                foreach (var sourceSubDirectoryInfo in sourceDirectoryInfo.EnumerateDirectories())
                                    directoriesToProcess.Enqueue((
                                        sourcePath: sourceSubDirectoryInfo.FullName,
                                        destinationPath: Path.Combine(destinationPath, sourceSubDirectoryInfo.Name)));
                            }
                        }
                    }
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
            ((DelegateCommand)me.SyncPlaylistCommand).FireCanExecuteChanged();
            ((DelegateCommand)me.ClearDataCommand).FireCanExecuteChanged();
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
                _=this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Increment);
            return new DelegateDisposable(() =>
            {
                if (this.Dispatcher.HasThreadAccess)
                    Decrement();
                else
                  _=  this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Decrement);

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


        private async Task SyncPlaylist(CancellationToken cancellationToken)
        {

            using (this.StartLoad())
            {

                if (!MicrosoftGraphService.Instance.IsAuthenticated)
                    if (!await MicrosoftGraphService.Instance.TryLoginAsync())
                        throw new NotAuthenticatedException();

                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;


                //try
                //{
                //    await MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Special.AppRoot.Children["playlists"].Request().DeleteAsync();
                //    return;
                //}
                //catch (Exception xx)
                //{
                //}

                var playlistFolder = await this.EnsureFolderExists(MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Special.AppRoot, "playlists", cancellationToken);

                var downloadProposes = new List<DriveItem>();

                do
                {
                    downloadProposes.Clear();
                    var lastToken = localSettings.Values[ONE_DRIVE_PLAYLIST_DELTA_TOKEN] as string;

                    IDriveItemDeltaRequest deltaRequest;
                    if (lastToken != null)
                    {
                        deltaRequest = new DriveItemDeltaRequestBuilder(lastToken, MicrosoftGraphService.Instance.GraphProvider).Request();
                    }
                    else
                    {
                        deltaRequest = MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Items[playlistFolder.Id].Delta().Request().Select("name,deleted,audio");
                        //deltaRequest = new DriveItemDeltaRequestBuilder("https://graph.microsoft.com/v1.0/me/drive/items/634529B99B5BB82C!17006/delta", MicrosoftGraphService.Instance.GraphProvider).Request().Select("name,audio,id,deleted,cTag,size");
                    }
                    IDriveItemDeltaCollectionPage lastResponse = null;
                    while (deltaRequest != null)
                    {
                        var response = await deltaRequest.GetAsync(cancellationToken);
                        //[driveItem.Id] = driveItem.CTag;

                        var toAdd = response;
                        downloadProposes.AddRange(toAdd.Where(x => x.Name != "playlists"));

                        if (response.Any())
                            deltaRequest = response.NextPageRequest;
                        else
                            deltaRequest = null;

                        lastResponse = response;
                    }
                    var nextUpdate = lastResponse.AdditionalData["@odata.deltaLink"]?.ToString();


                    var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                    var playlistFile = await localFolder.CreateFileAsync(LOCAL_SETTINGS_LASTPLAYLIST_STATE, CreationCollisionOption.OpenIfExists);
                    var basicProperties = await playlistFile.GetBasicPropertiesAsync();
                    string lastPlayList;
                    if (basicProperties.Size != 0)
                    {
                        lastPlayList = await FileIO.ReadTextAsync(playlistFile);
                        // already Existed
                    }
                    else
                        lastPlayList = null;

                    var lastState = PlayListCollectionState.LoadFromString(lastPlayList);
                    var currentState = await GetCurrentPlayListState();
                    var localChanges = lastState.GetChanges(currentState);


                    cancellationToken.ThrowIfCancellationRequested();
                    if (downloadProposes.Any())
                    {
                        await PerformRemoteChanges(downloadProposes);
                        var newCurrentState = await GetCurrentPlayListState();
                        await FileIO.WriteTextAsync(playlistFile, newCurrentState.Persist());
                    }
                    localSettings.Values[ONE_DRIVE_PLAYLIST_DELTA_TOKEN] = nextUpdate;
                    cancellationToken.ThrowIfCancellationRequested();


                    int LocalChangeOrder(Change c)
                    {
                        switch (c)
                        {
                            case PlayListCreatedChange playListCreated:
                                return 0;
                            case SongCreatedChange songCreated:
                                return 1;
                            case SongDeletedChange songDeleted:
                                return 2;
                            case PlayListDeletedChange playListDeleted:
                                return 3;
                            case PlayListNameChange playListNameChange:
                                return 4;
                            case SongIndexChange songIndex:
                                return 5;
                            default:
                                return int.MaxValue;
                        }
                    }

                    foreach (var item in localChanges.OrderBy(LocalChangeOrder))
                    {
                        switch (item)
                        {
                            case PlayListCreatedChange playListCreated:
                                {
                                    await UploadPlaylist(playlistFolder, playListCreated.PlaylistId, playListCreated.Name, cancellationToken);
                                    foreach (var (song, index) in playListCreated.Songs.Select((value, index) => (value, index)))
                                        await UploadPlaylistEntry(playlistFolder, playListCreated.PlaylistId, song, index, cancellationToken);
                                }
                                break;

                            case PlayListNameChange playListNameChange:
                                {
                                    await UploadPlaylist(playlistFolder, playListNameChange.PlaylistId, playListNameChange.NewName, cancellationToken);
                                }
                                break;

                            case PlayListDeletedChange playListDeleted:
                                {
                                    await DeletePlaylist(playlistFolder, playListDeleted.PlaylistId, cancellationToken);
                                }
                                break;
                            case SongCreatedChange songCreated:
                                {
                                    await UploadPlaylistEntry(playlistFolder, songCreated.PlaylistId, new SongState(libraryProvider: songCreated.LibraryProvider, mediaId: songCreated.MediaId), songCreated.Index, cancellationToken);
                                }
                                break;

                            case SongDeletedChange songDeleted:
                                {
                                    await DeletePlaylistEntry(playlistFolder, songDeleted.PlaylistId, new SongState(libraryProvider: songDeleted.LibraryProvider, mediaId: songDeleted.MediaId), cancellationToken);

                                }
                                break;

                            case SongIndexChange songIndex:
                                {
                                    await UploadPlaylistEntry(playlistFolder, songIndex.PlaylistId, new SongState(libraryProvider: songIndex.LibraryProvider, mediaId: songIndex.MediaId), songIndex.Index, cancellationToken);

                                }
                                break;



                            default:
                                break;
                        }
                    }



                } while (downloadProposes.Any());


                int EntryType(DriveItem arg)
                {
                    switch (arg.Audio.Title)
                    {
                        case "song":
                            return 2;
                        case "playlist":
                            return 1;
                        default:
                            return 0;
                    }
                }

                async Task PerformRemoteChanges(IEnumerable<DriveItem> changes)
                {
                    foreach (var item in changes.OrderBy(EntryType))
                    {
                        switch (item.Audio.Title)
                        {
                            case "song":
                                try
                                {
                                    var array = Path.GetFileNameWithoutExtension(item.Name).Split('~');

                                    if (array.Length != 3)
                                        throw new FormatException("Filename not as expeted");

                                    var playListId = Guid.Parse(array[0]);
                                    var providerId = array[1];
                                    var mediaId = array[2];


                                    var playlist = MusicStore.Instance.PlayLists.FirstOrDefault(x => x.Id == playListId);

                                    if (playlist is null)
                                    {
                                        System.Diagnostics.Debug.Assert(item.Deleted is null, "When the playlist does not exist, we may not get this item.");
                                        continue;
                                    }



                                    if (item.Deleted != null)
                                    {

                                        var songToDelete = playlist.Songs.FirstOrDefault(x => x.MediaId == mediaId && x.LibraryProvider == providerId);
                                        if (songToDelete != null)
                                            await MusicStore.Instance.RemovePlaylistSong(playlist, songToDelete);
                                    }
                                    else
                                    {
                                        var toChange = playlist.Songs.FirstOrDefault(x => x.MediaId == mediaId && x.LibraryProvider == providerId);


                                        if (toChange is null) // we need to AddIt
                                        {
                                            var song = MusicStore.Instance.GetSongByMediaId(providerId, mediaId);
                                            if (song is null) // not jet synced?
                                                continue; // TODO: well we need to remember this....
                                            await MusicStore.Instance.AddPlaylistSong(playlist, song);
                                        }
                                        else // otherwise we changed the index, but the player does not store index of songs yet....
                                        { }
                                    }
                                }
                                catch (FormatException)
                                {

                                }
                                break;
                            case "playlist":
                                try
                                {
                                    var id = Guid.Parse(Path.GetFileNameWithoutExtension(item.Name));
                                    if (item.Deleted != null)
                                    {
                                        var toDeleted = MusicStore.Instance.PlayLists.FirstOrDefault(x => x.Id == id);
                                        if (toDeleted != null)
                                            await MusicStore.Instance.RemovePlaylist(toDeleted);
                                    }
                                    else
                                    {
                                        var toChange = MusicStore.Instance.PlayLists.FirstOrDefault(x => x.Id == id);
                                        if (toChange is null) // we need to create it
                                            await MusicStore.Instance.CreatePlaylist(item.Audio.Album, id);
                                        else // otherwise rename it
                                            toChange.Name = item.Audio.Album;
                                    }
                                }
                                catch (FormatException)
                                {

                                }

                                break;
                            default:
                                break;
                        }
                    }
                }




            }



            async Task<PlayListCollectionState> GetCurrentPlayListState()
            {
                await MusicStore.Instance.WhenInitilized;
                return new PlayListCollectionState(

                    playlists: MusicStore.Instance.PlayLists.ToArray().Select(p =>
                    {

                        return new PlaylistState(
                            id: p.Id,
                            name: p.Name,
                            songs: p.Songs.ToArray().Select(s =>
                                new SongState(

                                    libraryProvider: s.LibraryProvider,
                                    mediaId: s.MediaId
                                )).ToImmutableArray()
                        );
                    }).ToImmutableArray()
                );
            }
        }


        private static async Task UploadPlaylistEntry(DriveItem playlistFolder, Guid playlistId, SongState song, int index, CancellationToken cancellationToken)
        {
            using (var source = new MemoryStream(MP3Const.header.ToArray()))
            using (var stream = new MemoryStream())
            {
                await source.CopyToAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);

                var abstracStream = new TagLib.StreamFileAbstraction("x.mp3", stream, stream);
                using (var tagFile = TagLib.File.Create(abstracStream))
                {
                    var tag = tagFile.Tag;
                    tag.Track = (uint)index;
                    tag.Title = "song";
                    await Task.Run(() => tagFile.Save());
                }
                using (var uploadStream = new MemoryStream(stream.ToArray()))
                    await UploadFile(playlistFolder, $"{playlistId}~{song.LibraryProvider}~{song.MediaId}.mp3", uploadStream, cancellationToken);
            }
        }
        private static async Task DeletePlaylistEntry(DriveItem playlistFolder, Guid playlistId, SongState song, CancellationToken cancellationToken)
        {
            await DeleteFile(playlistFolder, $"{playlistId}~{song.LibraryProvider}~{song.MediaId}.mp3", cancellationToken);
        }

        private static async Task DeletePlaylist(DriveItem playlistFolder, Guid playlistId, CancellationToken cancellationToken)
        {
            await DeleteFile(playlistFolder, $"{playlistId}.mp3", cancellationToken);
        }

        private static async Task UploadPlaylist(DriveItem playlistFolder, Guid playlistId, string name, CancellationToken cancellationToken)
        {
            using (var source = new MemoryStream(MP3Const.header.ToArray()))
            using (var stream = new MemoryStream())
            {
                await source.CopyToAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);

                var abstracStream = new TagLib.StreamFileAbstraction("x.mp3", stream, stream);
                using (var tagFile = TagLib.File.Create(abstracStream))
                {
                    var tag = tagFile.Tag;
                    tag.Album = name;
                    tag.Title = "playlist";
                    await Task.Run(() => tagFile.Save());
                }
                using (var uploadStream = new MemoryStream(stream.ToArray()))
                    await UploadFile(playlistFolder, $"{playlistId}.mp3", uploadStream, cancellationToken);
            }
        }

        private async Task<DriveItem> EnsureFolderExists(IDriveItemRequestBuilder appRoot, string name, CancellationToken cancellationToken)
        {

            try
            {
                return await appRoot.Children[name].Request().Select("id,name").GetAsync(cancellationToken);

            }
            catch (Microsoft.Graph.ServiceException ex1) when (ex1.StatusCode == System.Net.HttpStatusCode.NotFound)
            {


                var driveItem = new DriveItem()
                {
                    Name = name,
                    Folder = new Folder(),

                    AdditionalData = new Dictionary<string, object>() { { "@microsoft.graph.conflictBehavior", "fail" } }
                };
                try
                {

                    return await appRoot.Children.Request().Select("id,name").AddAsync(driveItem, cancellationToken);
                }
                catch (Microsoft.Graph.ServiceException ex2) when (ex2.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    return await appRoot.Children[name].Request().Select("id,name").GetAsync(cancellationToken);

                }
            }
        }
        private static async Task UploadFile(DriveItem folder, string name, Stream stream, CancellationToken cancellationToken)
        {
            var upoadSession = await MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Items[folder.Id].ItemWithPath(name).CreateUploadSession().Request().PostAsync(cancellationToken);

            var maxChunkSize = 5 * 1024 * 1024; // 5MB is the default. But we wan't to make shure for the buffer below
            var provider = new ChunkedUploadProvider(upoadSession, MicrosoftGraphService.Instance.GraphProvider, stream, maxChunkSize);

            // Setup the chunk request necessities
            var chunkRequests = provider.GetUploadChunkRequests();
            var readBuffer = new byte[maxChunkSize];
            var trackedExceptions = new List<Exception>();
            DriveItem itemResult = null;

            //upload the chunks
            foreach (var request in chunkRequests)
            {
                // Do your updates here: update progress bar, etc.
                // ...
                // Send chunk request
                var chunkResult = await provider.GetChunkRequestResponseAsync(request, readBuffer, trackedExceptions);

                if (chunkResult.UploadSucceeded)
                    itemResult = chunkResult.ItemResponse;
            }

            // Check that upload succeeded
            if (itemResult == null)
            {
                // Retry the upload
                // ...
            }
        }
        private static async Task DeleteFile(DriveItem folder, string name, CancellationToken cancellationToken)
        {
            try
            {
                await MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Items[folder.Id].ItemWithPath(name).Request().DeleteAsync(cancellationToken);

            }
            catch (Microsoft.Graph.ServiceException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {

                // It was already deleted.
            }
        }

        private async Task ClearData()
        {
            using (this.StartLoad())
            {
                await MusicStore.Instance.WhenInitilized;
                var toDelete = MusicStore.Instance.Albums.SelectMany(x => x.Songs).SelectMany(x => x.Songs).Where(x => x.LibraryProvider == this.Id);

                await MusicStore.Instance.RemoveSong(toDelete);
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values.Remove(ONE_DRIVE_MUSIC_DELTA_TOKEN);
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

        private async Task Update(CancellationToken token)
        {
            using (this.StartLoad())
            {

                if (!MicrosoftGraphService.Instance.IsAuthenticated)
                    if (!await MicrosoftGraphService.Instance.TryLoginAsync())
                        throw new NotAuthenticatedException();

                await MusicStore.Instance.WhenInitilized;

                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                var lastToken = localSettings.Values[ONE_DRIVE_MUSIC_DELTA_TOKEN] as string;
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
                    var response = await deltaRequest.GetAsync(token);
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

                IEnumerable<DriveItem> toAdd = work.DriveItems.ToArray();
                var mediaFolder = await this.GetDataStoreFolder(StorageType.Media);
                var coverFolder = await this.GetDataStoreFolder(StorageType.Cover);

                System.Collections.Concurrent.ConcurrentBag<DriveItem> failedItems;
                do
                {

                    //var problems = toAdd.GroupBy(x => x.Id).Where(x => x.Count() > 1).ToArray();
                    failedItems = new System.Collections.Concurrent.ConcurrentBag<DriveItem>();



                    var allTasks = toAdd.Select(driveItem => new Task<Task<bool>>(async () =>
                     {
                         try
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
                                     CurrentDownload currentDownload = null;

                                     string[] composers = default;
                                     string[] performers = default;
                                     string[] genres = default;
                                     uint year = default;
                                     string joinedAlbumArtists = default;
                                     string album = default;
                                     string title = default;
                                     int track = default;
                                     int disc = default;
                                     TimeSpan duration = default;
                                     TagLib.IPicture picture = default;
                                     string targetFileName = default;

                                     if (token.IsCancellationRequested)
                                         return (composers, performers, genres, year, joinedAlbumArtists, album, title, track, disc, duration, picture, targetFileName);

                                     await this.imageSemaphore.WaitAsync(token);
                                     try
                                     {
                                         await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                         {
                                             currentDownload = new CurrentDownload(driveItem)
                                             {
                                                 MaximumToRead = 1
                                             };


                                             work.CurrentWork.Add(currentDownload);
                                         });

                                         var extension = Path.GetExtension(driveItem.Name);
                                         targetFileName = $"{driveItem.Id}{extension}";
                                         var temporaryFile = await mediaFolder.CreateFileAsync(targetFileName, CreationCollisionOption.GenerateUniqueName);
                                         var content = await MicrosoftGraphService.Instance.GraphProvider.Me.Drive.Items[driveItem.Id].Content.Request().GetAsync(token);


                                         using (var destinationStream = await temporaryFile.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.None))
                                         using (var sourceStream = content)
                                         using (var dStream = destinationStream.AsStream())
                                         {
                                             Progress<long> progress = null;
                                             await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                             {
                                                 currentDownload.MaximumToRead = dStream.Length;
                                                 progress = new Progress<long>((readed) => currentDownload.CurrentRead = readed);
                                             });
                                             await sourceStream.CopyToAsync(dStream, token, progress);
                                             dStream.Seek(0, SeekOrigin.Begin);
                                             var abstracStream = new TagLib.StreamFileAbstraction(temporaryFile.Name, dStream, null);



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

                                             var musicProperties = await temporaryFile.Properties.GetMusicPropertiesAsync();
                                             duration = musicProperties.Duration;

                                         }
                                         if (temporaryFile.Name != targetFileName)
                                         {
                                             // It seems that closing the stream takes some time. tring to rename the file can fail.
                                             await Task.Delay(3);
                                             var mediaFile = await mediaFolder.CreateFileAsync(driveItem.Id, Windows.Storage.CreationCollisionOption.OpenIfExists);
                                             System.Diagnostics.Debug.WriteLine($"BEFORE RENAME {mediaFile.Path}");
                                             await temporaryFile.MoveAndReplaceAsync(mediaFile);
                                         }


                                         return (composers, performers, genres, year, joinedAlbumArtists, album, title, track, disc, duration, picture, targetFileName);
                                     }
                                     finally
                                     {
                                         this.imageSemaphore.Release();
                                         await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                         {
                                             if (currentDownload != null)
                                             {

                                                 work.CurrentWork.Remove(currentDownload);
                                             }
                                         });
                                     }
                                 });


                                 var mediaData = await downloadMuediaFileTask;
                                 if (token.IsCancellationRequested)
                                     return false;
                                 if (mediaData.picture != null)
                                 {
                                     var temporaryFile = await coverFolder.CreateFileAsync(driveItem.Id, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                                     using (var destinationStream = await temporaryFile.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.None))
                                     using (var sourceStream = new MemoryStream(mediaData.picture.Data.Data))
                                     using (var dstram = destinationStream.AsStream())
                                         await sourceStream.CopyToAsync(dstram);
                                 }




                                 var song = await MusicStore.Instance.AddSong(this.Id, mediaData.targetFileName,
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
                         }
                         catch (System.OperationCanceledException)
                         {
                             // We do not rescedule cancled downloads
                             return false;
                         }
                         catch (Exception ex)
                         {
                             System.Diagnostics.Debug.WriteLine(ex.Message);
                             failedItems.Add(driveItem);
                             return false;
                         }
                         await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => work.DriveItems.Remove(driveItem));
                         return true;

                     })).ToArray();

                    await RunInParallel(allTasks, 10, token);

                    var toAddReult = await Task.WhenAll(allTasks.Select(x => x.Unwrap()));

                    if (toAddReult.All(x => x)) // if we had an error, we don't want to save the delta token. Otherwise we will not download the errored file...
                        localSettings.Values[ONE_DRIVE_MUSIC_DELTA_TOKEN] = work.NextRequest;
                    toAdd = failedItems;
                } while (failedItems.Count > 0 && !token.IsCancellationRequested);
                this.OneDriveWork = null;
            }
        }


        private async static Task RunInParallel<T>(IEnumerable<Task<Task<T>>> tasks, int maxParalesm, CancellationToken cancellationToken)
        {
            var allTasks = new List<Task>(); // this wll help us to get all exceptions
            using (var semaphor = new SemaphoreSlim(maxParalesm))
            {

                foreach (var t in tasks)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await semaphor.WaitAsync(cancellationToken);
                    allTasks.Add(t.ContinueWith(c => c.Result.ContinueWith(c2 => { if (!cancellationToken.IsCancellationRequested) semaphor.Release(); })));
                    t.Start();
                }
                await Task.WhenAll(allTasks);

                // consume the completed semaphore Then we know we did released every still running task.
                for (int i = 0; i < maxParalesm; i++)
                    await semaphor.WaitAsync();

            }
        }
    }


    public class OneDriveWork
    {
        public ObservableCollection<CurrentDownload> CurrentWork { get; } = new ObservableCollection<CurrentDownload>();
        public ObservableCollection<DriveItem> DriveItems { get; }
        public string NextRequest { get; }
        public long DownloadSizeInBytes { get; }

        public OneDriveWork(IEnumerable<DriveItem> driveItems, string nextRequest)
        {
            this.DriveItems = new ObservableCollection<DriveItem>(driveItems ?? throw new ArgumentNullException(nameof(driveItems)));
            this.NextRequest = nextRequest ?? throw new ArgumentNullException(nameof(nextRequest));
            this.DownloadSizeInBytes = driveItems.Sum(x => x.Size ?? 0);
        }
    }

    public class CurrentDownload : DependencyObject
    {
        public DriveItem CurrentItem { get; }



        public long MaximumToRead
        {
            get { return (long)this.GetValue(MaximumToReadProperty); }
            set { this.SetValue(MaximumToReadProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaximumToRead.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaximumToReadProperty =
            DependencyProperty.Register("MaximumToRead", typeof(long), typeof(CurrentDownload), new PropertyMetadata(0L));



        public long CurrentRead
        {
            get { return (long)this.GetValue(CurrentReadProperty); }
            set { this.SetValue(CurrentReadProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentRead.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentReadProperty =
            DependencyProperty.Register("CurrentRead", typeof(long), typeof(CurrentDownload), new PropertyMetadata(0L));

        public CurrentDownload(DriveItem currentItem)
        {
            this.CurrentItem = currentItem ?? throw new ArgumentNullException(nameof(currentItem));
        }
    }

    public static class StreamExteions
    {
        public static async Task CopyToAsync(this Stream source, Stream destination, CancellationToken cancellationToken, IProgress<long> progress)
        {
            var bufferSize = source.GetCopyBufferSize();
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                long bytesTotal = 0;
                while (true)
                {
                    int bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    if (bytesRead == 0) break;
                    await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                    bytesTotal += bytesRead;
                    progress.Report(bytesTotal);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        // From source.dot.net
        private static int GetCopyBufferSize(this Stream source)
        {
            const int DefaultCopyBufferSize = 81920;
            int bufferSize = DefaultCopyBufferSize;

            if (source.CanSeek)
            {
                long length = source.Length;
                long position = source.Position;
                if (length <= position) // Handles negative overflows
                {
                    // There are no bytes left in the stream to copy.
                    // However, because CopyTo{Async} is virtual, we need to
                    // ensure that any override is still invoked to provide its
                    // own validation, so we use the smallest legal buffer size here.
                    bufferSize = 1;
                }
                else
                {
                    long remaining = length - position;
                    if (remaining > 0)
                    {
                        // In the case of a positive overflow, stick to the default size
                        bufferSize = (int)Math.Min(bufferSize, remaining);
                    }
                }
            }

            return bufferSize;
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
