using System;

namespace MusicPlayer.Core
{
    internal class DelegateDisposable : IDisposable
    {
        private readonly Action p;
        private bool disposedValue;

        public DelegateDisposable(Action p)
        {
            this.p = p;
        }

        protected virtual void Dispose(bool disposing)
        {
        }


        public void Dispose()
        {
            if (!this.disposedValue)
            {
                this.disposedValue = true;
                this.p();
            }
        }
    }
}