# Asmichi.ProcessPipeline
A .NET library for creating child processes and child process pipelines. Easier, less error-prone, more flexible than `System.Diagnostic.Process`.

This library can be obtained via NuGet (URL)(TBD).

## Comparison with `System.Diagnostic.Process`

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

- Async methods. (Not implemented yet.)
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

See samples (not yet) for more examples. 
