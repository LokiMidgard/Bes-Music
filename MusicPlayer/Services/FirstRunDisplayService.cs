using Microsoft.Toolkit.Uwp.Helpers;
using MusicPlayer.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace MusicPlayer.Services
{
    public static class FirstRunDisplayService
    {
        private static bool shown = false;

        internal static async Task ShowIfAppropriateAsync()
        {
            await Windows.UI.Xaml.Window.Current.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, async () =>
                {
                    if (SystemInformation.IsFirstRun && !shown)
                    {
                        shown = true;
                        var dialog = new FirstRunDialog();
                        await dialog.ShowAsync();
                    }
                });
        }
    }

}
