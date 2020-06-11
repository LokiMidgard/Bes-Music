using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using MusicPlayer.Viewmodels;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        //private MediaPlayer player;

        public AlbumCollectionViewmodel Albums => AlbumCollectionViewmodel.Instance;

        public MainPage()
        {
            this.DataContext = this.Albums;
            this.InitializeComponent();

            //this.player = new MediaPlayer();

            this.InitMusicLibraryAsync();



            //this.renderTarget.RenderAsync(toRender);
        }


        public  void InitMusicLibraryAsync()
        {
            //var files = await this.GetFilesAsync(KnownFolders.MusicLibrary);
            ////files.Select(x=>x.Properties.GetMusicPropertiesAsync())
            //var p = await files.First().Properties.GetMusicPropertiesAsync();

            //var current = files.First(x=>x.Name.EndsWith(".mp3"));
            //this.player.Source = MediaSource.CreateFromStorageFile(current);
            ////this.player.Play();
            //this.player.AutoPlay = true;
        }

        private async void ToRender_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            var root = args.ItemContainer.ContentTemplateRoot as FrameworkElement;
            var image = root.FindName("cover") as Image;
            var vm = args.Item as AlbumViewmodel;
            if (args.Phase == 0)
            {
                var oldCancel = root.Tag as CancellationTokenSource;
                if (oldCancel != null)
                {
                    oldCancel.Cancel();
                    oldCancel.Dispose();
                }
                var cancel = new CancellationTokenSource();
                args.RegisterUpdateCallback(this.ToRender_ContainerContentChanging);
                root.Tag = cancel;
                image.Opacity = 0;
            }
            else if (args.Phase == 1)
            {
                var cancel = root.Tag as CancellationTokenSource;
                var imageSource = await vm.LoadCoverAsync(cancel.Token);
                if (!cancel.IsCancellationRequested)
                {
                    image.Source = imageSource;
                    image.Opacity = 1;
                }

            }

            args.Handled = true;

        }

        private void AlbumClicked(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as AlbumViewmodel;
            var container = this.toRender.ContainerFromItem(e.ClickedItem) as GridViewItem;


            var root = container.ContentTemplateRoot as FrameworkElement;
            var cover = root.FindName("cover") as UIElement;
            var name = root.FindName("name") as UIElement;

            ConnectedAnimationService.GetForCurrentView()
                .PrepareToAnimate("forwardAnimationCover", cover);
            ConnectedAnimationService.GetForCurrentView()
                            .PrepareToAnimate("forwardAnimationName", name);

            Services.NavigationService.Navigate<Pages.AlbumPage>(item);
        }
        private childItem FindVisualChild<childItem>(DependencyObject obj)
            where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = this.FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

    }
}
