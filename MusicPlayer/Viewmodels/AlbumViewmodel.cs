using MusicPlayer.Controls;
using MusicPlayer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Media.Core;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MusicPlayer.Viewmodels
{

    public class AlbumViewmodel : DependencyObject, IEquatable<AlbumViewmodel>
    {

        public ICommand PlayAlbumCommand { get; }

        public Album Model { get; }



        private WeakReference<BitmapImage> cover;

        public async Task<ImageSource> LoadCoverAsync(CancellationToken cancellationToken)
        {
            if (this.cover != null && this.cover.TryGetTarget(out var target))
                return target;

            var stream = await this.Model.GetCoverImageSource(300, cancellationToken);
            return stream;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as AlbumViewmodel);
        }

        public bool Equals(AlbumViewmodel other)
        {
            return other != null &&
                   EqualityComparer<Album>.Default.Equals(this.Model, other.Model);
        }

        public override int GetHashCode()
        {
            return -623947254 + EqualityComparer<Album>.Default.GetHashCode(this.Model);
        }

        public AlbumViewmodel(Album item)
        {
            this.Model = item ?? throw new ArgumentNullException(nameof(item));
            this.PlayAlbumCommand = new DelegateCommand(async () =>
            {
                await MediaplayerViewmodel.Instance.ClearSongs();
                foreach (var songGroup in this.Model.Songs)
                {
                    await MediaplayerViewmodel.Instance.AddSong(songGroup.Songs.First());
                }
                await MediaplayerViewmodel.Instance.Play();

            });
        }

        public static bool operator ==(AlbumViewmodel viewmodel1, AlbumViewmodel viewmodel2)
        {
            return EqualityComparer<AlbumViewmodel>.Default.Equals(viewmodel1, viewmodel2);
        }

        public static bool operator !=(AlbumViewmodel viewmodel1, AlbumViewmodel viewmodel2)
        {
            return !(viewmodel1 == viewmodel2);
        }
    }
}
