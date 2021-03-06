Visit https://github.com/asmichi/ChildProcess for the current development.
This repository has been archived as a reference for a ProcessPipeline implementation.

# Asmichi.ProcessPipeline
A .NET library that provides functionality for creating child processes. Easier, less error-prone, more flexible than `System.Diagnostics.Process` at creating child processes.

This library can be obtained via [NuGet](https://www.nuget.org/packages/Asmichi.ProcessPipeline/).

[![Build Status](https://dev.azure.com/asmichi/ProcessPipeline/_apis/build/status/ProcessPipeline-CI?branchName=master)](https://dev.azure.com/asmichi/ProcessPipeline/_build/latest?definitionId=2&branchName=master)

## Comparison with `System.Diagnostics.Process`

- Concentrates on creating a child process and obtaining its output.
    - Cannot query status of a process.
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
- `WaitForExitAsync`.

# License

[The MIT License](LICENSE)

# Supported Runtimes

Frameworks:

- `net471` or later
- `netcoreapp2.1` or later
- (Will support frameworks that implement `netstandard2.1`)

Runtimes:

- `win10-x86` or later (Desktop)
- `win10-x64` or later (Desktop)

`linux-x64` support is planned but not implemented.

# Notes

- When overriding environment variables, it is recommended that you include basic environment variables such as `SystemRoot`, etc.

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
    await p.WaitForExitAsync();
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
    await p.WaitForExitAsync();
}

// ALLUSERSPROFILE=C:\ProgramData
// ...
Console.WriteLine(File.ReadAllText("env.txt"));
```
