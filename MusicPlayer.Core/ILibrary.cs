using System;
using System.Threading;
using System.Threading.Tasks;

namespace MusicPlayer.Core
{
    public interface ILibrary<TMediaType, TImageType> : ILibrary
    {
        Task<TImageType> GetImage(string id, int size, CancellationToken cancellationToken);
        Task<TMediaType> GetMediaSource(string id, CancellationToken cancellationToken);
    }
    public interface ILibrary
    {
        string Id { get; }
    }

    public static class LibraryExtension
    {
        public static async Task<TImageType> GetImageRetryAsync<TMediaType, TImageType>(this ILibrary<TMediaType, TImageType> library, string id, int size, CancellationToken cancellationToken)
        {
            // we will try multuple times
            for (int i = 0; i < 3; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(5 * i));
                if (cancellationToken.IsCancellationRequested)
                    return default;
                return await library.GetImage(id, size, cancellationToken);
            }
            return default;

        }
    }
}