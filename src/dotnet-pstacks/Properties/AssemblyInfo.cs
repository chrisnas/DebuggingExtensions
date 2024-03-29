﻿using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("dotnet-pstacks")]
[assembly: AssemblyDescription("Global tool to display merged call stacks")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("ParallelStacks")]
[assembly: AssemblyCopyright("Copyright © Christophe Nasarre 2020-2021")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("ff145365-87f4-4cc8-a738-eac6e8da486b")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("2.0.2.0")]
[assembly: AssemblyFileVersion("2.0.2.0")]
//
// version 2.0.2
// -----------------------------------------
//  - support .NET Core 3.0, 3.1, .NET 5.0, 6.0
//  - remove support for .NET Core 2.2
//  - add LICENCE file to the package
//
// version 2.0.0
// -----------------------------------------
//  - use ClrMD 2.0
//
// version 1.3.2
// -----------------------------------------
//  - make pstacks available as a Global CLI tool
