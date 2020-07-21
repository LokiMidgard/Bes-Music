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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MusicPlayer.Controls
{
    public sealed partial class DownloadControl : UserControl
    {
        public DownloadControl()
        {
            this.InitializeComponent();
        }

        private void ItemsStackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsEventPresent("Windows.UI.Xaml.UIElement", nameof(this.PreviewKeyDown)))
            {
                var panel = sender as UIElement;
                panel.BringIntoViewRequested += this.ListView_BringIntoViewRequested;
            }
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
    }
}
