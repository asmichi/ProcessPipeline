// Copyright 2018 @asmichi (at github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;

namespace Asmichi.Utilities.ProcessManagement
{
    /// <summary>
    /// Controls how a process in a pipeline will be created.
    /// </summary>
    [Flags]
    public enum ProcessPipelineItemFlags
    {
        /// <summary>
        /// No specific flags.
        /// </summary>
        None = 0,

        /// <summary>
        /// If set, the stdout of the process is redirected to the stdin of the next process in the pipeline
        /// or the stdout of the pipeline if the process is the last process in the pipeline.
        /// If unset, the stdout is redirected to the stdout of the pipeline.
        /// </summary>
        RedirectStandardOutput = 0x01,

        /// <summary>
        /// If set, the stderr of the process is redirected to the stdin of the next process in the pipeline
        /// or the stdout of the pipeline if the process is the last process in the pipeline.
        /// If unset, the stderr is redirected to the stderr of the pipeline.
        /// </summary>
        RedirectStandardError = 0x02,

        /// <summary>
        /// Both <see cref="RedirectStandardOutput"/> and <see cref="RedirectStandardError"/>.
        /// </summary>
        RedirectBothOutput = RedirectStandardOutput | RedirectStandardError,
    }

    /// <summary>
    /// Specifies parameters that are used to start one of the child processes of a pipeline.
    /// </summary>
    public sealed class ProcessPipelineItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessPipelineItem"/> class.
        /// The FileName and Arguments properties must be set later.
        /// </summary>
        public ProcessPipelineItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessPipelineItem"/> class with the specified command.
        /// </summary>
        /// <param name="fileName">Path to the executable to start.</param>
        /// <param name="arguments">The command-line arguments to be passed to the child process.</param>
        public ProcessPipelineItem(string fileName, params string[] arguments)
        {
            this.FileName = fileName;
            this.Arguments = arguments;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessPipelineItem"/> class with the specified command.
        /// </summary>
        /// <param name="flags"><see cref="ProcessPipelineItemFlags"/>.</param>
        /// <param name="fileName">Path to the executable to start.</param>
        /// <param name="arguments">The command-line arguments to be passed to the child process.</param>
        public void Add(ProcessPipelineItemFlags flags, string fileName, params string[] arguments)
        {
            this.Flags = flags;
            this.FileName = fileName;
            this.Arguments = arguments;
        }

        /// <summary>
        /// Path to the executable to start.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The command-line arguments to be passed to the child process.
        /// null will be treated as Array.Empty&lt;string&gt;().
        /// </summary>
        public IReadOnlyCollection<string> Arguments { get; set; }

        /// <summary>
        /// The working directory of the child process.
        /// If it is null, the child process inherits the working directory of the current process.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// The list of the environment variables that apply to the child process.
        /// If it is null, the child process inherits the environment variables of the current process.
        /// </summary>
        public IReadOnlyCollection<(string name, string value)> EnvironmentVariables { get; set; }

        /// <summary>
        /// Specifies whether each of the stdout and the stderr is redirected to the next process in the pipeline.
        /// </summary>
        public ProcessPipelineItemFlags Flags { get; set; } = ProcessPipelineItemFlags.RedirectStandardOutput;
    }

    /// <summary>
    /// Specifies parameters that are used to start a pipeline.
    /// </summary>
    public sealed class ProcessPipelineStartInfo
    {
        private readonly List<ProcessPipelineItem> _items = new List<ProcessPipelineItem>();

        /// <summary>
        /// Specifies how the stdin of the pipeline (as a whole) is redirected.
        /// The default value is <see cref="InputRedirection.NullDevice"/>.
        /// </summary>
        public InputRedirection StdInputRedirection { get; set; } = InputRedirection.NullDevice;

        /// <summary>
        /// Specifies how the stdout of the pipeline (as a whole) is redirected.
        /// The default value is <see cref="OutputRedirection.ParentOutput"/>.
        /// </summary>
        public OutputRedirection StdOutputRedirection { get; set; } = OutputRedirection.ParentOutput;

