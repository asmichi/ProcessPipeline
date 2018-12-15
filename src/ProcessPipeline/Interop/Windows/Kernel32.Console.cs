// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.Interop.Windows
{
    internal static partial class Kernel32
    {
        [DllImport(DllName)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport(DllName, SetLastError = true)]
        public static extern bool GetConsoleMode([In] SafeHandle hConsoleHandle, [Out] out int lpMode);

        // Not using SafeHandle; we do not own the returned handle.
        [DllImport(DllName, SetLastError = true)]
        public static extern IntPtr GetStdHandle([In]int nStdHandle);

        [DllImport(DllName, SetLastError = false)]
        public static extern int CreatePseudoConsole(
            [In] COORD size,
            [In] SafeFileHandle hInput,
            [In] SafeFileHandle hOutput,
            [In] uint dwFlags,
            [Out] out SafePseudoConsoleHandle phPC);

        [DllImport(DllName, SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern bool ClosePseudoConsole([In]IntPtr handle);
    }
}
