using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Linq;

namespace MusicPlayer
{
    public class ObservableStack<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly Stack<T> stack;


        public ObservableStack()
        {
            this.stack = new Stack<T>();
        }

        public ObservableStack(IEnumerable<T> collection)
        {
            this.stack = new Stack<T>(collection);
        }

        public int Count => ((IReadOnlyCollection<T>)this.stack).Count;

        public bool IsSynchronized => ((ICollection)this.stack).IsSynchronized;

        public object SyncRoot => ((ICollection)this.stack).SyncRoot;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;


        public void Clear()
        {
            this.stack.Clear();
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
        }

        public T Pop()
        {
            var item = this.stack.Pop();
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, 0));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
            return item;
        }
        public T Peek()
        {
            return this.stack.Peek();
        }

        public void Push(T item)
        {
            this.stack.Push(item);
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, 0));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)this.stack).CopyTo(array, index);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)this.stack).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.stack).GetEnumerator();
        }
    }
    public class ReadonlyObservableQueue<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly ObservableQueue<T> queue;

        public ReadonlyObservableQueue(ObservableQueue<T> queue)
        {
            this.queue = queue;
        }

        public int Count => ((IReadOnlyCollection<T>)this.queue).Count;

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                ((INotifyCollectionChanged)this.queue).CollectionChanged += value;
            }

            remove
            {
                ((INotifyCollectionChanged)this.queue).CollectionChanged -= value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                ((INotifyPropertyChanged)this.queue).PropertyChanged += value;
            }

            remove
            {
                ((INotifyPropertyChanged)this.queue).PropertyChanged -= value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)this.queue).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.queue).GetEnumerator();
        }
    }
    public class ObservableQueue<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly Queue<T> stack;


        public ObservableQueue()
        {
            this.stack = new Queue<T>();
        }

        public ObservableQueue(IEnumerable<T> collection)
        {
            this.stack = new Queue<T>(collection);
        }

        public int Count => ((IReadOnlyCollection<T>)this.stack).Count;

        public bool IsSynchronized => ((ICollection)this.stack).IsSynchronized;

        public object SyncRoot => ((ICollection)this.stack).SyncRoot;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;


        public void Clear()
        {
            this.stack.Clear();
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
        }

        public T Dequeue()
        {
            var item = this.stack.Dequeue();
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, this.stack.Count));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
            return item;
        }
        public T Peek()
        {
            return this.stack.Peek();
        }

        public void Enqueue(T item)
        {
            this.stack.Enqueue(item);
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, this.stack.Count - 1));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)this.stack).CopyTo(array, index);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)this.stack).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.stack).GetEnumerator();
        }
    }

}
