using MusicPlayer.Controls;
using MusicPlayer.Core;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Windows.Graphics.Printing;
using Windows.UI.Xaml;

namespace MusicPlayer
{
    class NetworkViewmodelAccessor : DependencyObject
    {
        public NetworkViewmodel Instance => NetworkViewmodel.Instance;
    }
    class NetworkViewmodel : DependencyObject
    {
        private static NetworkViewmodel instance;
        internal static NetworkViewmodel Instance
        {
            get
            {
                if (instance is null)
                    instance = new NetworkViewmodel();
                return instance;
            }
        }

        private const int numberOfDownloads = 10;

        private CancellationTokenSource cancellation;

        public ReadOnlyObservableCollection<DownloadItem> Downloading { get; }
        private readonly ObservableCollection<DownloadItem> downloading;
        public ReadonlyObservableQueue<DownloadItem> WaitForDownloads { get; }
        private readonly ObservableQueue<DownloadItem> waitForDownloads;
        public ReadOnlyObservableCollection<DownloadItem> AllQueued { get; }
        private readonly ObservableCollection<DownloadItem> allQueued;

        public ICommand CancelAllCommand { get; }

        private NetworkViewmodel()
        {
            this.downloading = new ObservableCollection<DownloadItem>();
            this.Downloading = new ReadOnlyObservableCollection<DownloadItem>(this.downloading);

            this.allQueued = new ObservableCollection<DownloadItem>();
            this.AllQueued = new ReadOnlyObservableCollection<DownloadItem>(this.allQueued);

            this.waitForDownloads = new ObservableQueue<DownloadItem>();
            this.WaitForDownloads = new ReadonlyObservableQueue<DownloadItem>(this.waitForDownloads);
            this.cancellation = new CancellationTokenSource();

            this.CancelAllCommand = new DelegateCommand(() =>
            {
                this.CancelAllDownloads();
            }, () => this.downloading.Count > 0 || this.waitForDownloads.Count > 0);

            this.downloading.CollectionChanged += (sender, e) =>
            {
                ((DelegateCommand)this.CancelAllCommand).FireCanExecuteChanged();
            };

            this.waitForDownloads.CollectionChanged += (sender, e) =>
            {
                ((DelegateCommand)this.CancelAllCommand).FireCanExecuteChanged();
            };

            this.DownloadLoop();

        }


        private readonly SemaphoreSlim addSemaphore = new SemaphoreSlim(0);
        private readonly SemaphoreSlim concurrentSemaphore = new SemaphoreSlim(numberOfDownloads);


        public void CancelAllDownloads()
        {
            this.cancellation.Cancel();
            Interlocked.Exchange(ref this.cancellation, new CancellationTokenSource());
        }

        private async void DownloadLoop()
        {
            var displayRequest = new Windows.System.Display.DisplayRequest();

            while (true)
            {
                await this.addSemaphore.WaitAsync();
                var current = this.waitForDownloads.Dequeue();
                await this.concurrentSemaphore.WaitAsync();

                if (this.downloading.Count == 0)
                    displayRequest.RequestActive();

                this.downloading.Add(current);
                _ = Task.Run(() =>
                   current.StartDownload().ContinueWith(async t =>
                  {
                      await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                      {
                          this.concurrentSemaphore.Release();
                          this.downloading.Remove(current);
                          this.allQueued.Remove(current);

                          if (current.Finished.IsFaulted)
                          {
                              App.Current.NotifyError(current, current.Finished.Exception);
                          }

                          if (this.downloading.Count == 0)
                              displayRequest.RequestRelease();
                      });

                  })); ;
            }
        }

