// Copyright 2018 @asmichi (at github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.IO;
using Asmichi.Utilities.Interop;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.ProcessManagement
{
    // Process creation part
    public sealed partial class ProcessPipeline
    {
        /// <summary>
        /// Starts a process pipeline as specified in <paramref name="startInfo"/>.
        /// </summary>
        /// <remarks>
        /// Please note that <see cref="Start(ProcessPipelineStartInfo)"/> does not throw <see cref="ProcessCreationFailedException"/>
        /// when one of the child processes cannot be created. Instead <see cref="GetExitCodes"/> returns an exit code of null for such a process.
        /// </remarks>
        /// <param name="startInfo"><see cref="ProcessPipelineStartInfo"/>.</param>
        /// <returns>The started process pipeline.</returns>
        /// <exception cref="IOException">Failed to open a specified file.</exception>
        public static ProcessPipeline Start(ProcessPipelineStartInfo startInfo)
        {
            startInfo = startInfo ?? throw new ArgumentNullException(nameof(startInfo));

            if (startInfo.ProcessPipelineItems.Count == 0)
            {
                throw new ArgumentException("At least one item must be added to ProcessPipelineStartInfo.", nameof(startInfo));
            }

            using (var stdHandles = new PipelineStdHandleCreator(
                startInfo.StdInputRedirection,
                startInfo.StdOutputRedirection,
                startInfo.StdErrorRedirection,
                startInfo.StdInputFile,
                startInfo.StdOutputFile,
                startInfo.StdErrorFile,
                startInfo.StdInputHandle,
                startInfo.StdOutputHandle,
                startInfo.StdErrorHandle))
            {
                var count = startInfo.ProcessPipelineItems.Count;
                var entries = new ProcessEntry[count];
                var interChildPipes = new (SafeFileHandle readPipe, SafeFileHandle writePipe)[count - 1];

                try
                {
                    for (int i = 0; i < count - 1; i++)
                    {
                        interChildPipes[i] = FilePal.CreatePipePair();
                    }

                    int index = 0;

                    foreach (var item in startInfo.ProcessPipelineItems)
                    {
                        var readPipe = index == 0 ? stdHandles.PipelineStdIn : interChildPipes[index - 1].readPipe;
                        var writePipe = index == count - 1 ? stdHandles.PipelineStdOut : interChildPipes[index].writePipe;

                        var flags = item.Flags;

                        var stdInput = readPipe ?? stdHandles.PipelineStdIn;
                        var stdOutput = ((flags & ProcessPipelineItemFlags.RedirectStandardOutput) != 0)
                            ? writePipe
                            : stdHandles.PipelineStdOut;
                        var stdError = ((flags & ProcessPipelineItemFlags.RedirectStandardError) != 0)
                            ? writePipe
                            : stdHandles.PipelineStdErr;

                        try
                        {
                            entries[index].ProcessHandle = ChildProcess.Start(
                                fileName: item.FileName,
                                arguments: item.Arguments,
                                workingDirectory: item.WorkingDirectory,
                                environmentVariables: item.EnvironmentVariables,
                                stdIn: stdInput,
                                stdOut: stdOutput,
                                stdErr: stdError);
                        }
                        catch (ProcessCreationFailedException)
                        {
                            entries[index].ProcessHandle = null;
                        }

                        index++;
                    }

                    var processPipeline = new ProcessPipeline(entries, stdHandles.InputStream, stdHandles.OutputStream, stdHandles.ErrorStream);
                    stdHandles.DetachStreams();
                    return processPipeline;
                }
                catch
                {
                    foreach (var e in entries)
                    {
                        e.ProcessHandle?.Dispose();
                    }

                    throw;
                }
                finally
                {
                    foreach (var (readPipe, writePipe) in interChildPipes)
                    {
                        readPipe?.Dispose();
                        writePipe?.Dispose();
                    }
                }
            }
        }
    }
}
