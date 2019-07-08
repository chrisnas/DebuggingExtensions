# ParallelStacks.Runtime

This .NET library allows you to compute and display the merged threads call stacks à la Visual Studio "Parallel Stacks".
It supports memory dumps and live processes (on Windows only).

## Platforms
Windows and Linux.
Note that it is not possible to attach to a live process on Linux (depends on fixes to be done in ClrMD)