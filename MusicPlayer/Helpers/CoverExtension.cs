using MusicPlayer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace System
{
    internal static class CoverExtension
    {

        public static async Task<RandomAccessStreamReference> GetCover(this Song song, int size, CancellationToken cancellationToken = default)
        {
            if (song.LibraryImageId is null)
                return null;
            var image = await LibraryRegistry<MediaSource, StorageItemThumbnail>.Get(song.LibraryProvider).GetImage(song.LibraryImageId, size, cancellationToken);
            if (image is null)
                return null;
            else
                return RandomAccessStreamReference.CreateFromStream(image);
        }

        public static async Task<RandomAccessStreamReference> GetCover(this Album album, int size, CancellationToken cancellationToken = default)
        {
            if (!album.LibraryImages.Any())
                return null;


            var song = album.LibraryImages.First();
            if (song.imageId is null || song.providerId is null)
                return null;
            var image = await LibraryRegistry<MediaSource, StorageItemThumbnail>.Get(song.providerId).GetImage(song.imageId, size, cancellationToken);
            if (image is null)
                return null;
            else
                return RandomAccessStreamReference.CreateFromStream(image);
        }

    }
}
