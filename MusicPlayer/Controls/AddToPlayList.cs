using MusicPlayer.Core;
using MusicPlayer.Viewmodels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace MusicPlayer.Controls
{
    public class AddToPlayList
    {


        public static bool GetSupressCurrentPlayList(DependencyObject obj)
        {
            return (bool)obj.GetValue(SupressCurrentPlayListProperty);
        }

        public static void SetSupressCurrentPlayList(DependencyObject obj, bool value)
        {
            obj.SetValue(SupressCurrentPlayListProperty, value);
        }

        // Using a DependencyProperty as the backing store for SupressCurrentPlayList.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SupressCurrentPlayListProperty =
            DependencyProperty.RegisterAttached("SupressCurrentPlayList", typeof(bool), typeof(AddToPlayList), new PropertyMetadata(false));



        public static Song GetIsPlaylistMenue(MenuFlyout obj)
        {
            return (Song)obj.GetValue(IsPlaylistMenueProperty);
        }

        public static void SetIsPlaylistMenue(MenuFlyout obj, Song value)
        {
            obj.SetValue(IsPlaylistMenueProperty, value);
        }

        public static Song GetIsPlaylistSubMenue(MenuFlyoutSubItem obj)
        {
            return (Song)obj.GetValue(IsPlaylistSubMenueProperty);
        }

        public static void SetIsPlaylistSubMenue(MenuFlyoutSubItem obj, Song value)
        {
            obj.SetValue(IsPlaylistSubMenueProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsPlaylistMenue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsPlaylistMenueProperty =
            DependencyProperty.RegisterAttached("IsPlaylistMenue", typeof(Song), typeof(AddToPlayList), new PropertyMetadata(null, Changed));
        // Using a DependencyProperty as the backing store for IsPlaylistMenue.  This enables animation, styling, binding, etc...

        public static readonly DependencyProperty IsPlaylistSubMenueProperty =
            DependencyProperty.RegisterAttached("IsPlaylistSubMenue", typeof(Song), typeof(AddToPlayList), new PropertyMetadata(null, Changed));

        private static ConditionalWeakTable<MenuFlyout, NotifyCollectionChangedEventHandler> menuLookUp = new ConditionalWeakTable<MenuFlyout, NotifyCollectionChangedEventHandler>();

        private static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs ev)
        {
            IList<MenuFlyoutItemBase> items;
            NotifyCollectionChangedEventHandler handler;
            {
                if (d is MenuFlyoutSubItem subItem)
                {
                    items = subItem.Items;
                    handler = subItem.Tag as NotifyCollectionChangedEventHandler;
                }
                else if (d is MenuFlyout menu)
                {
                    items = menu.Items;
                    if (!menuLookUp.TryGetValue(menu, out handler))
                        handler = null;
                }
                else
                    throw new NotSupportedException();
            }

            var supressCurrentPlaylist = GetSupressCurrentPlayList(d);

            var song = ev.NewValue as Song;
            var songs = ev.NewValue as IEnumerable<Song>;
            var album = ev.NewValue as Album;
            var albums = ev.NewValue as IEnumerable<Album>;
            var playListSong = ev.NewValue as PlayingSong;
            var playListSongs = ev.NewValue as IEnumerable<PlayingSong>;

            var anythingSet = song is null && album is null && songs is null && albums is null && playListSongs is null && playListSongs is null;
            if (anythingSet && ev.NewValue != null)
                throw new NotSupportedException();

            var oldSong = ev.OldValue;

            if (oldSong != null)
            {
                ((INotifyCollectionChanged)App.Current.MusicStore.PlayLists).CollectionChanged -= handler;
                if (oldSong is INotifyCollectionChanged collectionChanged2)
                    collectionChanged2.CollectionChanged -= handler;

            }

            if (anythingSet)
                items.Clear();
            else
            {
                items.Clear();

                UpdateEntys(items, song, album, songs, albums, supressCurrentPlaylist, playListSong, playListSongs);

                App.Current.StopEverything.Register(() =>
                {
                    song = null;
                    songs = null;
                    album = null;
                    albums = null;
                    playListSong = null;
                    playListSongs = null;
                    items = null;
                });

                handler = async (sender, e) =>
                 {
                     if (d.Dispatcher.HasThreadAccess)
                     {
                         items.Clear();
                         UpdateEntys(items, song, album, songs, albums, supressCurrentPlaylist, playListSong, playListSongs);

                     }
                     else
                     {
                         await d.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                         {
                             items.Clear();
                             UpdateEntys(items, song, album, songs, albums, supressCurrentPlaylist, playListSong, playListSongs);
                         });
                     }
                 };



                if (ev.NewValue is INotifyCollectionChanged collectionChanged)
                    collectionChanged.CollectionChanged += handler;


                ((INotifyCollectionChanged)App.Current.MusicStore.PlayLists).CollectionChanged += handler;

                if (d is MenuFlyoutSubItem subItem)
                {
                    subItem.Tag = handler;
                }
                else if (d is MenuFlyout menu)
                {
                    menuLookUp.Remove(menu);
                    menuLookUp.Add(menu, handler);
                }
                else
                    throw new NotSupportedException();


            }


        }

        private static void UpdateEntys(IList<MenuFlyoutItemBase> items, Song song, Album album, IEnumerable<Song> songs, IEnumerable<Album> albums, bool supressCurrentPlaylist, PlayingSong playListSong, IEnumerable<PlayingSong> playListSongs)
        {
            if (!supressCurrentPlaylist)
            {
                App.Current.StopEverything.Register(() =>
                {
                    song = null;
                    songs = null;
                    album = null;
                    albums = null;
                    playListSong = null;
                    playListSongs = null;
                    items = null;
                });
                var currentPlaying = new MenuFlyoutItem()
                {
                    Text = "Add To Now Playing"
                };

                currentPlaying.Click += async (sender, e) =>
                {
                    if (song != null)
                        await App.Current.MediaplayerViewmodel.AddSong(song);
                    if (songs != null)
                        foreach (var item in songs)
                            await App.Current.MediaplayerViewmodel.AddSong(song);
                    if (album != null)
                        foreach (var item in album.Songs)
                            await App.Current.MediaplayerViewmodel.AddSong(item.Songs.First());
                    if (albums != null)
                        foreach (var item in albums.SelectMany(x => x.Songs))
                            await App.Current.MediaplayerViewmodel.AddSong(item.Songs.First());
                    if (playListSong != null)
                        await App.Current.MediaplayerViewmodel.AddSong(playListSong.Song);
                    if (playListSongs != null)
                        foreach (var item in playListSongs.Select(x => x.Song))
                            await App.Current.MediaplayerViewmodel.AddSong(item);
                };

                items.Add(currentPlaying);
                items.Add(new MenuFlyoutSeparator());
            }


            var newPlaylist = new MenuFlyoutItem()
            {
                Text = "Create Playlist"
            };

            var flyOut = GenerateFlyOut(song, album, songs, albums, playListSong, playListSongs);

            newPlaylist.Click += (sender, e) =>
            {
                flyOut.ShowAt(e.OriginalSource as FrameworkElement ?? Window.Current.Content as FrameworkElement);
            };

            items.Add(newPlaylist);

            if (App.Current.MusicStore.PlayLists?.Any() ?? false)
                items.Add(new MenuFlyoutSeparator());



            if (App.Current.MusicStore.PlayLists != null)
                foreach (var playList in App.Current.MusicStore.PlayLists)
                {
                    var addToPlaylist = new MenuFlyoutItem()
                    {
                        Text = playList.Name
                    };

                    addToPlaylist.Click += async (sender, e) =>
                    {
                        if (song != null)
                            await App.Current.MusicStore.AddPlaylistSong(playList, song);
                        if (album != null)
                            foreach (var s in album.Songs)
                                await App.Current.MusicStore.AddPlaylistSong(playList, s.Songs.First());
                        if (songs != null)
                            foreach (var s in songs)
                                await App.Current.MusicStore.AddPlaylistSong(playList, song);
                        if (albums != null)
                            foreach (var s in albums.SelectMany(x => x.Songs))
                                await App.Current.MusicStore.AddPlaylistSong(playList, s.Songs.First());
                        if (playListSong != null)
                            await App.Current.MusicStore.AddPlaylistSong(playList, playListSong.Song);
                        if (playListSongs != null)
                            foreach (var item in playListSongs.Select(x => x.Song))
                                await App.Current.MusicStore.AddPlaylistSong(playList, item);
                    };

                    items.Add(addToPlaylist);

                }
        }

        private static Flyout GenerateFlyOut(Song song, Album album, IEnumerable<Song> songs, IEnumerable<Album> albums, PlayingSong playListSong, IEnumerable<PlayingSong> playListSongs)
        {

            App.Current.StopEverything.Register(() =>
            {
                song = null;
                songs = null;
                album = null;
                albums = null;
                playListSong = null;
                playListSongs = null;
            });
            var stackPanel = new StackPanel();
            var flyout = new Flyout();

            var textbox = new TextBox();
            stackPanel.Children.Add(textbox);

            var button = new Button() { Content = "Create Playlist" };

            button.Click += async (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(textbox.Text))
                    return;

                flyout.Hide();
                var playlist = await App.Current.MusicStore.CreatePlaylist(textbox.Text.Trim());
                if (song != null)
                    await App.Current.MusicStore.AddPlaylistSong(playlist, song);
                if (album != null)
                    foreach (var s in album.Songs)
                        await App.Current.MusicStore.AddPlaylistSong(playlist, s.Songs.First());
                if (songs != null)
                    foreach (var s in songs)
                        await App.Current.MusicStore.AddPlaylistSong(playlist, song);
                if (albums != null)
                    foreach (var s in albums.SelectMany(x => x.Songs))
                        await App.Current.MusicStore.AddPlaylistSong(playlist, s.Songs.First());
                if (playListSong != null)
                    await App.Current.MusicStore.AddPlaylistSong(playlist, playListSong.Song);
                if (playListSongs != null)
                    foreach (var item in playListSongs.Select(x => x.Song))
                        await App.Current.MusicStore.AddPlaylistSong(playlist, item);

            };

            stackPanel.Children.Add(button);


            flyout.Content = stackPanel;
            return flyout;
        }
    }
}
