using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace MusicPlayer
{
    public sealed class CustomTransportControl : MediaTransportControls
    {

        static CustomTransportControl()
        {
            
        }

        public CustomTransportControl()
        {
            this.DefaultStyleKey = typeof(CustomTransportControl);
            
        }

        protected override void OnApplyTemplate()
        {
            // Find the custom button and create an event handler for its Click event.
            var likeButton = GetTemplateChild("FullWindowButton2") as Button;
            likeButton.Click += FullWindowButton_Click;
            base.OnApplyTemplate();
        }

        private void FullWindowButton_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
