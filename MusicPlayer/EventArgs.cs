using System;

namespace MusicPlayer
{
    public class EventArgs<T> : EventArgs
    {
        public EventArgs(T argument)
        {
            this.Argument = argument;
        }

        public T Argument { get; }
    }
}