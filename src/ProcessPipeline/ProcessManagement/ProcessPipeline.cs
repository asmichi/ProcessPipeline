// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Asmichi.Utilities.Interop.Windows;
using Asmichi.Utilities.PlatformAbstraction;
using Asmichi.Utilities.Utilities;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.ProcessManagement
{
    /// <summary>
    /// Represents a pipeline of processes created.
    /// Static members are thread-safe.
    /// All instance members are not thread-safe and must not be called simultaneously by multiple threads.
    /// </summary>
    public sealed partial class ProcessPipeline : IDisposable, IChildProcess
    {
        private static readonly Func<Task, object, bool> CachedAreAllWaitSuccessfulDelegate = AreAllWaitSuccessful;

        private readonly ProcessEntry[] _entries;
        private readonly WaitHandle[] _waitHandles;
        private readonly Stream _standardInput;
        private readonly Stream _standardOutput;
        private readonly Stream _standardError;
        private bool _isDisposed;
        private bool _hasExitCodes;

        private ProcessPipeline(
            ProcessEntry[] entries,
            Stream standardInput,
            Stream standardOutput,
            Stream standardError)
        {
            this._entries = entries;
            this._standardInput = standardInput;
            this._standardOutput = standardOutput;
            this._standardError = standardError;

            try
            {
                int validProcessCount = 0;
                for (int i = 0; i < _entries.Length; i++)
                {
                    if (_entries[i].ProcessHandle != null)
                    {
                        validProcessCount++;
                    }
                }

                if (validProcessCount == 0)
                {
                    this._waitHandles = Array.Empty<WaitHandle>();
                    this._hasExitCodes = true;
                }
                else
                {
                    this._waitHandles = new WaitHandle[validProcessCount];
                    int waitHandleIndex = 0;
                    for (int i = 0; i < _entries.Length; i++)
                    {
                        if (_entries[i].ProcessHandle != null)
                        {
                            _waitHandles[waitHandleIndex++] = new ChildProcessWaitHandle(HandlePal.ToWaitHandle(_entries[i].ProcessHandle));
                        }
                    }
                }
            }
            catch
            {
                foreach (var h in _waitHandles)
                {
                    h?.Dispose();
                }
                throw;
            }
        }

        /// <summary>
        /// Releases resources associated to this object.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                foreach (var e in _entries)
                {
                    e.ProcessHandle?.Dispose();
                }
                foreach (var h in _waitHandles)
                {
                    h?.Dispose();
                }

                _standardInput?.Dispose();
                _standardOutput?.Dispose();
                _standardError?.Dispose();
                _isDisposed = true;
            }
        }

        /// <summary>
        /// If created with <see cref="ProcessPipelineStartInfo.StdInputRedirection"/> set to <see cref="InputRedirection.InputPipe"/>,
        /// a stream assosiated to that pipe.
        /// Otherwise null.
        /// </summary>
        public Stream StandardInput => _standardInput;

        /// <summary>
        /// If created with <see cref="ProcessPipelineStartInfo.StdOutputRedirection"/> and/or <see cref="ProcessPipelineStartInfo.StdErrorRedirection"/>
        /// set to <see cref="OutputRedirection.OutputPipe"/>, a stream assosiated to that pipe.
        /// Otherwise null.
        /// </summary>
        public Stream StandardOutput => _standardOutput;

        /// <summary>
        /// If created with <see cref="ProcessPipelineStartInfo.StdOutputRedirection"/> and/or <see cref="ProcessPipelineStartInfo.StdErrorRedirection"/>
        /// set to <see cref="OutputRedirection.ErrorPipe"/>, a stream assosiated to that pipe.
        /// Otherwise null.
        /// </summary>
        public Stream StandardError => _standardError;

        /// <summary>
        /// Waits indefinitely for all the processes in the pipeline to exit.
        /// </summary>
        public void WaitForExit() => WaitForExit(Timeout.Infinite);

        /// <summary>
        /// Waits <paramref name="millisecondsTimeout"/> milliseconds for all the processes in the pipeline to exit.
        /// </summary>
        /// <param name="millisecondsTimeout">The amount of time in milliseconds to wait for the processes to exit. <see cref="Timeout.Infinite"/> means infinite amount of time.</param>
        /// <returns>true if the processes have exited. Otherwise false.</returns>
        public bool WaitForExit(int millisecondsTimeout)
        {
            ArgumentValidationUtil.CheckTimeOutRange(millisecondsTimeout);
            CheckNotDisposed();

            if (_hasExitCodes)
            {
                return true;
            }

            Debug.Assert(_waitHandles.Length != 0);

            if (!WaitHandle.WaitAll(_waitHandles, millisecondsTimeout))
            {
                return false;
            }

            DangerousRetrieveExitCodes();
            return true;
        }

        /// <summary>
        /// Asynchronously waits indefinitely for all the processes in the pipeline to exit.
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the wait operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous wait operation.</returns>
        public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
            WaitForExitAsync(Timeout.Infinite, cancellationToken);

        /// <summary>
        /// Asynchronously waits <paramref name="millisecondsTimeout"/> milliseconds for all the processes in the pipeline to exit.
        /// </summary>
        /// <param name="millisecondsTimeout">The amount of time in milliseconds to wait for the processes to exit. <see cref="Timeout.Infinite"/> means infinite amount of time.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the wait operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous wait operation. true if the processes have exited. Otherwise false.</returns>
        public Task<bool> WaitForExitAsync(int millisecondsTimeout, CancellationToken cancellationToken = default)
        {
            ArgumentValidationUtil.CheckTimeOutRange(millisecondsTimeout);
            CheckNotDisposed();

            if (_hasExitCodes)
            {
                return CompletedBoolTask.True;
            }

            Debug.Assert(_waitHandles.Length != 0);

            // Collect unsignaled handles.
            int unsignaledHandleCount = 0;
            Span<bool> signaled = stackalloc bool[_waitHandles.Length];
            for (int i = 0; i < _waitHandles.Length; i++)
            {
                if (_waitHandles[i].WaitOne(0))
                {
                    signaled[i] = true;
                }
                else
                {
                    signaled[i] = false;
                    unsignaledHandleCount++;
                }
            }

            // Synchronous path: all the process have already exited.
            if (unsignaledHandleCount == 0)
            {
                DangerousRetrieveExitCodes();
                return CompletedBoolTask.True;
            }

            // Synchronous path: already canceled.
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<bool>(cancellationToken);
            }

            // Start asynchronous wait operations.
            var waitTasks = new Task<bool>[unsignaledHandleCount];
            var waitTaskIndex = 0;

            for (int i = 0; i < _waitHandles.Length; i++)
            {
                if (!signaled[i])
                {
                    waitTasks[waitTaskIndex++] = new WaitAsyncOperation().StartAsync(_waitHandles[i], millisecondsTimeout, cancellationToken);
                }
            }

            return Task.WhenAll(waitTasks).ContinueWith(CachedAreAllWaitSuccessfulDelegate, waitTasks, cancellationToken);
        }

        private static bool AreAllWaitSuccessful(Task prevTask, object state)
        {
            var waitTasks = (Task<bool>[])state;

            for (int i = 0; i < waitTasks.Length; i++)
            {
                if (!waitTasks[i].Result)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets if the exit codes of the processes are all 0.
        /// </summary>
        /// <exception cref="InvalidOperationException">One of the processes has not exited yet.</exception>
        public bool IsSuccessful => ExitCode == 0;

        /// <summary>
        /// Gets the exit code with the largest unsigned value among the processes.
        /// </summary>
        /// <exception cref="InvalidOperationException">One of the processes has not exited yet.</exception>
        public int ExitCode
        {
            get
            {
                CheckNotDisposed();
                RetrieveExitCodes();

                int exitCode = 0;
                foreach (var e in _entries)
                {
                    if ((uint)exitCode < (uint)e.ExitCode)
                    {
                        exitCode = e.ExitCode;
                    }
                }

                return exitCode;
            }
        }

        /// <summary>
        /// Returns the exit codes of the processes. An entry of null indicates that creation of the corresponding process failed.
        /// </summary>
        /// <returns>Exit codes of the processes.</returns>
        public int?[] GetExitCodes()
        {
            CheckNotDisposed();
            RetrieveExitCodes();

            var ret = new int?[_entries.Length];

            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].ProcessHandle != null)
                {
                    ret[i] = _entries[i].ExitCode;
                }
            }

            return ret;
        }

        private void CheckNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ChildProcess));
            }
        }

        private void RetrieveExitCodes()
        {
            if (!_hasExitCodes)
            {
                if (!WaitForExit(0))
                {
                    throw new InvalidOperationException("One of the processes has not exited. Call WaitForExit before accessing ExitCode.");
                }

                DangerousRetrieveExitCodes();
            }
        }

        // Pre: The processes have exited. Otherwise we will end up getting STILL_ACTIVE (259).
        private void DangerousRetrieveExitCodes()
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                ref var e = ref _entries[i];

                if (e.ProcessHandle != null)
                {
                    if (!Kernel32.GetExitCodeProcess(e.ProcessHandle, out int exitCode))
                    {
                        throw new Win32Exception();
                    }

                    e.ExitCode = exitCode;
                }
            }

            _hasExitCodes = true;
        }

        private struct ProcessEntry
        {
            // null if process creation failed.
            public SafeProcessHandle ProcessHandle;
            public int ExitCode;
        }
    }
}
