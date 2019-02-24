using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ClrMDExports
{
    internal class Init
    {
        [RGiesecke.DllExport.DllExport("DebugExtensionInitialize")]
        public static int DebugExtensionInitialize(ref uint version, ref uint flags)
        {
            var extensionFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            try
            {
                var file = Path.Combine(extensionFolder, "ClrMDExports.dll");

                var assemblyName = AssemblyName.GetAssemblyName(file);

                ForceAssemblyLoad(assemblyName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not load ClrMDExports: " + ex);
            }

            InitializeDebuggingContext();

            // Set the extension version to 1, which expects exports with this signature:
            //      void _stdcall function(IDebugClient *client, const char *args)
            version = DEBUG_EXTENSION_VERSION(1, 0);
            flags = 0;
            return 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InitializeDebuggingContext()
        {
            Private.Initialization.IsWinDbg = true;
        }

        private static void ForceAssemblyLoad(AssemblyName assemblyName)
        {
            var codeBase = assemblyName.CodeBase;

            if (codeBase.StartsWith("file://"))
            {
                codeBase = codeBase.Substring(8).Replace('/', '\\');
            }

            ResolveEventHandler assemblyResolve = (sender, args) => args.Name == assemblyName.FullName ? Assembly.LoadFrom(codeBase) : null;

            AppDomain.CurrentDomain.AssemblyResolve += assemblyResolve;

            Assembly.Load(assemblyName.FullName);

            AppDomain.CurrentDomain.AssemblyResolve -= assemblyResolve;
        }

        static uint DEBUG_EXTENSION_VERSION(uint major, uint minor)
        {
            return ((((major) & 0xffff) << 16) | ((minor) & 0xffff));
        }
    }
}