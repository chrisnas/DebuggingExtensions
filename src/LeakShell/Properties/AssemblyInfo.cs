using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("LeakShell")]
[assembly: AssemblyDescription("Tool for detecting .NET memory leaks")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Christophe.Nasarre")]
[assembly: AssemblyProduct("LeakShell")]
[assembly: AssemblyCopyright("Copyright © Christophe Nasarre 2011-2020")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("ad8dca09-3828-4ac6-82a7-2a6a63ad2c01")]

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
[assembly: AssemblyVersion("1.5.1.0")]
[assembly: AssemblyFileVersion("1.5.1.0")]
//
//  1.5.1 : no need to have "!dumpheap -stat" in the text, only the "MT    Count    TotalSize Class Name" header
//
//  1.5.0 : [BUG] check for invalid "!dumpheap -stat" from clipboard
//          remove dump file drag & drop with COM objects installed with DebugDiag: it is now implemented with ClrMD
//  1.4.2 : [BUG] count instances with an Int64 because Int32 is not enough for Criteo...
//  1.4.1 : add startup checks for environment variables and DebugDiag
//        : show build version number in About Box
//        : add 2012 in copyrights
//  1.4   : directly load dump files to call !dumpheap -stat behind the scene
//  1.3.2 : ???
//  1.3.1 : add charts title
//          remove Compare button et handle check box state change instead
//          automatically add the version to the main window title
//          make the "Sorted List" tab the default one instead of "Raw"
//  1.3   : handle list of snapshots
//  1.2   : show sortable lists of diffs + Filtered sub list
//  1.1   : listen to the Clipboard chain
//  1.0   : initial version
//
//
