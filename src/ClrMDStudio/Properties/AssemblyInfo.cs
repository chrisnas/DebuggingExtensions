using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ClrMDStudio")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("ClrMDStudio")]
[assembly: AssemblyCopyright("Copyright © Christophe Nasarre 2016-2019")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

//In order to begin building localizable applications, set
//<UICulture>CultureYouAreCodingWith</UICulture> in your .csproj file
//inside a <PropertyGroup>.  For example, if you are using US english
//in your source files, set the <UICulture> to en-US.  Then uncomment
//the NeutralResourceLanguage attribute below.  Update the "en-US" in
//the line below to match the UICulture setting in the project file.

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]


[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page,
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page,
                                              // app, or any theme specific resource dictionaries)
)]


// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.5.2.0")]
[assembly: AssemblyFileVersion("1.5.2.0")]
//
// version 1.5.2
// -----------------------------------------
//  - update dependencies version
//
// version 1.5.1
// -----------------------------------------
//  - bug fix for .NET Core CLR 2.0/2.1 --> 2.2 structure changes for thread pool work item callback
//
// 1.5
// -------------------------
//  - show GC segments with free and pinned objects
//  - list pinned objects
//
// 1.4
// -------------------------
//  - FIX: change symbol management to rely on "http" instead of "https" and use a user defined local folder
//  - FIX: group results in ThreadPool analyzer summary
//  - FIX: try to detect mini dumps in which there is no CLR information
//  - list timers
//
// 1.3
// -------------------------
//  - FIX: clear all panes when a new dump is loaded
//  - provides strings statistics like in !dumpheap -stat
//
//
// 1.2
// -------------------------
//  - multiple windows UI
//
//
// 1.1
// -------------------------
//  - string duplicate
//
//
// 1.0: initial version
// -------------------------
//  - threads
//  - thread pool
//