        public async Task AddDownload(Song songToDOwnload, DownloadDelegate downloadMethod)
        {
            if (!this.Dispatcher.HasThreadAccess)
            {
                var completionSource = new TaskCompletionSource<object>();
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await this.AddDownload(songToDOwnload, downloadMethod);
                    completionSource.SetResult(null);
                });
                await completionSource.Task;
                return;
            }

            var item = new DownloadItem(songToDOwnload, downloadMethod, this.cancellation.Token);
            this.waitForDownloads.Enqueue(item);
            this.allQueued.Add(item);
            this.addSemaphore.Release();
            await item.Finished;
        }

        public async Task AddDownload(string title, DownloadDelegate downloadMethod)
        {
            if (!this.Dispatcher.HasThreadAccess)
            {
                var completionSource = new TaskCompletionSource<object>();
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    try
                    {
                        await this.AddDownload(title, downloadMethod);
                        completionSource.SetResult(null);
                    }
                    catch (Exception e)
                    {
                        completionSource.SetException(e);
                    }
                });
                await completionSource.Task;
                return;
            }

            var item = new DownloadItem(title, downloadMethod, this.cancellation.Token);
            this.waitForDownloads.Enqueue(item);
            this.allQueued.Add(item);
            this.addSemaphore.Release();
            await item.Finished;
        }


    }





    public delegate Task DownloadDelegate(IProgress<(string state, double percentage)> progress, CancellationToken cancel);

    public class DownloadItem : DependencyObject
    {
        private readonly DownloadDelegate downloadFunction;
        private readonly CancellationToken globalCancle;
        private CancellationTokenSource localCancle;

        public Song Song { get; }
        public string Title { get; }

        public string State
        {
            get { return (string)this.GetValue(StateProperty); }
            set { this.SetValue(StateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for State.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(string), typeof(DownloadItem), new PropertyMetadata(string.Empty));



        public bool IsDownloading
        {
            get { return (bool)this.GetValue(IsDownloadingProperty); }
            private set { this.SetValue(IsDownloadingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsDownloading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsDownloadingProperty =
            DependencyProperty.Register("IsDownloading", typeof(bool), typeof(DownloadItem), new PropertyMetadata(false));

        private readonly TaskCompletionSource<object> taskCompletionSource;
        public Task Finished => this.taskCompletionSource.Task;

        public DownloadItem(Song songToDownload, DownloadDelegate downloadFunction, CancellationToken cancel) : this(downloadFunction, cancel)
        {
            this.Song = songToDownload;
        }
        public DownloadItem(string title, DownloadDelegate downloadFunction, CancellationToken cancel) : this(downloadFunction, cancel)
        {
            this.Title = title;
        }

        private DownloadItem(DownloadDelegate downloadFunction, CancellationToken cancel)
        {
            this.downloadFunction = downloadFunction;
            this.globalCancle = cancel;
            this.taskCompletionSource = new TaskCompletionSource<object>();
        }

        public async Task CancelDownload()
        {

            if (!this.Dispatcher.HasThreadAccess)
            {
                var completionSource = new TaskCompletionSource<object>();
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await this.CancelDownload();
                    completionSource.SetResult(null);
                });
                await completionSource.Task;
                return;
            }
            this.localCancle?.Cancel();
        }

        public async Task StartDownload()
        {

            if (!this.Dispatcher.HasThreadAccess)
            {
                var completionSource = new TaskCompletionSource<object>();
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    try
                    {
                        await this.StartDownload();
                        completionSource.SetResult(null);
                    }
                    catch (Microsoft.Graph.ServiceException)
                    {
                        completionSource.SetResult(null);

                    }
                    catch (OperationCanceledException)
                    {
                        completionSource.SetResult(null);
                    }
                    catch (Exception e)
                    {
                        completionSource.SetException(e);
                    }
                });
                await completionSource.Task;
                return;
            }

            if (this.IsDownloading)
                return;

            this.IsDownloading = true;
            try
            {



                using (this.localCancle = new CancellationTokenSource())
                using (var actualCancel = CancellationTokenSource.CreateLinkedTokenSource(this.localCancle.Token, this.globalCancle))
                {
                    try
                    {
                        if (!actualCancel.Token.IsCancellationRequested)
                            await this.downloadFunction(new Progress<(string state, double percentage)>(progress =>
                            {
                                this.Downloaded = progress.percentage;
                                this.State = progress.state ?? string.Empty;
                            }), actualCancel.Token);
                    }
                    catch (Exception e)
                    {
                        // if the task is canceld the error is propably based on canceling, even if it is not an OperationCanceldException.
                        if (!actualCancel.Token.IsCancellationRequested)
                            throw; 
                    }
                }

            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                this.taskCompletionSource.SetException(e);
            }
            this.IsDownloading = false;
            this.taskCompletionSource.TrySetResult(null);


        }


        public double Downloaded
        {
            get { return (double)this.GetValue(DownloadedProperty); }
            set { this.SetValue(DownloadedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Downloaded.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DownloadedProperty =
            DependencyProperty.Register("Downloaded", typeof(double), typeof(DownloadItem), new PropertyMetadata(0.0));
    }

}
