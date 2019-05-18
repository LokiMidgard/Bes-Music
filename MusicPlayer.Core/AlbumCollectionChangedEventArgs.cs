using System;

namespace MusicPlayer.Core
{
    public class AlbumCollectionChangedEventArgs
    {
        public AlbumChanges Action { get; internal set; }
        public string AlbumName { get; internal set; }
        public string AlbumInterpret { get; internal set; }
        public string ProviderId { get; internal set; }
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