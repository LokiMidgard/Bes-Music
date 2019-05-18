using System.Collections.Generic;

namespace MusicPlayer.Viewmodels
{
    internal class AlbumViewmodelComparer : IComparer<AlbumViewmodel>
    {
        public int Compare(AlbumViewmodel x, AlbumViewmodel y) => x.Name.CompareTo(y.Name);
    }
}