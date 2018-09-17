// Copyright 2018 @asmichi (at github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Asmichi.Utilities.Interop;
using Asmichi.Utilities.Interop.Windows;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.ProcessManagement
{
    /// <summary>
    /// Represents a child process created.
    /// </summary>
    public sealed partial class ChildProcess : IDisposable, IChildProcess
    {
        private readonly SafeProcessHandle _processHandle;
        private readonly WaitHandle _waitHandle;
        private readonly Stream _standardInput;
        private readonly Stream _standardOutput;
        private readonly Stream _standardError;
        private int _exitCode;
        private bool _isDisposed;
        private bool _hasExitCode;

        private ChildProcess(
            SafeProcessHandle processHandle,
            Stream standardInput,
            Stream standardOutput,
            Stream standardError)
        {
            this._processHandle = processHandle;
            this._standardInput = standardInput;
            this._standardOutput = standardOutput;
            this._standardError = standardError;

            // In Windows it is easy to get a WaitHandle from a process handle... What about Linux?
            this._waitHandle = new ChildProcessWaitHandle(HandlePal.ToWaitHandle(processHandle));
        }

        /// <summary>
        /// Releases resources associated to this object.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _processHandle.Dispose();
                _waitHandle.Dispose();
                _standardInput?.Dispose();
                _standardOutput?.Dispose();
                _standardError?.Dispose();

                _isDisposed = true;
            }
        }

        /// <summary>
        /// If created with <see cref="ChildProcessStartInfo.StdInputRedirection"/> set to <see cref="InputRedirection.InputPipe"/>,
        /// a stream assosiated to that pipe.
        /// Otherwise null.
        /// </summary>
        public Stream StandardInput => _standardInput;

        /// <summary>
        /// If created with <see cref="ChildProcessStartInfo.StdOutputRedirection"/> and/or <see cref="ProcessPipelineStartInfo.StdErrorRedirection"/>
        /// set to <see cref="OutputRedirection.OutputPipe"/>, a stream assosiated to that pipe.
        /// Otherwise null.
        /// </summary>
        public Stream StandardOutput => _standardOutput;

        /// <summary>
        /// If created with <see cref="ChildProcessStartInfo.StdOutputRedirection"/> and/or <see cref="ProcessPipelineStartInfo.StdErrorRedirection"/>
        /// set to <see cref="OutputRedirection.ErrorPipe"/>, a stream assosiated to that pipe.
        /// Otherwise null.
        /// </summary>
        public Stream StandardError => _standardError;

        /// <summary>
        /// Waits indefinitely for the process to exit.
        /// </summary>
        public void WaitForExit()
        {
            WaitForExit(Timeout.Infinite);
        }

        /// <summary>
        /// Waits <paramref name="millisecondsTimeout"/> milliseconds for the process to exit.
        /// </summary>
        /// <param name="millisecondsTimeout">The amount of time in milliseconds to wait for the process to exit. <see cref="Timeout.Infinite"/> means infinite amount of time.</param>
        /// <returns>true if the process has exited. Otherwise false</returns>
        public bool WaitForExit(int millisecondsTimeout)
        {
            CheckNotDisposed();

            if (!_waitHandle.WaitOne(millisecondsTimeout))
            {
                return false;
            }

            DangerousRetrieveExitCode();
            return true;
        }

        /// <summary>
        /// Gets if the exit code of the process is 0.
        /// </summary>
        /// <exception cref="InvalidOperationException">The process has not exited yet.</exception>
        public bool IsSuccessful => ExitCode == 0;

        /// <summary>
        /// Gets the exit code of the process.
        /// </summary>
        /// <exception cref="InvalidOperationException">The process has not exited yet.</exception>
        public int ExitCode
        {
            get
            {
                CheckNotDisposed();
                RetrieveExitCode();

                return _exitCode;
            }
        }

        private void CheckNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ChildProcess));
            }
        }

        private void RetrieveExitCode()
        {
            if (!_hasExitCode)
            {
                if (!WaitForExit(0))
                {
                    throw new InvalidOperationException("The process has not exited. Call WaitForExit before accessing ExitCode.");
                }

                DangerousRetrieveExitCode();
            }
        }

        // Pre: The process has exited. Otherwise we will end up getting STILL_ACTIVE (259).
        private void DangerousRetrieveExitCode()
        {
            if (!Kernel32.GetExitCodeProcess(_processHandle, out _exitCode))
            {
                throw new Win32Exception();
            }

            _hasExitCode = true;
        }
    }
}
