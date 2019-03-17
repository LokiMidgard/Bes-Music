namespace MusicPlayer.Core
{
    public class SongCollectionChangedEventArgs
    {
        public CollectionAction Action { get; internal set; }
        public Song Song { get; internal set; }
    }
}