using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MusicPlayer.Viewmodels
{
    public class SortedGroup<TKey, TValue> : ObservableCollection<TValue>, IGrouping<TKey, TValue>
    {
        /// <summary>
        /// The Group Title
        /// </summary>
        public TKey Key
        {
            get;
        }

        private readonly IComparer<TValue> comparer;

        /// <summary>
        /// Constructor ensure that a Group Title is included
        /// </summary>
        /// <param name="key">string to be used as the Group Title</param>
        public SortedGroup(TKey key, IComparer<TValue> comparer)
        {
            this.Key = key;
            this.comparer = comparer;
        }

        /// <summary>
        /// Returns true if the group has a count more than zero
        /// </summary>
        public bool HasItems
        {
            get
            {
                return (this.Count != 0);
            }

        }

        public new void Add(TValue value)
        {
            var index = this.BinarySearch(value, this.comparer);
            if (index < 0)
                index = ~index;
            this.Insert(index, value);
        }




    }
}
