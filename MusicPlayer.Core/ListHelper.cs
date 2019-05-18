using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class ListHelper
    {
        /// <summary>
        /// The index. if index negative it is the bit compliment of the insertion index
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="value"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static int BinarySearch<T>(this IList<T> list, T value, IComparer<T> comparer) => BinarySearch(list, 0, list.Count, value, comparer);

        /// <summary>
        /// The index. if index negative it is the bit compliment of the insertion index
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="list"></param>
        /// <param name="value"></param>
        /// <param name="transform"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static int BinarySearch<TElement, TResult>(this IList<TElement> list, TElement value, Func<TElement, TResult> transform, IComparer<TResult> comparer) => BinarySearch(list, 0, list.Count, value, transform, comparer);
        public static int BinarySearch<TElement, TResult>(this IList<TElement> list, TElement value, Func<TElement, TResult> transform, Func<TResult, TResult, int> compare) => BinarySearch(list, 0, list.Count, value, transform, new DelegateCompare<TResult>(compare));

        public static int BinarySearch<TList, TElement, TResult>(this IList<TList> list, TElement value, Func<TElement, TResult> elementTransform, Func<TList, TResult> listTransform, IComparer<TResult> comparer)
            => BinarySearch(list, 0, list.Count, value, elementTransform, listTransform, comparer);

        public static int BinarySearch<TList, TElement, TResult>(this IList<TList> list, TElement value, Func<TElement, TResult> elementTransform, Func<TList, TResult> listTransform, Func<TResult, TResult, int> compare)
            => BinarySearch(list, 0, list.Count, value, elementTransform, listTransform, new DelegateCompare<TResult>(compare));


        private static int BinarySearch<T>(this IList<T> list, int index, int length, T value, IComparer<T> comparer)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (list.Count - (index) < length)
                throw new ArgumentOutOfRangeException(nameof(index));


            if (comparer == null)
                comparer = Comparer<T>.Default;

            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                // i might overflow if lo and hi are both large positive numbers. 
                int i = GetMedian(lo, hi);
                int c = comparer.Compare(list[i], value);
                if (c == 0)
                    return i;
                if (c < 0)
                    lo = i + 1;
                else
                    hi = i - 1;
            }
            return ~lo;

            int GetMedian(int low, int hi2) => low + ((hi2 - low) >> 1);
        }

        private static int BinarySearch<TElement, TResult>(this IList<TElement> list, int index, int length, TElement value, Func<TElement, TResult> transform, IComparer<TResult> comparer)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (list.Count - (index) < length)
                throw new ArgumentOutOfRangeException(nameof(index));


            if (comparer == null)
                comparer = Comparer<TResult>.Default;

            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                // i might overflow if lo and hi are both large positive numbers. 
                int i = GetMedian(lo, hi);
                int c = comparer.Compare(transform(list[i]), transform(value));
                if (c == 0)
                    return i;
                if (c < 0)
                    lo = i + 1;
                else
                    hi = i - 1;
            }
            return ~lo;

            int GetMedian(int low, int hi2) => low + ((hi2 - low) >> 1);
        }
        private static int BinarySearch<TList, TElement, TResult>(this IList<TList> list, int index, int length, TElement value, Func<TElement, TResult> elementTransform, Func<TList, TResult> lsitTransform, IComparer<TResult> comparer)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (list.Count - (index) < length)
                throw new ArgumentOutOfRangeException(nameof(index));


            if (comparer == null)
                comparer = Comparer<TResult>.Default;

            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                // i might overflow if lo and hi are both large positive numbers. 
                int i = GetMedian(lo, hi);
                int c = comparer.Compare(lsitTransform(list[i]), elementTransform(value));
                if (c == 0)
                    return i;
                if (c < 0)
                    lo = i + 1;
                else
                    hi = i - 1;
            }
            return ~lo;

            int GetMedian(int low, int hi2) => low + ((hi2 - low) >> 1);
        }

        private class DelegateCompare<TResult> : IComparer<TResult>
        {
            private Func<TResult, TResult, int> compare;

            public DelegateCompare(Func<TResult, TResult, int> compare)
            {
                this.compare = compare;
            }

            public int Compare(TResult x, TResult y) => this.compare(x, y);
        }
    }
}
