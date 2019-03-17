using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace MusicPlayer
{
    public class FileQuery : IObservable<(StorageFile, FileQuery.Defer)>
    {
        public class Defer
        {
            private readonly TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();

            public Task Task => this.taskCompletionSource.Task;

            public Defer(CancellationToken token)
            {
                this.CancelToken = token;
            }

            public CancellationToken CancelToken { get; }

            public void Complete() => this.taskCompletionSource.TrySetResult(null);

            internal void Complete(Exception e) => this.taskCompletionSource.SetException(e);

            public System.Runtime.CompilerServices.TaskAwaiter GetAwaiter()
            {
                return (this.taskCompletionSource.Task as Task).GetAwaiter();
            }



        }
        private readonly StorageFolder root;

        public FileQuery(StorageFolder root)
        {
            this.root = root;


        }

        private class Observer : IDisposable
        {
            private StorageFolder root;
            private IObserver<(StorageFile, Defer)> observer;
            private readonly CancellationTokenSource cancel;
            private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

            public Observer(StorageFolder root, IObserver<(StorageFile, Defer)> observer)
            {
                this.root = root;
                this.observer = observer;
                this.cancel = new CancellationTokenSource();
                this.Start();
            }

            private async void Start()
            {
                try
                {
                    await this.GetFilesAsync(this.root);

                }
                catch (Exception e)
                {
                    if (!this.cancel.IsCancellationRequested)
                        this.observer.OnError(e);
                }
                if (!this.cancel.IsCancellationRequested)
                    this.observer.OnCompleted();
            }

            private async Task GetFilesAsync(StorageFolder musicLibrary)
            {
                try
                {
                    await this.semaphore.WaitAsync(this.cancel.Token);
                    if (this.cancel.IsCancellationRequested)
                        return;
                    var files = await musicLibrary.GetFilesAsync();
                    foreach (var file in files)
                    {
                        var defer = new Defer(this.cancel.Token);
                        this.observer.OnNext((file, defer));
                        await defer.Task;
                        if (this.cancel.IsCancellationRequested)
                            return;
                    }
                }
                finally
                {
                    this.semaphore.Release();
                }
                var folders = await musicLibrary.GetFoldersAsync();
                if (this.cancel.IsCancellationRequested)
                    return;
                await Task.WhenAll(folders.Select(x => this.GetFilesAsync(x)));
            }

            #region IDisposable Support
            private bool disposedValue = false; // Dient zur Erkennung redundanter Aufrufe.

            protected virtual void Dispose(bool disposing)
            {
                if (!this.disposedValue)
                {
                    if (disposing)
                    {
                        this.cancel.Cancel();
                        this.cancel.Dispose();
                        // TODO: verwalteten Zustand (verwaltete Objekte) entsorgen.
                    }

                    // TODO: nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer weiter unten überschreiben.
                    // TODO: große Felder auf Null setzen.

                    this.disposedValue = true;
                }
            }

            // TODO: Finalizer nur überschreiben, wenn Dispose(bool disposing) weiter oben Code für die Freigabe nicht verwalteter Ressourcen enthält.
            // ~Observer() {
            //   // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(bool disposing) weiter oben ein.
            //   Dispose(false);
            // }

            // Dieser Code wird hinzugefügt, um das Dispose-Muster richtig zu implementieren.
            public void Dispose()
            {
                // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(bool disposing) weiter oben ein.
                this.Dispose(true);
                // TODO: Auskommentierung der folgenden Zeile aufheben, wenn der Finalizer weiter oben überschrieben wird.
                // GC.SuppressFinalize(this);
            }
            #endregion

        }


        public IDisposable Subscribe(IObserver<(StorageFile, Defer)> observer)
        {
            var n = new Observer(this.root, observer);
            return n;
        }
    }


}

