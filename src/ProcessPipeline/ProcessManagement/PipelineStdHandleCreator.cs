// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Asmichi.Utilities.Interop;
using Asmichi.Utilities.Interop.Windows;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.ProcessManagement
{
    /// <summary>
    /// Provides in/out/err handles of a pipeline.
    /// </summary>
    internal sealed class PipelineStdHandleCreator : IDisposable
    {
        private readonly SafeFileHandle _inputReadPipe;
        private readonly SafeFileHandle _outputWritePipe;
        private readonly SafeFileHandle _errorWritePipe;
        private SafePseudoConsoleHandle _pseudoConsoleHandle;
        private List<IDisposable> _objectsToDispose;
        private bool _isDisposed;

        public PipelineStdHandleCreator(
            InputRedirection stdInputRedirection,
            OutputRedirection stdOutputRedirection,
            OutputRedirection stdErrorRedirection,
            string stdInputFile,
            string stdOutputFile,
            string stdErrorFile,
            SafeFileHandle stdInputHandle,
            SafeFileHandle stdOutputHandle,
            SafeFileHandle stdErrorHandle)
        {
            if (stdInputRedirection == InputRedirection.Handle && stdInputHandle == null)
            {
                throw new ArgumentNullException(nameof(ChildProcessStartInfo.StdInputHandle));
            }
            if (stdInputRedirection == InputRedirection.File && stdInputFile == null)
            {
                throw new ArgumentNullException(nameof(ChildProcessStartInfo.StdInputFile));
            }
            if (stdOutputRedirection == OutputRedirection.Handle && stdOutputHandle == null)
            {
                throw new ArgumentNullException(nameof(ChildProcessStartInfo.StdOutputHandle));
            }
            if (IsFileRedirection(stdOutputRedirection) && stdOutputFile == null)
            {
                throw new ArgumentNullException(nameof(ChildProcessStartInfo.StdOutputFile));
            }
            if (stdErrorRedirection == OutputRedirection.Handle && stdErrorHandle == null)
            {
                throw new ArgumentNullException(nameof(ChildProcessStartInfo.StdErrorHandle));
            }
            if (IsFileRedirection(stdErrorRedirection) && stdErrorFile == null)
            {
                throw new ArgumentNullException(nameof(ChildProcessStartInfo.StdErrorFile));
            }

            bool redirectingToSameFile = IsFileRedirection(stdOutputRedirection) && IsFileRedirection(stdErrorRedirection) && stdOutputFile == stdErrorFile;
            if (redirectingToSameFile && stdErrorRedirection != stdOutputRedirection)
            {
                throw new ArgumentException(
                    "StdOutputRedirection and StdErrorRedirection must be the same value when both stdout and stderr redirect to the same file.",
                    nameof(ChildProcessStartInfo.StdErrorRedirection));
            }

            var inputWritePipe = default(SafeFileHandle);
            var outputReadPipe = default(SafeFileHandle);
            var errorReadPipe = default(SafeFileHandle);
            var pseudoConsoleInputWritePipe = default(SafeFileHandle);
            var pseudoConsoleInputReadPipeForChild = default(SafeFileHandle);
            var pseudoConsoleOutputWritePipeForChild = default(SafeFileHandle);
            var pseudoConsoleOutputReadPipe = default(SafeFileHandle);

            try
            {
                if (stdInputRedirection == InputRedirection.InputPipe)
                {
                    (_inputReadPipe, inputWritePipe) = FilePal.CreatePipePairWithAsyncServerSide(System.IO.Pipes.PipeDirection.Out);
                    this.InputStream = new FileStream(inputWritePipe, FileAccess.Write, 4096, isAsync: true);
                    inputWritePipe = null;
                }

                if (stdOutputRedirection == OutputRedirection.OutputPipe
                    || stdErrorRedirection == OutputRedirection.OutputPipe)
                {
                    (outputReadPipe, _outputWritePipe) = FilePal.CreatePipePairWithAsyncServerSide(System.IO.Pipes.PipeDirection.In);
                    this.OutputStream = new FileStream(outputReadPipe, FileAccess.Read, 4096, isAsync: true);
                    outputReadPipe = null;
                }

                if (stdOutputRedirection == OutputRedirection.ErrorPipe
                    || stdErrorRedirection == OutputRedirection.ErrorPipe)
                {
                    (errorReadPipe, _errorWritePipe) = FilePal.CreatePipePairWithAsyncServerSide(System.IO.Pipes.PipeDirection.In);
                    this.ErrorStream = new FileStream(errorReadPipe, FileAccess.Read, 4096, isAsync: true);
                    errorReadPipe = null;
                }

                (pseudoConsoleInputReadPipeForChild, pseudoConsoleInputWritePipe) = FilePal.CreatePipePairWithAsyncServerSide(System.IO.Pipes.PipeDirection.Out);
                this.PseudoConsoleInputStream = new FileStream(pseudoConsoleInputWritePipe, FileAccess.Write, 4096, isAsync: true);
                pseudoConsoleInputWritePipe = null;

                (pseudoConsoleOutputReadPipe, pseudoConsoleOutputWritePipeForChild) = FilePal.CreatePipePairWithAsyncServerSide(System.IO.Pipes.PipeDirection.In);
                this.PseudoConsoleOutputStream = new FileStream(pseudoConsoleOutputReadPipe, FileAccess.Read, 4096, isAsync: true);
                pseudoConsoleOutputReadPipe = null;

                var hr = Kernel32.CreatePseudoConsole(
                    new Kernel32.COORD(80, 25),
                    pseudoConsoleInputReadPipeForChild,
                    pseudoConsoleOutputWritePipeForChild,
                    0,
                    out _pseudoConsoleHandle);
                Marshal.ThrowExceptionForHR(hr);

                this.PipelineStdIn = ChooseInput(
                    stdInputRedirection,
                    stdInputFile,
                    stdInputHandle,
                    _inputReadPipe);

                this.PipelineStdOut = ChooseOutput(
                    stdOutputRedirection,
                    stdOutputFile,
                    stdOutputHandle,
                    _outputWritePipe,
                    _errorWritePipe);

                if (redirectingToSameFile)
                {
                    this.PipelineStdErr = this.PipelineStdOut;
                }
                else
                {
                    this.PipelineStdErr = ChooseOutput(
                        stdErrorRedirection,
                        stdErrorFile,
                        stdErrorHandle,
                        _outputWritePipe,
                        _errorWritePipe);
                }
            }
            catch
            {
                Dispose();
                throw;
            }
            finally
            {
                inputWritePipe?.Dispose();
                outputReadPipe?.Dispose();
                errorReadPipe?.Dispose();
                pseudoConsoleInputWritePipe?.Dispose();
                pseudoConsoleOutputReadPipe?.Dispose();
                // Can be closed after passed to CreatePseudoConsole.
                pseudoConsoleInputReadPipeForChild?.Dispose();
                pseudoConsoleOutputWritePipeForChild?.Dispose();
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_objectsToDispose != null)
                {
                    foreach (var h in _objectsToDispose)
                    {
                        h.Dispose();
                    }
                }

                _inputReadPipe?.Dispose();
                _outputWritePipe?.Dispose();
                _errorWritePipe?.Dispose();
                _pseudoConsoleHandle?.Dispose();

                InputStream?.Dispose();
                OutputStream?.Dispose();
                ErrorStream?.Dispose();
                _isDisposed = true;
            }
        }

        /// <summary>
        /// A handle that should be used as the stdin handle of the pipeline.
        /// </summary>
        public SafeFileHandle PipelineStdIn { get; }

        /// <summary>
        /// A handle that should be used as the stdout handle of the pipeline.
        /// </summary>
        public SafeFileHandle PipelineStdOut { get; }

        /// <summary>
        /// A handle that should be used as the stderr handle of the pipeline.
        /// </summary>
        public SafeFileHandle PipelineStdErr { get; }

        public SafePseudoConsoleHandle PseudoConsoleHandle => _pseudoConsoleHandle;

        /// <summary>
        /// An asynchronous <see cref="Stream"/> that writes to the pipeline.
        /// </summary>
        public Stream InputStream { get; private set; }

        /// <summary>
        /// An asynchronous <see cref="Stream"/> that reads from the standard output of the pipeline.
        /// </summary>
        public Stream OutputStream { get; private set; }

        /// <summary>
        /// An asynchronous <see cref="Stream"/> that reads from the standard error of the pipeline.
        /// </summary>
        public Stream ErrorStream { get; private set; }

        public Stream PseudoConsoleOutputStream { get; private set; }

        public Stream PseudoConsoleInputStream { get; private set; }

        /// <summary>
        /// Detaches <see cref="InputStream"/>, <see cref="OutputStream"/> and <see cref="ErrorStream"/> so that they will no be disposed by this instance.
        /// Must be called in order to expose the streams to the caller.
        /// </summary>
        public void DetachHandles()
        {
            InputStream = null;
            OutputStream = null;
            ErrorStream = null;
            _pseudoConsoleHandle = null;
            PseudoConsoleOutputStream = null;
            PseudoConsoleInputStream = null;
        }

        private SafeFileHandle ChooseInput(
            InputRedirection redirection,
            string fileName,
            SafeFileHandle handle,
            SafeFileHandle inputPipe)
        {
            switch (redirection)
            {
                case InputRedirection.ParentInput:
                    return ConsolePal.GetStdInputHandleForChild() ?? OpenNullDevice(FileAccess.Read);
                case InputRedirection.InputPipe:
                    return inputPipe;
                case InputRedirection.File:
                    return OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                case InputRedirection.Handle:
                    return handle;
                case InputRedirection.NullDevice:
                    return OpenNullDevice(FileAccess.Read);
                default:
                    throw new ArgumentOutOfRangeException(nameof(redirection), "Not a valid value for " + nameof(InputRedirection) + ".");
            }
        }

        private SafeFileHandle ChooseOutput(
            OutputRedirection redirection,
            string fileName,
            SafeFileHandle handle,
            SafeFileHandle outputPipe,
            SafeFileHandle errorPipe)
        {
            switch (redirection)
            {
                case OutputRedirection.ParentOutput:
                    return ConsolePal.GetStdOutputHandleForChild() ?? OpenNullDevice(FileAccess.Write);
                case OutputRedirection.ParentError:
                    return ConsolePal.GetStdErrorHandleForChild() ?? OpenNullDevice(FileAccess.Write);
                case OutputRedirection.OutputPipe:
                    return outputPipe;
                case OutputRedirection.ErrorPipe:
                    return errorPipe;
                case OutputRedirection.File:
                    return OpenFile(fileName, FileMode.Create, FileAccess.Write, FileShare.Read);
                case OutputRedirection.AppendToFile:
                    return OpenFile(fileName, FileMode.Append, FileAccess.Write, FileShare.Read);
                case OutputRedirection.Handle:
                    return handle;
                case OutputRedirection.NullDevice:
                    return FilePal.OpenNullDevice(FileAccess.Write);
                default:
                    throw new ArgumentOutOfRangeException(nameof(redirection), "Not a valid value for " + nameof(OutputRedirection) + ".");
            }
        }

        private SafeFileHandle OpenFile(
            string fileName,
            FileMode mode,
            FileAccess access,
            FileShare share)
        {
            EnsureObjectsToDispose();

            var fs = new FileStream(fileName, mode, access, share);
            _objectsToDispose.Add(fs);
            return fs.SafeFileHandle;
        }

        private SafeFileHandle OpenNullDevice(FileAccess access)
        {
            EnsureObjectsToDispose();

            var handle = FilePal.OpenNullDevice(access);
            _objectsToDispose.Add(handle);
            return handle;
        }

        private void EnsureObjectsToDispose()
        {
            if (_objectsToDispose == null)
            {
                _objectsToDispose = new List<IDisposable>(5);
            }
        }

        private static bool IsFileRedirection(OutputRedirection redirection) =>
            redirection == OutputRedirection.File || redirection == OutputRedirection.AppendToFile;
    }
}
