using System;
using System.Collections.Generic;
using System.Text;

namespace MusicPlayer.Core
{
    public static class LibraryRegistry<TMediaType, TImageType>
    {

        private static readonly Dictionary<string, ILibrary<TMediaType, TImageType>> librarys = new Dictionary<string, ILibrary<TMediaType, TImageType>>();


        public static ILibrary<TMediaType, TImageType> Get(string Id) => librarys[Id];

        public static void Register(ILibrary<TMediaType, TImageType> library) => librarys.Add(library.Id, library);


    }

    public static class LibraryRegistry
    {
        public static void Register<TMediaType, TImageType>(ILibrary<TMediaType, TImageType> library) => LibraryRegistry<TMediaType, TImageType>.Register(library);
    }

}
