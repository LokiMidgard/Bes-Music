using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MusicPlayer.Helpers
{
    public class ConstantsHelper : Control
    {
        private static double playListHeightField;
        public static double PlayListHeightField
        {
            get => playListHeightField; set
            {
                if (playListHeightField != value)
                {
                    playListHeightField = value;
                    playListHeightChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(PlayListHeightField)));
                }
            }
        }

        private static event PropertyChangedEventHandler playListHeightChanged;


        public ConstantsHelper()
        {

            var weak = new WeakEventListener<ConstantsHelper, object, PropertyChangedEventArgs>(this)
            {
                OnEventAction = (instance, source, eventArgs) =>
                {
                    if (eventArgs.PropertyName == nameof(PlayListHeightField))
                        this.PlayListHeight = PlayListHeightField;
                },
            };
            playListHeightChanged += weak.OnEvent;

            this.PlayListHeight = PlayListHeightField;
        }



        public double PlayListHeightSet
        {
            get { return (double)GetValue(PlayListHeightSetProperty); }
            set { SetValue(PlayListHeightSetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PlayListHeightSet.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlayListHeightSetProperty =
            DependencyProperty.Register("PlayListHeightSet", typeof(double), typeof(ConstantsHelper), new PropertyMetadata(0.0, PlayListHeightChanged));



        public double PlayListHeight
        {
            get { return (double)this.GetValue(PlayListHeightProperty); }
            set { this.SetValue(PlayListHeightProperty, value); }
        }


        // Using a DependencyProperty as the backing store for PlayListHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlayListHeightProperty =
            DependencyProperty.Register("PlayListHeight", typeof(double), typeof(ConstantsHelper), new PropertyMetadata(0.0));

        private static void PlayListHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PlayListHeightField = (double)e.NewValue;
        }
    }
}
