using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Controls
{
    internal class RandomList<T>
    {
        private readonly static Random r = new Random();
        private T[] data;
        private int index;

        public RandomList(IEnumerable<T> enumerable)
        {
            this.data = Shuffle(enumerable);
        }

        public T Next()
        {
            if (this.index >= this.data.Length)
            {
                this.index = 0;
                this.data = Shuffle(this.data);
            }
            return this.data[this.index++];
        }

        private static T[] Shuffle(IEnumerable<T> enumerable)
        {
            return enumerable.Select(x => (value: x, index: r.NextDouble())).OrderBy(x => x.index).Select(x => x.value).ToArray();
        }
    }
}