// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Asmichi.Utilities.ProcessManagement
{
    // Creates an asynchronous operation that performs WaitHandle.WaitOne and cleans itself up.
    internal sealed class WaitAsyncOperation
    {
        private static readonly WaitOrTimerCallback CachedWaitForExitCompletedDelegate = WaitForExitCompleted;
        private static readonly Action<object> CachedWaitForExitCanceledDelegate = WaitForExitCanceled;

        private TaskCompletionSource<bool> _completionSource;
        private RegisteredWaitHandle _waitRegistration;
        private CancellationTokenRegistration _cancellationTokenRegistration;

        public Task<bool> StartAsync(WaitHandle waitHandle, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            lock (this)
            {
                _completionSource = new TaskCompletionSource<bool>();

                _waitRegistration = ThreadPool.RegisterWaitForSingleObject(
                    waitHandle, CachedWaitForExitCompletedDelegate, this, millisecondsTimeout, executeOnlyOnce: true);

                if (cancellationToken.CanBeCanceled)
                {
                    _cancellationTokenRegistration = cancellationToken.Register(
                        CachedWaitForExitCanceledDelegate, this, useSynchronizationContext: false);
                }
            }

            return _completionSource.Task;
        }

        private static void WaitForExitCanceled(object state)
        {
            // Ensure that all writes made by Register are visible.
            lock (state)
            {
            }

            var self = (WaitAsyncOperation)state;

            self._completionSource.TrySetCanceled();
            self.ReleaseResources();
        }

        private static void WaitForExitCompleted(object state, bool timedOut)
        {
            // Ensure that all writes made by Register are visible.
            lock (state)
            {
            }

            var self = (WaitAsyncOperation)state;

            // Not calling parent.DangerousRetrieveExitCode here. It would require some memory barrier.
            self._completionSource.TrySetResult(!timedOut);
            self.ReleaseResources();
        }

        private void ReleaseResources()
        {
            if (_cancellationTokenRegistration != default)
            {
                lock (this)
                {
                    _cancellationTokenRegistration.Dispose();
                }
            }

            // RegisteredWaitHandle is thread-safe.
            _waitRegistration.Unregister(null);
        }
    }
}
