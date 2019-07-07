using MusicPlayer.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MusicPlayer.Controls
{
    public class AddToPlayList
    {


        public static Song GetIsPlaylistMenue(MenuFlyoutSubItem obj)
        {
            return (Song)obj.GetValue(IsPlaylistMenueProperty);
        }

        public static void SetIsPlaylistMenue(MenuFlyoutSubItem obj, Song value)
        {
            obj.SetValue(IsPlaylistMenueProperty, value);
        }

        public static Song GetIsPlaylistMenue(MenuFlyout obj)
        {
            return (Song)obj.GetValue(IsPlaylistMenueProperty);
        }

        public static void SetIsPlaylistMenue(MenuFlyout obj, Song value)
        {
            obj.SetValue(IsPlaylistMenueProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsPlaylistMenue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsPlaylistMenueProperty =
            DependencyProperty.RegisterAttached("IsPlaylistMenue", typeof(Song), typeof(AddToPlayList), new PropertyMetadata(null, Changed));

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

            var song = ev.NewValue as Song;
            var album = ev.NewValue as Album;
            if (song is null && album is null && ev.NewValue != null)
                throw new NotSupportedException();

            var oldSong = ev.OldValue;

            if (oldSong != null)
                ((INotifyCollectionChanged)MusicStore.Instance.PlayLists).CollectionChanged -= handler;

            if (song is null && album is null)
                items.Clear();
            else
            {
                items.Clear();

                UpdateEntys(items, song, album);

                handler = (sender, e) =>
                {
                    UpdateEntys(items, song, album);

                };

                ((INotifyCollectionChanged)MusicStore.Instance.PlayLists).CollectionChanged += handler;

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

        private static void UpdateEntys(IList<MenuFlyoutItemBase> items, Song song, Album album)
        {
            var currentPlaying = new MenuFlyoutItem()
            {
                Text = "Add To Now Playing"
            };

            currentPlaying.Click += async (sender, e) =>
            {
                if (song != null)
                    await Viewmodels.MediaplayerViewmodel.Instance.AddSong(song);
                if (album != null)
                    foreach (var item in album.Songs)
                        await Viewmodels.MediaplayerViewmodel.Instance.AddSong(item.Songs.First());
            };

            items.Add(currentPlaying);

            items.Add(new MenuFlyoutSeparator());

            var newPlaylist = new MenuFlyoutItem()
            {
                Text = "Create Playlist"
            };

            var flyOut = GenerateFlyOut(song, album);

            newPlaylist.Click += async (sender, e) =>
            {
                flyOut.ShowAt(App.Shell);
            };

            items.Add(newPlaylist);

            if (MusicStore.Instance.PlayLists.Any())
                items.Add(new MenuFlyoutSeparator());




            foreach (var playList in MusicStore.Instance.PlayLists)
            {
                var addToPlaylist = new MenuFlyoutItem()
                {
                    Text = playList.Name
                };

                addToPlaylist.Click += async (sender, e) =>
                {
                    if (song != null)
                        await MusicStore.Instance.AddPlaylistSong(playList, song);
                    if (album != null)
                        foreach (var s in album.Songs)
                            await MusicStore.Instance.AddPlaylistSong(playList, s.Songs.First());
                };

                items.Add(addToPlaylist);

            }
        }

        private static Flyout GenerateFlyOut(Song song, Album album)
        {
            var stackPanel = new StackPanel();

            var textbox = new TextBox();
            stackPanel.Children.Add(textbox);

            var button = new Button() { Content = "Create Playlist" };

            button.Click += async (sender, e) =>
            {
                var playlist = await MusicStore.Instance.CreatePlaylist(textbox.Text ?? string.Empty);
                if (song != null)
                    await MusicStore.Instance.AddPlaylistSong(playlist, song);
                if (album != null)
                    foreach (var s in album.Songs)
                        await MusicStore.Instance.AddPlaylistSong(playlist, s.Songs.First());
            };

            stackPanel.Children.Add(button);


            return new Flyout() { Content = stackPanel };
        }
    }
}
