using System;

namespace MusicPlayer
{
    internal class DelegateDisposable : IDisposable
    {
        private readonly Action p;

        public DelegateDisposable(Action p) => this.p = p ?? throw new ArgumentNullException(nameof(p));

        public void Dispose() => this.p();
    }
}