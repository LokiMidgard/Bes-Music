using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace MusicPlayer.Controls
{
    public class FullScreenViewmodel : DependencyObject
    {

        public FullScreenViewmodel()
        {
            BindToTransportControl(SwitchFullScreenCommandProperty, nameof(this.SwitchFullScreenCommand));
            BindToTransportControl(IsFullscreenProperty, nameof(this.IsFullscreen));
            BindToTransportControl(SymbolProperty, nameof(this.Symbol));

            void BindToTransportControl(DependencyProperty songProperty, string Path)
            {
                var myBinding = new Binding
                {
                    Source = FullScreenViewmodelPrivate.Instance,
                    Path = new PropertyPath(Path),
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                BindingOperations.SetBinding(this, songProperty, myBinding);
            }
        }

        public ICommand SwitchFullScreenCommand
        {
            get { return (ICommand)this.GetValue(SwitchFullScreenCommandProperty); }
            set { this.SetValue(SwitchFullScreenCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SwitchFullScreenCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SwitchFullScreenCommandProperty =
            DependencyProperty.Register("SwitchFullScreenCommand", typeof(ICommand), typeof(FullScreenViewmodel), new PropertyMetadata(DisabledCommand.Instance));




        public Symbol Symbol
        {
            get { return (Symbol)this.GetValue(SymbolProperty); }
            private set { this.SetValue(SymbolProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Symbol.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SymbolProperty =
            DependencyProperty.Register("Symbol", typeof(Symbol), typeof(FullScreenViewmodel), new PropertyMetadata(Symbol.FullScreen));



        public bool IsFullscreen
        {
            get { return (bool)this.GetValue(IsFullscreenProperty); }
            set { this.SetValue(IsFullscreenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsFullscreen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsFullscreenProperty =
            DependencyProperty.Register("IsFullscreen", typeof(bool), typeof(FullScreenViewmodel), new PropertyMetadata(false));


        private class FullScreenViewmodelPrivate : DependencyObject
        {

            private static FullScreenViewmodelPrivate instance;
            public static FullScreenViewmodelPrivate Instance
            {
                get
                {
                    if (instance is null)
                        instance = new FullScreenViewmodelPrivate();
                    return instance;
                }
            }

            public FullScreenViewmodelPrivate()
            {
                this.SwitchFullScreenCommand = new DelegateCommand(() => this.IsFullscreen = !this.IsFullscreen);
                this.IsFullscreen = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().IsFullScreenMode;
            }

            public ICommand SwitchFullScreenCommand
            {
                get { return (ICommand)this.GetValue(SwitchFullScreenCommandProperty); }
                set { this.SetValue(SwitchFullScreenCommandProperty, value); }
            }

            // Using a DependencyProperty as the backing store for SwitchFullScreenCommand.  This enables animation, styling, binding, etc...
            public static readonly DependencyProperty SwitchFullScreenCommandProperty =
                DependencyProperty.Register("SwitchFullScreenCommand", typeof(ICommand), typeof(FullScreenViewmodelPrivate), new PropertyMetadata(DisabledCommand.Instance));




            public Symbol Symbol
            {
                get { return (Symbol)this.GetValue(SymbolProperty); }
                private set { this.SetValue(SymbolProperty, value); }
            }

            // Using a DependencyProperty as the backing store for Symbol.  This enables animation, styling, binding, etc...
            public static readonly DependencyProperty SymbolProperty =
                DependencyProperty.Register("Symbol", typeof(Symbol), typeof(FullScreenViewmodelPrivate), new PropertyMetadata(Symbol.FullScreen));



            public bool IsFullscreen
            {
                get { return (bool)this.GetValue(IsFullscreenProperty); }
                set { this.SetValue(IsFullscreenProperty, value); }
            }

            // Using a DependencyProperty as the backing store for IsFullscreen.  This enables animation, styling, binding, etc...
            public static readonly DependencyProperty IsFullscreenProperty =
                DependencyProperty.Register("IsFullscreen", typeof(bool), typeof(FullScreenViewmodelPrivate), new PropertyMetadata(false, IsFullscreenChanged));

            private static void IsFullscreenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var me = d as FullScreenViewmodelPrivate;
                var isFullscreen = (bool)e.NewValue;

                if (isFullscreen)
                    Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
                else
                    Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().ExitFullScreenMode();

                me.Symbol = isFullscreen ? Symbol.BackToWindow : Symbol.FullScreen;
            }

        }
    }
}
