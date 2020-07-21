using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace MusicPlayer.Controls
{
    public sealed partial class AlbumControl : UserControl
    {
        private bool isTouch;
        private bool isMouseOver;



        public bool IsDisplayedLarge
        {
            get { return (bool)this.GetValue(IsDisplayedLargeProperty); }
            set { this.SetValue(IsDisplayedLargeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsDisplayedLarge.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsDisplayedLargeProperty =
            DependencyProperty.Register("IsDisplayedLarge", typeof(bool), typeof(AlbumControl), new PropertyMetadata(false, DisplayStateChanged));

        private static void DisplayStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = (AlbumControl)d;
            var isLarge = (bool)e.NewValue;
            if (isLarge)
                VisualStateManager.GoToState(me, "DisplayLarge", true);
            else
                VisualStateManager.GoToState(me, "DisplayNormal", true);
        }




        public AlbumControl()
        {
            this.Resources.Add("Infinit", double.PositiveInfinity);
            this.InitializeComponent();
            this.Loaded += this.AlbumControl_Loaded;
            this.Unloaded += this.AlbumControl_Unloaded;

            App.Current.StopEverything.Register(() =>
            {
                this.AlbumControl_Unloaded(null, null);
            });

        }

        private void AlbumControl_Unloaded(object sender, RoutedEventArgs e)
        {
            App.Current.PropertyChanged -= this.Current_PropertyChanged;
        }

        private void AlbumControl_Loaded(object sender, RoutedEventArgs e)
        {
            App.Current.PropertyChanged += this.Current_PropertyChanged;
            this.isTouch = App.Current.IsTochMode;
            this.UpdateMouseOverEffekt();
        }

        private void Current_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(App.IsTochMode))
                return;
            this.isTouch = App.Current.IsTochMode;
            this.UpdateMouseOverEffekt();
        }

        private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            this.isMouseOver = true;
            this.UpdateMouseOverEffekt();
        }

        private void Border_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            this.isMouseOver = false;
            this.UpdateMouseOverEffekt();
        }

        private void UpdateMouseOverEffekt()
        {
            if (!this.isMouseOver && !this.isTouch)
                VisualStateManager.GoToState(this, "Normal", false);
            else
                VisualStateManager.GoToState(this, "DoingOver", false);
        }
    }
}
