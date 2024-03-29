﻿using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ParallelStacks.Runtime")]
[assembly: AssemblyDescription("Helpers to build and render parallel stacks")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("ParallelStacks.Runtime")]
[assembly: AssemblyCopyright("Copyright © Christophe Nasarre 2019-2021")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("25ed6f0f-2c89-43e0-86b4-4f6fab30a905")]

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
//  - add support for .NET Core 3.0, 3.1, .NET 5.0, 6.0
//  - remove support for .NET Core 2.2
//  - add LICENCE file to the package
//
//
// version 2.0.1
// -----------------------------------------
//  - add a strong name to be useable by dotnet-dump
//
//
// version 2.0
// -----------------------------------------
//  - use ClrMD 2.0
//  - update copyright year to 2020
//
//
// version 1.3
// -----------------------------------------
//  - refactor rendering outside of the ParallelStack class
//  - add HTML rendering
//
//
// version 1.2
// -----------------------------------------
//  - show up to 4 thread IDs per frame group by default
//
//
// version 1.1
// -----------------------------------------
//  - use ClrMD version 1.1.37504 to support process-attach on Linux
//
//
// version 1.0
// -----------------------------------------
//  - refactor rendering
//