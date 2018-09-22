# Asmichi.ProcessPipeline
A .NET library for creating child processes and child process pipelines. Easier, less error-prone, more flexible than `System.Diagnostics.Process`.

This library can be obtained via [NuGet](https://www.nuget.org/packages/Asmichi.ProcessPipeline/).

## Comparison with `System.Diagnostics.Process`

- It concentrates on creating a child process and obtaining its output. It cannot query status of a process.
- Can redirect stdin, stdout and stderr differently.
- More destinations of redirection:
    - NUL
    - File (optionally appended)
    - Pipe
    - Handle
- Less error-prone default values for redirection:
    - stdin to NUL
    - stdout to the current stdout
    - stderr to the current stderr
- Pipes are asynchronous; asynchronous reads and writes will be handled by IO completion ports.
- Async methods. (Not implemented yet.)

## Features Not Implemented Yet

- Async methods.
- Custom environment variables.

# License

[The MIT License](LICENSE)

# Supported Runtimes

Frameworks:

- `net461` or later
- `netcoreapp2.1` or later
- (Will support frameworks that implement `netstandard2.1`)

Runtimes:

- `win7-x86` or later (Desktop)
- `win7-x64` or later (Desktop)

`linux-x64` support is planned but not implemented.

# Examples

See [ProcessPipeline.Example](src/ProcessPipeline.Example/) (not yet) for more examples.

## Basic

```cs
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
```

## Redirection to File

```cs
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
```

## Process Pipeline

```cs
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
```
