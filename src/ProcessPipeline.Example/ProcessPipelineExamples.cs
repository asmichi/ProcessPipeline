// Copyright 2018 @asmichi (at github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;
using System.Threading.Tasks;
using Asmichi.Utilities.ProcessManagement;

#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.

namespace Asmichi.Utilities
{
    public static class ProcessPipelineExamples
    {
        public static async Task Main()
        {
            WriteHeader(nameof(BasicAsync));
            await BasicAsync();

            WriteHeader(nameof(RedirectionToFile));
            RedirectionToFile();

            WriteHeader(nameof(Pipeline));
            Pipeline();
        }

        private static void WriteHeader(string name)
        {
            Console.WriteLine();
            Console.WriteLine("*** {0}", name);
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
                p.WaitForExit();
                // ExitCode: 0
                Console.WriteLine("ExitCode: {0}", p.ExitCode);
            }
        }

        private static void RedirectionToFile()
        {
            var si = new ChildProcessStartInfo("cmd", "/C", "set")
            {
                StdOutputRedirection = OutputRedirection.File,
                StdOutputFile = "env.txt"
            };

            using (var p = ChildProcess.Start(si))
            {
                p.WaitForExit();
            }

            // ALLUSERSPROFILE=C:\ProgramData
            // ...
            Console.WriteLine(File.ReadAllText("env.txt"));
        }

        private static void Pipeline()
        {
            var si = new ProcessPipelineStartInfo()
            {
                StdOutputRedirection = OutputRedirection.File,
                StdOutputFile = "env.txt"
            };
            si.Add("cmd", "/C", "set");
            si.Add("findstr", "PROCESSOR");

            using (var p = ProcessPipeline.Start(si))
            {
                p.WaitForExit();
            }

            // NUMBER_OF_PROCESSORS=16
            // PROCESSOR_ARCHITECTURE = AMD64
            // ...
            Console.WriteLine(File.ReadAllText("env.txt"));
        }
    }
}
