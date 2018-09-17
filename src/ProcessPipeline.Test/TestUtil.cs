// Copyright 2018 @asmichi (at github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;

namespace Asmichi.Utilities
{
    internal static class TestUtil
    {
        public static string TestChildPath => Path.Combine(Environment.CurrentDirectory, "TestChild.exe");
    }

    internal sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            this.Location = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(Location);
        }

        public void Dispose()
        {
            Directory.Delete(Location, true);
        }

        public string Location { get; }
    }
}
