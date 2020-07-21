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

namespace MusicPlayer.Controls
{
    public sealed class SymbolButton : Button
    {



        public bool ShowText
        {
            get { return (bool)this.GetValue(ShowTextProperty); }
            set { this.SetValue(ShowTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowTextProperty =
            DependencyProperty.Register("ShowText", typeof(bool), typeof(SymbolButton), new PropertyMetadata(true));



        public Symbol Symbol
        {
            get { return (Symbol)this.GetValue(SymbolProperty); }
            set { this.SetValue(SymbolProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Symbol.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SymbolProperty =
            DependencyProperty.Register("Symbol", typeof(Symbol), typeof(SymbolButton), new PropertyMetadata(default(Symbol)));

        public SymbolButton()
        {
            this.DefaultStyleKey = typeof(SymbolButton);
        }
    }
}
