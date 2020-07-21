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
using Windows.Foundation.Metadata;
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

            this.Loaded += this.PlayListControl_Loaded;

        }

        private void PlayListControl_Loaded(object sender, RoutedEventArgs e)
        {
          
        }

        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is PlayList playList)
            {
                if (playList.Availability != Availability.NotAvailable)
                    await App.Current.MediaplayerViewmodel.ResetSongs(playList.Songs.ToImmutableArray());
                else
                    OneDriveLibrary.Instance.DownloadDataCommand.Execute(playList);
            }
        }

        private async void MenuFlyoutItemPlay_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is PlayList playList)
            {
                await App.Current.MediaplayerViewmodel.ResetSongs(playList.Songs.ToImmutableArray());
            }
        }
        private async void MenuFlyoutItemAddToCurrentPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is PlayList playList)
            {
                foreach (var song in playList.Songs)
                {
                    await App.Current.MediaplayerViewmodel.AddSong(song);
                }
            }
        }
        private async void MenuFlyoutItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is PlayList playList)
            {
                await App.Current.MusicStore.RemovePlaylist(playList);
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

        private void MenuFlyoutItemDownload_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is PlayList playList)
            {
                OneDriveLibrary.Instance.DownloadDataCommand.Execute(playList);
            }

        }

        private void ItemsStackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsEventPresent("Windows.UI.Xaml.UIElement", nameof(this.PreviewKeyDown)))
            {
                var panel = sender as UIElement;
                panel.BringIntoViewRequested += this.ItemsStackPanel_BringIntoViewRequested;
            }

        }
    }
}
