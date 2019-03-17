using System.Threading;
using System.Threading.Tasks;

namespace MusicPlayer.Core
{
    public interface ILibrary<TMediaType, TImageType>
    {
        string Id { get; }
        Task<TImageType> GetImage(string id, int size, CancellationToken cancellationToken);
        Task<TMediaType> GetMediaSource(string id);



    }
}