        /// <summary>
        /// Specifies how the stderr of the pipeline (as a whole) is redirected.
        /// The default value is <see cref="OutputRedirection.ParentError"/>.
        /// </summary>
        public OutputRedirection StdErrorRedirection { get; set; } = OutputRedirection.ParentError;

        /// <summary>
        /// If <see cref="StdInputRedirection"/> is <see cref="InputRedirection.File"/>,
        /// specifies the file where the stdin of the pipeline is redirected.
        /// Otherwise not used.
        /// </summary>
        public string StdInputFile { get; set; }

        /// <summary>
        /// If <see cref="StdOutputRedirection"/> is <see cref="OutputRedirection.File"/> or <see cref="OutputRedirection.AppendToFile"/>,
        /// specifies the file where the stdout of the pipeline is redirected.
        /// Otherwise not used.
        /// </summary>
        public string StdOutputFile { get; set; }

        /// <summary>
        /// If <see cref="StdErrorRedirection"/> is <see cref="OutputRedirection.File"/> or <see cref="OutputRedirection.AppendToFile"/>,
        /// specifies the file where the stderr of the pipeline is redirected.
        /// Otherwise not used.
        /// </summary>
        public string StdErrorFile { get; set; }

        /// <summary>
        /// If <see cref="StdInputRedirection"/> is <see cref="InputRedirection.Handle"/>,
        /// specifies the file handle where the stdin of the pipeline is redirected.
        /// Otherwise not used.
        /// </summary>
        public SafeFileHandle StdInputHandle { get; set; }

        /// <summary>
        /// If <see cref="StdOutputRedirection"/> is <see cref="OutputRedirection.Handle"/>,
        /// specifies the file handle where the stdout of the pipeline is redirected.
        /// Otherwise not used.
        /// </summary>
        public SafeFileHandle StdOutputHandle { get; set; }

        /// <summary>
        /// If <see cref="StdErrorRedirection"/> is <see cref="OutputRedirection.Handle"/>,
        /// specifies the file handle where the stderr of the pipeline is redirected.
        /// Otherwise not used.
        /// </summary>
        public SafeFileHandle StdErrorHandle { get; set; }

        /// <summary>
        /// Adds an item of the pipeline. Its stdout will be redirected to the next process in the pipeline (if any) or the stdout of the pipeline.
        /// The stderr will be redirected to the stderr of the pipeline.
        /// </summary>
        /// <param name="fileName">Path to the executable to start.</param>
        /// <param name="arguments">The command-line arguments to be passed to the child process.</param>
        public void Add(string fileName, params string[] arguments)
        {
            Add(ProcessPipelineItemFlags.RedirectStandardOutput, fileName, arguments);
        }

        /// <summary>
        /// Adds an item of the pipeline. Its stdout and the stderr are redirected as specified in <paramref name="flags"/>.
        /// </summary>
        /// <param name="flags"><see cref="ProcessPipelineItemFlags"/>.</param>
        /// <param name="fileName">Path to the executable to start.</param>
        /// <param name="arguments">The command-line arguments to be passed to the child process.</param>
        public void Add(ProcessPipelineItemFlags flags, string fileName, params string[] arguments)
        {
            var item = new ProcessPipelineItem()
            {
                FileName = fileName,
                Arguments = arguments,
                Flags = flags,
            };

            Add(item);
        }

        /// <summary>
        /// Adds an item of the pipeline.
        /// </summary>
        /// <param name="item">A <see cref="ProcessPipelineItem"/> that describes the item.</param>
        public void Add(ProcessPipelineItem item)
        {
            _items.Add(item);
        }

        /// <summary>
        /// Gets all items added to the pipeline.
        /// </summary>
        public IReadOnlyCollection<ProcessPipelineItem> ProcessPipelineItems => _items;
    }
}
