using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace MusicPlayer.Pages
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {



        public double HubSize
        {
            get { return (double)this.GetValue(HubSizeProperty); }
            set { this.SetValue(HubSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HubSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HubSizeProperty =
            DependencyProperty.Register("HubSize", typeof(double), typeof(MainPage), new PropertyMetadata(60.0));



        public MainPage()
        {
            this.InitializeComponent();
            this.SizeChanged += this.MainPage_SizeChanged;
            this.HubSize = Math.Max(30, this.Width - 60);

            this.NavigationCacheMode = NavigationCacheMode.Enabled;
        }



        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
        }

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.HubSize = Math.Max(30, e.NewSize.Width - 60);
        }

        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedSong = (e.OriginalSource as FrameworkElement).DataContext as Viewmodels.PlayingSong;
            await App.Current.MediaplayerViewmodel.RemoveSong(selectedSong);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MusicPlayer.Services.NavigationService.Navigate<NowPlaying>();
        }

        private void ListView_BringIntoViewRequested(UIElement sender, BringIntoViewRequestedEventArgs args)
        {
            if (args.TargetRect.Height <= Helpers.ConstantsHelper.PlayListHeightField)
            {

                var t = args.TargetRect;
                t = new Rect(t.X, t.Y, t.Width, t.Height + Helpers.ConstantsHelper.PlayListHeightField);
                args.TargetRect = t;
            }
           
        }

        private void ItemsStackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsEventPresent("Windows.UI.Xaml.UIElement", nameof(this.PreviewKeyDown)))
            {
                var panel = sender as UIElement;
                panel.BringIntoViewRequested += this.ListView_BringIntoViewRequested;
            }
        }

    }


}
