﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Helpers;
using MusicPlayer.Views;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace MusicPlayer.Services
{
    // For instructions on testing this service see https://github.com/Microsoft/WindowsTemplateStudio/blob/master/docs/UWP/features/whats-new-prompt.md
    public static class WhatsNewDisplayService
    {
        private static bool shown = false;

        internal static async Task ShowIfAppropriateAsync()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, async () =>
                {
                    if (SystemInformation.IsAppUpdated && !shown)
                    {
                        shown = true;
                        var dialog = new WhatsNewDialog();
                        await dialog.ShowAsync();
                    }
                });
        }
    }
}
