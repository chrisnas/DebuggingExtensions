using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("dotnet-dstrings")]
[assembly: AssemblyDescription("Global tool to provide duplicated strings statistics")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("DuplicatedStrings")]
[assembly: AssemblyCopyright("Copyright © Christophe Nasarre 2020")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d214ee18-bf36-461e-a8ed-fd628683c581")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]
//
// version 1.1.0
// -----------------------------------------
//  - fix command line arguments parsing
//  - fix support of attaching to .NET Core live process
//
//
// version 1.0.1
// -----------------------------------------
//  - make dstrings available as a Global CLI tool
//  - update the help to remove spaces