using System;

namespace MusicPlayer.Core
{
    public class AlbumCollectionChangedEventArgs
    {
        public AlbumChanges Action { get; internal set; }
        public Album Album { get; internal set; }
    }

    [Flags]
    public enum AlbumChanges
    {
        Added = 1,
        Deleted = 2,
        ImageUpdated = 4,
        NameUpdated = 8,
        SongsUpdated = 16,
    }
}