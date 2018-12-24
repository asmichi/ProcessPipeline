// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System.ComponentModel;
using Asmichi.Utilities.Interop.Windows;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.PlatformAbstraction.Windows
{
    internal static class HandlePalWindows
    {
        public static SafeWaitHandle ToWaitHandle(SafeProcessHandle handle)
        {
            if (!Kernel32.DuplicateHandle(
                Kernel32.GetCurrentProcess(),
                handle,
                Kernel32.GetCurrentProcess(),
                out SafeWaitHandle waitHandle,
                0,
                false,
                Kernel32.DUPLICATE_SAME_ACCESS))
            {
                throw new Win32Exception();
            }

            return waitHandle;
        }
    }
}
