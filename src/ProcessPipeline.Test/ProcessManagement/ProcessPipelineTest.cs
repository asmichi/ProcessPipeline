// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.

namespace Asmichi.Utilities.ProcessManagement
{
    public class ProcessPipelineTest
    {
        [Fact]
        public void CanCreateChildProcess()
        {
            {
                var si = new ProcessPipelineStartInfo()
                {
                    StdOutputRedirection = OutputRedirection.OutputPipe,
                };
                si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath);

                using (var sut = ProcessPipeline.Start(si))
                {
                    ChildProcessAssert.CanCreateChildProcess(sut);
                }
            }

            {
                var si = new ProcessPipelineStartInfo()
                {
                    StdOutputRedirection = OutputRedirection.OutputPipe,
                };
                si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath);
                si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack");

                using (var sut = ProcessPipeline.Start(si))
                {
                    ChildProcessAssert.CanCreateChildProcess(sut);
                }
            }
        }

        [Fact]
        public void ReportsCreationFailure()
        {
            var si = new ProcessPipelineStartInfo();
            si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "ExitCode", "0");
            si.Add("nonexistentfile");
            si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "ExitCode", "0");

            using (var sut = ProcessPipeline.Start(si))
            {
                sut.WaitForExit();
                Assert.Equal(new int?[] { 0, null, 0 }, sut.GetExitCodes());
            }
        }

        [Fact]
        public void CanObtainExitCodes()
        {
            {
                var si = new ProcessPipelineStartInfo();
                si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "ExitCode", "0");
                si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "ExitCode", "0");
                si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "ExitCode", "0");

                using (var sut = ProcessPipeline.Start(si))
                {
                    sut.WaitForExit();
                    Assert.True(sut.IsSuccessful);
                    Assert.Equal(0, sut.ExitCode);
                }
            }

            {
                var si = new ProcessPipelineStartInfo();
                si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "ExitCode", "0");
                si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "ExitCode", "1");
                si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "ExitCode", "-1");

                using (var sut = ProcessPipeline.Start(si))
                {
                    sut.WaitForExit();
                    Assert.False(sut.IsSuccessful);
                    Assert.Equal(-1, sut.ExitCode);
                    Assert.Equal(new int?[] { 0, 1, -1 }, sut.GetExitCodes());
                }
            }
        }

        [Fact]
        public void ExitCodesThrowBeforeChildrenExit()
        {
            var si = new ProcessPipelineStartInfo()
            {
                StdInputRedirection = InputRedirection.InputPipe,
            };
            si.Add(TestUtil.DotnetCommand, new[] { TestUtil.TestChildPath, "EchoBack" });
            si.Add(TestUtil.DotnetCommand, new[] { TestUtil.TestChildPath, "EchoBack" });
            si.Add(TestUtil.DotnetCommand, new[] { TestUtil.TestChildPath, "EchoBack" });

            using (var sut = ProcessPipeline.Start(si))
            {
                Assert.Throws<InvalidOperationException>(() => sut.IsSuccessful);
                Assert.Throws<InvalidOperationException>(() => sut.GetExitCodes());
                Assert.Throws<InvalidOperationException>(() => sut.ExitCode);

                sut.StandardInput.Close();
                sut.WaitForExit();

                Assert.True(sut.IsSuccessful);
                Assert.Equal(0, sut.ExitCode);
                Assert.Equal(new int?[] { 0, 0, 0 }, sut.GetExitCodes());
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
                    () => WaitHandle.WaitAll(sut.WaitHandles));
            }
        }

        private static ProcessPipeline CreateForWaitForExitTest()
        {
            var si = new ProcessPipelineStartInfo()
            {
                StdInputRedirection = InputRedirection.InputPipe,
                StdOutputRedirection = OutputRedirection.NullDevice,
            };
            si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack");
            si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack");
            si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack");

            return ProcessPipeline.Start(si);
        }

        [Fact]
        public async Task CorrectlyConnectOutputPipes()
        {
            {
                var si = new ProcessPipelineStartInfo()
                {
                    StdOutputRedirection = OutputRedirection.OutputPipe,
                    StdErrorRedirection = OutputRedirection.ErrorPipe,
                };
                si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoOutAndError");
                si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack");
                si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack");

                using (var sut = ProcessPipeline.Start(si))
                {
                    await ChildProcessAssert.CorrectlyConnectsPipesAsync(sut, "TestChild.Out", "TestChild.Error");
                }
            }

            {
                // invert stdout and stderr
                var si = new ProcessPipelineStartInfo()
                {
                    StdOutputRedirection = OutputRedirection.ErrorPipe,
                    StdErrorRedirection = OutputRedirection.OutputPipe,
                };
                si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoOutAndError");
                si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack");
                si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack");

                using (var sut = ProcessPipeline.Start(si))
                {
                    await ChildProcessAssert.CorrectlyConnectsPipesAsync(sut, "TestChild.Error", "TestChild.Out");
                }
            }
        }

        [Fact]
        public async Task PipesAreAsynchronous()
        {
            var si = new ProcessPipelineStartInfo()
            {
                StdInputRedirection = InputRedirection.InputPipe,
                StdOutputRedirection = OutputRedirection.OutputPipe,
                StdErrorRedirection = OutputRedirection.ErrorPipe,
            };
            si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack");
            si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack");
            si.Add(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack");

            using (var sut = ProcessPipeline.Start(si))
            {
                await ChildProcessAssert.PipesAreAsynchronousAsync(sut);
            }
        }

        [Fact]
        public async Task RedirectBothOutput()
        {
            var si = new ProcessPipelineStartInfo()
            {
                StdOutputRedirection = OutputRedirection.OutputPipe,
            };
            si.Add(ProcessPipelineItemFlags.RedirectBothOutput, TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoOutAndError");
            si.Add(new ProcessPipelineItem(TestUtil.DotnetCommand, TestUtil.TestChildPath, "EchoBack"));

            using (var sut = ProcessPipeline.Start(si))
            using (var sr = new StreamReader(sut.StandardOutput))
            {
                var output = await sr.ReadToEndAsync();
                sut.WaitForExit();

                Assert.Equal("TestChild.OutTestChild.Error", output);
            }
        }
    }
}
