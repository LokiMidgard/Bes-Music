using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if NOT_USE_NITO
namespace Nito.AsyncEx
{
    //
    // Summary:
    //     An async-compatible manual-reset event.
    public sealed class AsyncManualResetEvent
    {
        private TaskCompletionSource<object> completionSource;
        /// <summary>
        /// Creates an async-compatible manual-reset event that is initially unset.
        /// </summary>
        public AsyncManualResetEvent() : this(false)
        {

        }

        /// <summary>
        /// Creates an async-compatible manual-reset event.
        /// </summary>
        /// <param name="set">Whether the manual-reset event is initially set or unset.</param>
        public AsyncManualResetEvent(bool set)
        {
            if (!set)
            {
                this.completionSource = new TaskCompletionSource<object>();
            }
        }

        //
        // Summary:
        //     Whether this event is currently set. This member is seldom used; code using this
        //     member has a high possibility of race conditions.
        public bool IsSet => this.completionSource?.Task.IsCompleted ?? true;

        //
        // Summary:
        //     Resets the event. If the event is already reset, this method does nothing.
        public void Reset()
        {
            System.Threading.Interlocked.Exchange(ref this.completionSource, new TaskCompletionSource<object>());
        }
        //
        // Summary:
        //     Sets the event, atomically completing every task returned by Nito.AsyncEx.AsyncManualResetEvent.WaitAsync.
        //     If the event is already set, this method does nothing.
        public void Set()
        {
            this.completionSource?.TrySetResult(null);
        }
        //
        // Summary:
        //     Asynchronously waits for this event to be set.
        public Task WaitAsync()
        {
            return this.completionSource?.Task ?? Task.CompletedTask;
        }
        //
        // Summary:
        //     Asynchronously waits for this event to be set or for the wait to be canceled.
        //
        // Parameters:
        //   cancellationToken:
        //     The cancellation token used to cancel the wait. If this token is already canceled,
        //     this method will first check whether the event is set.
        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            var t = this.WaitAsync();
            if (t.IsCompleted)
                return;
            var cancelTask = new TaskCompletionSource<object>();
            using (cancellationToken.Register(() => cancelTask.SetResult(null)))
            {
                await Task.WhenAny(cancelTask.Task, t);
            }
        }
    }

}
#endif