using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MusicPlayer.Viewmodels
{
    public class GroupedObservableCollection<TKey, TValue> : ObservableCollection<SortedGroup<TKey, TValue>>
    {
        private readonly Func<TValue, TKey> keySelector;
        private readonly IComparer<SortedGroup<TKey, TValue>> groupComparer;
        private readonly IComparer<TValue> valueComparer;
        private readonly Dictionary<TKey, SortedGroup<TKey, TValue>> keyLookup = new Dictionary<TKey, SortedGroup<TKey, TValue>>();

        public GroupedObservableCollection(Func<TValue, TKey> keySelector, IComparer<TKey> groupComparer, IComparer<TValue> valueComparer)
        {
            this.keySelector = keySelector;
            this.groupComparer = new GroupComparer(groupComparer);
            this.valueComparer = valueComparer;
        }

        public void Add(TValue value)
        {
            var key = this.keySelector(value);
            SortedGroup<TKey, TValue> group;
            if (this.keyLookup.ContainsKey(key))
                group = this.keyLookup[key];
            else
            {
                group = new SortedGroup<TKey, TValue>(key, this.valueComparer);
                this.keyLookup.Add(key, group);
                var index = this.BinarySearch(group, this.groupComparer);
                if (index < 0)
                    index = ~index;
                this.Insert(index, group);
            }

            group.Add(value);
        }

        public void Remove(TValue value)
        {
            var key = this.keySelector(value);
            if (this.keyLookup.ContainsKey(key))
            {
                var group = this.keyLookup[key];
                group.Remove(value);
                if (!group.HasItems)
                {
                    this.Remove(group);
                    this.keyLookup.Remove(key);
                }
            }
        }



        private class GroupComparer : IComparer<SortedGroup<TKey, TValue>>
        {
            private readonly IComparer<TKey> groupComparer;

            public GroupComparer(IComparer<TKey> groupComparer) => this.groupComparer = groupComparer;

            public int Compare(SortedGroup<TKey, TValue> x, SortedGroup<TKey, TValue> y) => this.groupComparer.Compare(x.Key, y.Key);
        }

    }
}
