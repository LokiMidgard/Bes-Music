using System.Collections.Generic;

namespace MusicPlayer.Viewmodels
{
    internal class AlbumViewmodelComparer : IComparer<AlbumViewmodel>
    {
        public int Compare(AlbumViewmodel x, AlbumViewmodel y) => x.Model.Title.CompareTo(y.Model.Title);
    }
}