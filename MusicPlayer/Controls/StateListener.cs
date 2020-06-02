using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace MusicPlayer.Controls
{

    public enum StateDisplayMode
    {
        ShowAlwaysInTouchMode,
        ShowNeverInTouchMode,
        DefaultInTouchMode
    }

    [ContentProperty(Name = "Content")]
    public class StateListener : UserControl
    {



        public StateDisplayMode DisplayMode
        {
            get { return (StateDisplayMode)GetValue(DisplayModeProperty); }
            set { SetValue(DisplayModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DisplayMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.Register("DisplayMode", typeof(StateDisplayMode), typeof(StateListener), new PropertyMetadata(StateDisplayMode.DefaultInTouchMode));




        private bool isTouch;
        private bool isMouseOver;

        public StateListener()
        {
            this.PointerEntered += this.StateListener_PointerEntered;
            this.PointerExited += this.StateListener_PointerExited;
            this.Loaded += this.StateListener_Loaded;
            this.Unloaded += this.StateListener_Unloaded;
        }

        private void StateListener_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            App.Current.PropertyChanged -= this.Current_PropertyChanged;

        }

        private void StateListener_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (App.Current is null)
                return;
            App.Current.PropertyChanged += this.Current_PropertyChanged;
            this.isTouch = App.Current.IsTochMode;
            if (this.isTouch)
                VisualStateManager.GoToState(this, "Touch", true);
            else
                VisualStateManager.GoToState(this, "NoTouch", true);
            this.UpdateMouseOverEffekt();
        }

        private void Current_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(App.IsTochMode))
                return;
            this.isTouch = App.Current.IsTochMode;
            if (this.isTouch)
                VisualStateManager.GoToState(this, "Touch", true);
            else
                VisualStateManager.GoToState(this, "NoTouch", true);
            this.UpdateMouseOverEffekt();
        }

        private void StateListener_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            this.isMouseOver = false;

            this.UpdateMouseOverEffekt();
        }

        private void StateListener_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            this.isMouseOver = true;
            this.UpdateMouseOverEffekt();
        }

        private void UpdateMouseOverEffekt()
        {
            if (this.isTouch)
            {
                switch (this.DisplayMode)
                {
                    case StateDisplayMode.ShowAlwaysInTouchMode:
                        VisualStateManager.GoToState(this, "DoingOver", true);
                        return;
                    case StateDisplayMode.ShowNeverInTouchMode:
                        VisualStateManager.GoToState(this, "Normal", true);
                        return;
                    case StateDisplayMode.DefaultInTouchMode:
                    default:
                        break;
                }
            }

            if (!this.isMouseOver && !this.isTouch)
                VisualStateManager.GoToState(this, "Normal", true);
            else
                VisualStateManager.GoToState(this, "DoingOver", true);
        }
    }
}
