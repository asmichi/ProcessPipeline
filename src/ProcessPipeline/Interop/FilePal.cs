﻿// Copyright 2018 @asmichi (at github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using Asmichi.Utilities.Interop.Windows;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.Interop
{
    internal static class FilePal
    {
        private const string NullDeviceFileName = "NUL";
        private static int pipeSerialNumber;

        public static SafeFileHandle OpenNullDevice(FileAccess fileAccess)
        {
            return OpenFile(NullDeviceFileName, fileAccess);
        }

        private static SafeFileHandle OpenFile(
            string fileName,
            FileAccess fileAccess)
        {
            var handle = Kernel32.CreateFile(
                fileName,
                ToNativeDesiredAccess(fileAccess),
                Kernel32.FILE_SHARE_READ | Kernel32.FILE_SHARE_WRITE | Kernel32.FILE_SHARE_DELETE,
                IntPtr.Zero,
                Kernel32.OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                handle.Dispose();
                throw new Win32Exception();
            }

            return handle;
        }

        private static uint ToNativeDesiredAccess(FileAccess fileAccess)
        {
            return (((fileAccess & FileAccess.Read) != 0) ? Kernel32.GENERIC_READ : 0)
                & (((fileAccess & FileAccess.Write) != 0) ? Kernel32.GENERIC_WRITE : 0);
        }

        public static (SafeFileHandle readPipe, SafeFileHandle writePipe) CreatePipePair()
        {
            if (!Kernel32.CreatePipe(out var readPipe, out var writePipe, IntPtr.Zero, 0))
            {
                throw new Win32Exception();
            }

            return (readPipe, writePipe);
        }

        /// <summary>
        /// Creates a pipe pair. Overlapped mode is enabled for the server side.
        /// If <paramref name="pipeDirection"/> is <see cref="PipeDirection.In"/>, readPipe is created with Overlapped mode enabled.
        /// If <see cref="PipeDirection.Out"/>, writePipe is created with Overlapped mode enabled.
        /// </summary>
        /// <param name="pipeDirection">Specifies which side is the server side.</param>
        /// <returns>A pipe pair.</returns>
        public static (SafeFileHandle readPipe, SafeFileHandle writePipe) CreatePipePairWithAsyncServerSide(PipeDirection pipeDirection)
        {
            var (serverMode, clientMode) = ToModes(pipeDirection);

            // Make a unique name of a named pipe to create.
            var thisPipeSerialNumber = Interlocked.Increment(ref pipeSerialNumber);
            var pipeName = string.Format(
                CultureInfo.InvariantCulture,
                @"\\.\pipe\Asmichi.ProcessPipeline.7785FB5A-AB05-42B2-BC02-A14769CC463E.{0}.{1}",
                Kernel32.GetCurrentProcessId(),
                thisPipeSerialNumber);

            var serverPipe = Kernel32.CreateNamedPipe(
                pipeName,
                serverMode | Kernel32.FILE_FLAG_OVERLAPPED | Kernel32.FILE_FLAG_FIRST_PIPE_INSTANCE,
                Kernel32.PIPE_TYPE_BYTE | Kernel32.PIPE_READMODE_BYTE | Kernel32.PIPE_WAIT | Kernel32.PIPE_REJECT_REMOTE_CLIENTS,
                1,
                4096,
                4096,
                0,
                IntPtr.Zero);
            if (serverPipe.IsInvalid)
            {
                throw new Win32Exception();
            }

            var clientPipe = Kernel32.CreateFile(
                pipeName,
                clientMode,
                0,
                IntPtr.Zero,
                Kernel32.OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (clientPipe.IsInvalid)
            {
                serverPipe.Dispose();
                throw new Win32Exception();
            }

            if (pipeDirection == PipeDirection.In)
            {
                return (serverPipe, clientPipe);
            }
            else
            {
                return (clientPipe, serverPipe);
            }
        }

        private static (uint serverMode, uint clientMode) ToModes(PipeDirection pipeDirection)
        {
            switch (pipeDirection)
            {
                case PipeDirection.In:
                    return (Kernel32.PIPE_ACCESS_INBOUND, Kernel32.GENERIC_WRITE);
                case PipeDirection.Out:
                    return (Kernel32.PIPE_ACCESS_OUTBOUND, Kernel32.GENERIC_READ);
                case PipeDirection.InOut:
                default:
                    throw new ArgumentException("Must be PipeDirection.In or PipeDirection.Out", nameof(pipeDirection));
            }
        }
    }
}