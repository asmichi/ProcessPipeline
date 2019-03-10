// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.

namespace Asmichi.Utilities.ProcessManagement
{
    public class ChildProcessTest
    {
        [Fact]
        public void CanCreateChildProcess()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath)
            {
                StdOutputRedirection = OutputRedirection.OutputPipe,
            };

            using (var sut = ChildProcess.Start(si))
            {
                ChildProcessAssert.CanCreateChildProcess(sut);
            }
        }

        [Fact]
        public void ReportsCreationFailure()
        {
            var si = new ChildProcessStartInfo("nonexistentfile");

            Assert.Throws<ProcessCreationFailedException>(() => ChildProcess.Start(si));
        }

        [Fact]
        public void CanObtainExitCode()
        {
            {
                var si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "ExitCode", "0");

                using (var sut = ChildProcess.Start(si))
                {
                    sut.WaitForExit();
                    Assert.True(sut.IsSuccessful);
                    Assert.Equal(0, sut.ExitCode);
                }
            }

            {
                var si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "ExitCode", "-1");

                using (var sut = ChildProcess.Start(si))
                {
                    sut.WaitForExit();
                    Assert.False(sut.IsSuccessful);
                    Assert.Equal(-1, sut.ExitCode);
                }
            }
        }

        [Fact]
        public void ExitCodeThrowsBeforeChildExits()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack")
            {
                StdInputRedirection = InputRedirection.InputPipe,
            };

            using (var sut = ChildProcess.Start(si))
            {
                Assert.Throws<InvalidOperationException>(() => sut.IsSuccessful);
                Assert.Throws<InvalidOperationException>(() => sut.ExitCode);

                sut.StandardInput.Close();
                sut.WaitForExit();

                Assert.True(sut.IsSuccessful);
                Assert.Equal(0, sut.ExitCode);
            }
        }

        [Fact]
        public void WaitForExitTimesOut()
        {
            using (var sut = CreateForWaitForExitTest())
            {
                ChildProcessAssert.WaitForExitTimesOut(sut);
            }
        }

        [Fact]
        public async Task WaitForExitAsyncTimesOut()
        {
            using (var sut = CreateForWaitForExitTest())
            {
                await ChildProcessAssert.WaitForExitAsyncTimesOut(sut);
            }
        }

        [Fact]
        public async Task CanCancelWaitForExitAsync()
        {
            using (var sut = CreateForWaitForExitTest())
            {
                await ChildProcessAssert.CanCancelWaitForExitAsync(
                    sut,
                    () => sut.WaitHandle.WaitOne());
            }
        }

        private static ChildProcess CreateForWaitForExitTest()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack")
            {
                StdInputRedirection = InputRedirection.InputPipe,
                StdOutputRedirection = OutputRedirection.NullDevice,
            };
            return ChildProcess.Start(si);
        }

        [Fact]
        public async Task CorrectlyConnectOutputPipes()
        {
            {
                var si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoOutAndError")
                {
                    StdOutputRedirection = OutputRedirection.OutputPipe,
                    StdErrorRedirection = OutputRedirection.ErrorPipe,
                };

                using (var sut = ChildProcess.Start(si))
                {
                    await ChildProcessAssert.CorrectlyConnectsPipesAsync(sut, "TestChild.Out", "TestChild.Error");
                }
            }

            {
                // invert stdout and stderr
                var si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoOutAndError")
                {
                    StdOutputRedirection = OutputRedirection.ErrorPipe,
                    StdErrorRedirection = OutputRedirection.OutputPipe,
                };

                using (var sut = ChildProcess.Start(si))
                {
                    await ChildProcessAssert.CorrectlyConnectsPipesAsync(sut, "TestChild.Error", "TestChild.Out");
                }
            }
        }

        [Fact]
        public async Task PipesAreAsynchronous()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack")
            {
                StdInputRedirection = InputRedirection.InputPipe,
                StdOutputRedirection = OutputRedirection.OutputPipe,
                StdErrorRedirection = OutputRedirection.ErrorPipe,
            };

            using (var sut = ChildProcess.Start(si))
            {
                await ChildProcessAssert.PipesAreAsynchronousAsync(sut);
            }
        }

        [Fact]
        public void RedirectionToFile()
        {
            using (var tmp = new TemporaryDirectory())
            {
                var inFile = Path.Combine(tmp.Location, "in");
                var outFile = Path.Combine(tmp.Location, "out");
                var errFile = Path.Combine(tmp.Location, "err");

                // StdOutputFile StdErrorFile
                {
                    // File
                    var si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoOutAndError")
                    {
                        StdOutputRedirection = OutputRedirection.File,
                        StdOutputFile = outFile,
                        StdErrorRedirection = OutputRedirection.File,
                        StdErrorFile = errFile,
                    };

                    using (var sut = ChildProcess.Start(si))
                    {
                        sut.WaitForExit();
                        Assert.True(sut.IsSuccessful);
                    }

                    Assert.Equal("TestChild.Out", File.ReadAllText(outFile));
                    Assert.Equal("TestChild.Error", File.ReadAllText(errFile));

                    // AppendToFile
                    si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoOutAndError")
                    {
                        StdOutputRedirection = OutputRedirection.AppendToFile,
                        StdOutputFile = errFile,
                        StdErrorRedirection = OutputRedirection.AppendToFile,
                        StdErrorFile = outFile,
                    };

                    using (var sut = ChildProcess.Start(si))
                    {
                        sut.WaitForExit();
                        Assert.True(sut.IsSuccessful);
                    }

                    Assert.Equal("TestChild.OutTestChild.Error", File.ReadAllText(outFile));
                    Assert.Equal("TestChild.ErrorTestChild.Out", File.ReadAllText(errFile));
                }

                // StdInputFile
                {
                    const string text = "foobar";
                    File.WriteAllText(inFile, text);

                    var si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack")
                    {
                        StdInputRedirection = InputRedirection.File,
                        StdInputFile = inFile,
                        StdOutputRedirection = OutputRedirection.File,
                        StdOutputFile = outFile,
                    };

                    using (var sut = ChildProcess.Start(si))
                    {
                        sut.WaitForExit();
                        Assert.True(sut.IsSuccessful);
                    }

                    Assert.Equal(text, File.ReadAllText(outFile));
                }
            }
        }

        [Fact]
        public void CanRedirectToSameFile()
        {
            using (var tmp = new TemporaryDirectory())
            {
                var outFile = Path.Combine(tmp.Location, "out");

                // StdOutputFile StdErrorFile
                {
                    // File
                    var si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoOutAndError")
                    {
                        StdOutputRedirection = OutputRedirection.File,
                        StdOutputFile = outFile,
                        StdErrorRedirection = OutputRedirection.File,
                        StdErrorFile = outFile,
                    };

                    using (var sut = ChildProcess.Start(si))
                    {
                        sut.WaitForExit();
                        Assert.True(sut.IsSuccessful);
                    }

                    Assert.Equal("TestChild.OutTestChild.Error", File.ReadAllText(outFile));

                    // AppendToFile
                    si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoOutAndError")
                    {
                        StdOutputRedirection = OutputRedirection.AppendToFile,
                        StdOutputFile = outFile,
                        StdErrorRedirection = OutputRedirection.AppendToFile,
                        StdErrorFile = outFile,
                    };

                    using (var sut = ChildProcess.Start(si))
                    {
                        sut.WaitForExit();
                        Assert.True(sut.IsSuccessful);
                    }

                    Assert.Equal("TestChild.OutTestChild.ErrorTestChild.OutTestChild.Error", File.ReadAllText(outFile));
                }
            }
        }

        [Fact]
        public void RedirectionToHandle()
        {
            using (var tmp = new TemporaryDirectory())
            {
                var inFile = Path.Combine(tmp.Location, "in");
                var outFile = Path.Combine(tmp.Location, "out");
                var errFile = Path.Combine(tmp.Location, "err");

                // StdOutputHandle StdErrorHandle
                {
                    using (var fsOut = File.Create(outFile))
                    using (var fsErr = File.Create(errFile))
                    {
                        // File
                        var si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoOutAndError")
                        {
                            StdOutputRedirection = OutputRedirection.Handle,
                            StdOutputHandle = fsOut.SafeFileHandle,
                            StdErrorRedirection = OutputRedirection.Handle,
                            StdErrorHandle = fsErr.SafeFileHandle,
                        };

                        using (var sut = ChildProcess.Start(si))
                        {
                            sut.WaitForExit();
                            Assert.True(sut.IsSuccessful);
                        }
                    }

                    Assert.Equal("TestChild.Out", File.ReadAllText(outFile));
                    Assert.Equal("TestChild.Error", File.ReadAllText(errFile));
                }

                // StdInputHandle
                {
                    const string text = "foobar";
                    File.WriteAllText(inFile, text);

                    using (var fsIn = File.OpenRead(inFile))
                    {
                        var si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack")
                        {
                            StdInputRedirection = InputRedirection.Handle,
                            StdInputHandle = fsIn.SafeFileHandle,
                            StdOutputRedirection = OutputRedirection.File,
                            StdOutputFile = outFile,
                        };

                        using (var sut = ChildProcess.Start(si))
                        {
                            sut.WaitForExit();
                            Assert.True(sut.IsSuccessful);
                        }
                    }

                    Assert.Equal(text, File.ReadAllText(outFile));
                }
            }
        }

        [Fact]
        public void CanSetEnvironmentVariables()
        {
            var si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "DumpEnvironmentVariables")
            {
                StdOutputRedirection = OutputRedirection.OutputPipe,
                EnvironmentVariables = new[] { ("A", "a"), ("BB", "bb") },
            };

            using (var sut = ChildProcess.Start(si))
            using (var sr = new StreamReader(sut.StandardOutput))
            {
                var output = sr.ReadToEnd();
                sut.WaitForExit();
                Assert.Equal(new[] { "A=a", "BB=bb" }, output.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        [Fact]
        public void CanSetWorkingDirectory()
        {
            using (var tmp = new TemporaryDirectory())
            {
                var si = new ChildProcessStartInfo(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoWorkingDirectory")
                {
                    StdOutputRedirection = OutputRedirection.OutputPipe,
                    WorkingDirectory = tmp.Location,
                };

                using (var sut = ChildProcess.Start(si))
                using (var sr = new StreamReader(sut.StandardOutput))
                {
                    var output = sr.ReadToEnd();
                    sut.WaitForExit();
                    Assert.Equal(tmp.Location, output);
                }
            }
        }
    }
}
