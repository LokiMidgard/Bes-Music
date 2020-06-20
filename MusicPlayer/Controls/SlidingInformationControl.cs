using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace MusicPlayer.Controls
{
    public sealed class SlidingInformationControl : Control
    {
        public SlidingInformationControl()
        {
            this.DefaultStyleKey = typeof(SlidingInformationControl);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var root = (Grid)this.GetTemplateChild("root");

            var enterTitle = (Storyboard)root.Resources["EnterTitle"];
            var enterAlbum = (Storyboard)root.Resources["EnterAlbum"];
            var enterArtist = (Storyboard)root.Resources["EnterArtist"];

            var titleText = (TextBlock)this.GetTemplateChild("TitleText");
            var albumText = (TextBlock)this.GetTemplateChild("AlbumText");
            var artistText = (TextBlock)this.GetTemplateChild("ArtistText");


            var delay = TimeSpan.FromSeconds(5);
            enterTitle.Completed += async (sender, e) =>
            {
                await Task.Delay(delay);
                if (albumText.Text?.Length > 0)
                    enterAlbum.Begin();
                else if (artistText.Text?.Length > 0)
                    enterArtist.Begin();
                else
                    enterTitle.Begin();
            };
            enterAlbum.Completed += async (sender, e) =>
            {
                await Task.Delay(delay);
                if (artistText.Text?.Length > 0)
                    enterArtist.Begin();
                else if (titleText.Text?.Length > 0)
                    enterTitle.Begin();
                else
                    enterAlbum.Begin();
            };
            enterArtist.Completed += async (sender, e) =>
            {
                await Task.Delay(delay);
                if (titleText.Text?.Length > 0)
                    enterTitle.Begin();
                else if (albumText.Text?.Length > 0)
                    enterAlbum.Begin();
                else
                    enterArtist.Begin();
            };

            enterTitle.Begin();
        }


        public string Title
        {
            get { return (string)this.GetValue(TitleProperty); }
            set { this.SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(SlidingInformationControl), new PropertyMetadata(string.Empty));



        public string Album
        {
            get { return (string)this.GetValue(AlbumProperty); }
            set { this.SetValue(AlbumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Album.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AlbumProperty =
            DependencyProperty.Register("Album", typeof(string), typeof(SlidingInformationControl), new PropertyMetadata(string.Empty));



        public string Interpret
        {
            get { return (string)this.GetValue(InterpretProperty); }
            set { this.SetValue(InterpretProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Interpret.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InterpretProperty =
            DependencyProperty.Register("Interpret", typeof(string), typeof(SlidingInformationControl), new PropertyMetadata(string.Empty));


    }
}
