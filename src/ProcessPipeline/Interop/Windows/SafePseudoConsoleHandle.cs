// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.Interop.Windows
{
    internal sealed class SafePseudoConsoleHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafePseudoConsoleHandle()
            : this(IntPtr.Zero, true)
        {
        }

        public SafePseudoConsoleHandle(IntPtr handle)
            : this(handle, true)
        {
        }

        public SafePseudoConsoleHandle(IntPtr existingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(existingHandle);
        }

        protected override bool ReleaseHandle()
        {
            Kernel32.ClosePseudoConsole(handle);
            return true;
        }
    }
}
