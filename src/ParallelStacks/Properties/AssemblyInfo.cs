using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("pstacks")]
[assembly: AssemblyDescription("Parallel Stacks shows merged thread stacks")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("pstacks")]
[assembly: AssemblyCopyright("Copyright © Christophe Nasarre 2019")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("1bb98487-ff65-4752-ad21-41c8e8acdb9c")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("1.3.0.0")]
[assembly: AssemblyFileVersion("1.3.0.0")]
//
// version 1.3
// -----------------------------------------
//  - show 4 thread IDs per frame group by default (-all allow all threads to be displayed)
//
//
// version 1.2
// -----------------------------------------
//  - attaching to a live process now works with ClrMD 1.1.35704
//
// version 1.1
// -----------------------------------------
//  - supports attaching to a live process (even on Linux but not working with ClrMD 1.1.35504)
//  - change parallel stacks rendering implementation
//  - update dependencies version
//
// version 1.0.1
// -----------------------------------------
//  - remove namespaces from generic parameters type name in method signatures
