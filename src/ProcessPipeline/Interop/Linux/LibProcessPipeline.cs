// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.Interop.Linux
{
    internal static class LibProcessPipeline
    {
        private const string DllName = "libAsmichiProcessPipeline.so";

        [DllImport(DllName, EntryPoint = "CreateFileW", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern SafeFileHandle CreateFile(
            [In] string lpFileName,
            [In] uint dwDesiredAccess,
            [In] uint dwShareMode,
            [In] IntPtr lpSecurityAttributes,
            [In] int dwCreationDisposition,
            [In] int dwFlagsAndAttributes,
            [In] IntPtr hTemplateFile);
    }
}
