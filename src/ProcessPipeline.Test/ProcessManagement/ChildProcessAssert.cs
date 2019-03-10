// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.

namespace Asmichi.Utilities.ProcessManagement
{
    internal static class ChildProcessAssert
    {
        // Input: TestChild with no arg
        public static void CanCreateChildProcess(IChildProcess sut)
        {
            sut.WaitForExit();
            Assert.Equal(0, sut.ExitCode);

            // This closes StandardOutput, which should be acceptable.
            using (var sr = new StreamReader(sut.StandardOutput))
            {
                Assert.Equal("TestChild", sr.ReadToEnd());
            }
        }

        // Input: EchoBack process
        public static void ExitCodeThrowsBeforeChildExits(IChildProcess sut)
        {
            Assert.Throws<InvalidOperationException>(() => sut.IsSuccessful);
            Assert.Throws<InvalidOperationException>(() => sut.ExitCode);

            sut.StandardInput.Close();
            sut.WaitForExit();

            Assert.True(sut.IsSuccessful);
            Assert.Equal(0, sut.ExitCode);
        }

        // Input: EchoBack process
        public static void WaitForExitTimesOut(IChildProcess sut)
        {
            Assert.False(sut.WaitForExit(0));
            Assert.False(sut.WaitForExit(1));

            sut.StandardInput.Close();
            sut.WaitForExit();
            Assert.True(sut.WaitForExit(0));
        }

        // Input: EchoBack process
        public static async Task WaitForExitAsyncTimesOut(IChildProcess sut)
        {
            Assert.False(await sut.WaitForExitAsync(0));
            Assert.False(await sut.WaitForExitAsync(1));

            sut.StandardInput.Close();
            Assert.True(await sut.WaitForExitAsync(1000));
        }

        // Input: EchoBack process
        public static async Task CanCancelWaitForExitAsync(IChildProcess sut, Action rawWaitForExit)
        {
            Assert.False(await sut.WaitForExitAsync(0));

            using (var cts = new CancellationTokenSource())
            {
                var t = sut.WaitForExitAsync(1000, cts.Token);
                cts.Cancel();
                Assert.Throws<TaskCanceledException>(() => t.GetAwaiter().GetResult());
            }

            sut.StandardInput.Close();
            rawWaitForExit();

            // If the process has already exited, returns true instead of returning CanceledTask.
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                Assert.True(await sut.WaitForExitAsync(0, cts.Token));
            }
        }

        // Input: EchoOutAndError process
        public static async Task CorrectlyConnectsPipesAsync(IChildProcess sut, string expectedStdout, string expectedStderr)
        {
            using (var srOut = new StreamReader(sut.StandardOutput))
            using (var srErr = new StreamReader(sut.StandardError))
            {
                var stdoutTask = srOut.ReadToEndAsync();
                var stderrTask = srErr.ReadToEndAsync();
                sut.WaitForExit();

                Assert.Equal(expectedStdout, await stdoutTask);
                Assert.Equal(expectedStderr, await stderrTask);
            }
        }

        // Input: EchoBack process
        public static async Task PipesAreAsynchronousAsync(IChildProcess sut)
        {
            Assert.True(((FileStream)sut.StandardInput).IsAsync);
            Assert.True(((FileStream)sut.StandardOutput).IsAsync);
            Assert.True(((FileStream)sut.StandardError).IsAsync);

            using (var sr = new StreamReader(sut.StandardOutput))
            {
                const string text = "foobar";
                var stdoutTask = sr.ReadToEndAsync();
                using (var sw = new StreamWriter(sut.StandardInput))
                {
                    await sw.WriteAsync(text);
                }
                Assert.Equal(text, await stdoutTask);
            }

            sut.WaitForExit();
            Assert.Equal(0, sut.ExitCode);
        }
    }
}
