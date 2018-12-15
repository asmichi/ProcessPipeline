// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Asmichi.Utilities.ProcessManagement;

#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.

namespace Asmichi.Utilities
{
    public static class ProcessPipelineExamples
    {
        public static async Task Main()
        {
            WriteHeader(nameof(PseudoConsoleAsync));
            await PseudoConsoleAsync();

            // WriteHeader(nameof(BasicAsync));
            // await BasicAsync();

            // WriteHeader(nameof(RedirectionToFileAsync));
            // await RedirectionToFileAsync();

            // WriteHeader(nameof(PipelineAsync));
            // await PipelineAsync();

            // WriteHeader(nameof(WaitForExitAsync));
            // await WaitForExitAsync();
        }

        private static void WriteHeader(string name)
        {
            Console.WriteLine();
            Console.WriteLine("*** {0}", name);
        }

        private static async Task PseudoConsoleAsync()
        {
            var si = new ChildProcessStartInfo("ping", "localhost")
            {
                StdInputRedirection = InputRedirection.NullDevice,
                StdErrorRedirection = OutputRedirection.OutputPipe,
                StdOutputRedirection = OutputRedirection.OutputPipe,
            };

            using (var p = ChildProcess.Start(si))
            {
                Thread.Sleep(500);
                // Send Ctrl+C
                await p.PseudoConsoleInputStream.WriteAsync(new byte[] { 0x03 }, 0, 1);
                await p.PseudoConsoleInputStream.FlushAsync();

                using (var sr = new StreamReader(p.StandardOutput, Console.OutputEncoding))
                {
                    Console.WriteLine(await sr.ReadToEndAsync());
                }
                // ExitCode: C000013A
                // (STATUS_CONTROL_C_EXIT)
                Console.WriteLine("ExitCode: {0:X8}", p.ExitCode);
            }
        }

        private static async Task BasicAsync()
        {
            var si = new ChildProcessStartInfo("cmd", "/C", "echo", "foo")
            {
                StdOutputRedirection = OutputRedirection.OutputPipe,
            };

            using (var p = ChildProcess.Start(si))
            {
                using (var sr = new StreamReader(p.StandardOutput))
                {
                    // "foo"
                    Console.Write(await sr.ReadToEndAsync());
                }
                await p.WaitForExitAsync();
                // ExitCode: 0
                Console.WriteLine("ExitCode: {0}", p.ExitCode);
            }
        }

        private static async Task RedirectionToFileAsync()
        {
            var si = new ChildProcessStartInfo("cmd", "/C", "set")
            {
                StdOutputRedirection = OutputRedirection.File,
                StdOutputFile = "env.txt",
            };

            using (var p = ChildProcess.Start(si))
            {
                await p.WaitForExitAsync();
            }

            // ALLUSERSPROFILE=C:\ProgramData
            // ...
            Console.WriteLine(File.ReadAllText("env.txt"));
        }

        private static async Task PipelineAsync()
        {
            var si = new ProcessPipelineStartInfo()
            {
                StdOutputRedirection = OutputRedirection.File,
                StdOutputFile = "env.txt",
            };
            si.Add("cmd", "/C", "set");
            si.Add("findstr", "PROCESSOR");

            using (var p = ProcessPipeline.Start(si))
            {
                await p.WaitForExitAsync();
            }

            // NUMBER_OF_PROCESSORS=16
            // PROCESSOR_ARCHITECTURE = AMD64
            // ...
            Console.WriteLine(File.ReadAllText("env.txt"));
        }

        // Truely asynchronous WaitForExitAsync: WaitForExitAsync does not consume a thread-pool thread.
        // You will not need a dedicated thread for handling a child process.
        // You can handle more processes than the number of threads.
        private static async Task WaitForExitAsync()
        {
            const int N = 128;

            var stopWatch = Stopwatch.StartNew();
            var tasks = new Task[N];

            for (int i = 0; i < N; i++)
            {
                tasks[i] = SpawnCmdAsync();
            }

            // Spawned 128 processes.
            // The 128 processes have exited.
            // Elapsed Time: 3367 ms
            Console.WriteLine("Spawned {0} processes.", N);
            await Task.WhenAll(tasks);
            Console.WriteLine("The {0} processes have exited.", N);
            Console.WriteLine("Elapsed Time: {0} ms", stopWatch.ElapsedMilliseconds);

            async Task SpawnCmdAsync()
            {
                var si = new ChildProcessStartInfo("cmd", "/C", "timeout", "3")
                {
                    StdInputRedirection = InputRedirection.ParentInput,
                    StdOutputRedirection = OutputRedirection.NullDevice,
                };

                using (var p = ChildProcess.Start(si))
                {
                    await p.WaitForExitAsync();
                }
            }
        }
    }
}
