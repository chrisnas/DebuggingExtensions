# DebuggingExtensions
The few "debugging extensions" that have been created at Criteo to help post-mortem .NET applications analysis are now available:

- as a [stand alone tool](./Documentation/ClrMDStudio.md) to load a .NET application memory dump and start automatic thread, thread pool, tasks and timer analysis.
   [zip](./binaries/ClrMDStudio-1.5.2_x64.zip)
- as a [WinDBG extension](./Documentation/gsose.md) to get the same level of details plus more commands such as getting a method signature based on its address.
   [zip](./binaries/gsose-1.6.1_x64.zip)
- as a [.NET Core console tool](./Documentation/pstacks.md) to load a .NET application memory dump and show merged threads call stack à la Visual Studio "parallel stacks" (works also on Linux)
   [zip](./binaries/pstacks-1.1.zip).
   Note that you could install it as a global CLI tool
   - "dotnet tool install --global dotnet-pstacks" to install it
   - "dotnet pstacks <pid or .dmp file path>" to get your parallel stacks
- as a [.NET standard assembly](./Documentation/parallelStacks.Runtime.md) to build and render parallel stacks from a memory dump file or a live process (on Windows only).
   [zip](./binaries/ParallelStacks.Runtime-1.0.zip)
  More analyzers and commands will be added as needed.

- as a [GUI tool](./Documentation/leakShell.md) first published in 2011 to chase .NET memory leaks. The source code is now available and dumps are droppable into the snapshot listview to compare them automatically.
- as a [.NET Core console tool](./Documentation/dstrings.md) to analyze duplicated strings in a .NET application (live/memory dump) (works also on Linux)
   Note that you could install it as a global CLI tool
   - "dotnet tool install --global dotnet-dstrings" to install it
   - "dotnet dstrings <pid or .dmp file path>" to get the statistics 



## Introduction
Most of the code is detailed in the blog series related to ClrMD:

Part 1: [Bootstrap ClrMD to load a dump.](http://labs.criteo.com/2017/02/going-beyond-sos-clrmd-part-1/)

Part 2: [Find duplicated strings with ClrMD heap traversing.](http://labs.criteo.com/2017/03/clrmd-part-2-clrruntime-clrheap-traverse-managed-heap/)

Part 3: [List timers by following static fields links.](http://labs.criteo.com/2017/05/clrmd-part-3-dealing-static-instance-fields-list-timers/)

Part 4: [Identify timers callback and other properties.](http://labs.criteo.com/2017/05/clrmd-part-4-callbacks-called-timers/)

Part 5: [Use ClrMD to extend SOS in WinDBG.](http://labs.criteo.com/2017/06/clrmd-part-5-how-to-use-clrmd-to-extend-sos-in-windbg/)

Part 6: [Manipulate memory structures like real objects.](http://labs.criteo.com/2017/08/clrmd-part-6-manipulate-memory-structures-like-real-objects/)

Part 7: [Manipulate nested structs using dynamic.](http://labs.criteo.com/2017/08/clrmd-part-7-manipulate-nested-structs-using-dynamic/)

Part 8: [Spelunking inside the .NET Thread Pool.](http://labs.criteo.com/2017/11/clrmd-part-8-spelunking-inside-the-net-thread-pool/)

Part 9: [Deciphering Tasks and Thread Pool items.](http://labs.criteo.com/2017/12/clrmd-part-9-deciphering-tasks-thread-pool-items/)

part 10: [Getting another view on thread stacks with ClrMD](https://medium.com/@chnasarre/getting-another-view-on-thread-stacks-with-clrmd-2fd683642a73/)

The detailed features are available either as a [stand alone tool](./Documentation/ClrMDStudio.md) or a [WinDBG extension](./Documentation/gsose.md).
More commands will be added as needed.




## Source Code
The `DebuggingExtensions` Visual Studio 2017 solution contains three projects:

1. `ClrMDStudio`: WPF application that loads a dump file on which commands to be executed 

2. `gsose`: "***G**rand **S**on **O**f **S**trike **E**xtension*" for WinDBG that exposes the same commands (and more)

3. `pstacks`: .NET Core console application that loads a dump file (+ attachs to a live process on Windows) and shows merged parallel stacks

4. `ParallelStacks.Runtime`: .NET Assembly (and available as a nuget too) to let you build and render parallel stacks from your own code

5. `LeakShell`: .NET WinForms application to easily spot leaky class instances

6. `dstrings`: .NET Core console application that displays duplicated strings statistics 


These projects depends on Nuget packages:

- [ClrMD](https://github.com/Microsoft/clrmd): C# library to explore dump files.
- [DynaMD](https://github.com/kevingosse/DynaMD): C# `dynamic`-based helpers on top of ClrMD.
- [ClrMDExports](https://github.com/kevingosse/ClrMDExports): Helper to write WinDBG/LLDB extensionss on top of ClrMD.

