using MusicPlayer.Controls;
using MusicPlayer.Core;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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




        public bool HasMultipleDiscs
        {
            get { return (bool)this.GetValue(HasMultipleDiscsProperty); }
            set { this.SetValue(HasMultipleDiscsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HasMultipleDiscs.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasMultipleDiscsProperty =
            DependencyProperty.Register("HasMultipleDiscs", typeof(bool), typeof(AlbumViewmodel), new PropertyMetadata(false));



        private WeakReference<ImageSource> cover;

        public async Task<ImageSource> LoadCoverAsync(CancellationToken cancellationToken)
        {
            if (this.cover != null && this.cover.TryGetTarget(out var target))
                return target;

            var stream = await this.Model.GetCoverImageSource(300, cancellationToken);
            this.cover = new WeakReference<ImageSource>(stream);
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
            this.PlayAlbumCommand = new DelegateCommand<Song>(async (song) =>
            {
                await MediaplayerViewmodel.Instance.ResetSongs(this.Model.Songs.Select(x => x.Songs.First()).ToImmutableArray(), song);
            });

            ((System.Collections.Specialized.INotifyCollectionChanged)this.Model.Songs).CollectionChanged += this.AlbumViewmodel_CollectionChanged;
            this.CollectionUpdated();
        }

        private void AlbumViewmodel_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => this.CollectionUpdated();

        private void CollectionUpdated()
        {
            this.HasMultipleDiscs = this.Model.Songs.Select(x => x.DiscNumber).Distinct().Skip(1).Any();
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
