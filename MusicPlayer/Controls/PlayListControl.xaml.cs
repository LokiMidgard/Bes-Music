using MusicPlayer.Core;
using MusicPlayer.Viewmodels;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MusicPlayer.Controls
{
    public sealed partial class PlayListControl : UserControl
    {
        public PlayListControl()
        {
            this.InitializeComponent();
        }
        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is PlayList playList)
            {
                await MediaplayerViewmodel.Instance.ResetSongs(playList.Songs.ToImmutableArray());
            }
        }

        private async void MenuFlyoutItemPlay_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is PlayList playList)
            {
                await MediaplayerViewmodel.Instance.ResetSongs(playList.Songs.ToImmutableArray());
            }
        }
        private async void MenuFlyoutItemAddToCurrentPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is PlayList playList)
            {
                foreach (var song in playList.Songs)
                {
                    await MediaplayerViewmodel.Instance.AddSong(song);
                }
            }
        }
        private async void MenuFlyoutItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is PlayList playList)
            {
                await MusicStore.Instance.RemovePlaylist(playList);
            }
        }

        private void MenuFlyout_Opening(object sender, object e)
        {
            var t = sender as MenuFlyout;
            var dataContext = t.Target?.DataContext ?? (t.Target as ContentControl)?.Content;
            foreach (var item in t.Items)
            {
                item.DataContext = dataContext;
            }
        }

        private async void ExceptionHandlerConverter_OnError(Exception exception)
        {
            var dialog = new MessageDialog(exception.Message);
            await dialog.ShowAsync();
        }

        private void ItemsStackPanel_BringIntoViewRequested(UIElement sender, BringIntoViewRequestedEventArgs args)
        {
            if (args.TargetRect.Height <= Helpers.ConstantsHelper.PlayListHeightField)
            {

                var t = args.TargetRect;
                t = new Rect(t.X, t.Y, t.Width, t.Height + Helpers.ConstantsHelper.PlayListHeightField);
                args.TargetRect = t;
            }
        }
    }
}
