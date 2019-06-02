﻿using MusicPlayer.Controls;
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

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace MusicPlayer
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class NowPlaying : Page
    {
        public NowPlaying()
        {
            this.InitializeComponent();
            this.InitAsync();
        }

        private async System.Threading.Tasks.Task InitAsync()
        {

            var covers = Core.MusicStore.Instance.LibraryImages.Select(x => new CoverData() { Id = x.imageId, Provider = x.providerId });
            this.backgroundLarge.Covers = covers;
        }
    }
}