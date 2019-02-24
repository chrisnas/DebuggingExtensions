using ClrMDStudio;
using Microsoft.Diagnostics.Runtime;
using RGiesecke.DllExport;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using ClrMDExports;

namespace gsose
{
    public partial class DebuggerExtensions
    {
        [DllExport("po")]
        public static void po(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, OnPinnedObjects);
        }
        [DllExport("pinnedobjects")]
        public static void pinnedobjects(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, OnPinnedObjects);
        }
        [DllExport("PinnedObjects")]
        public static void PinnedObjects(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, OnPinnedObjects);
        }

        private static void OnPinnedObjects(ClrRuntime runtime, string args)
        {
            int minInstanceCount;
            if (!int.TryParse(args, out minInstanceCount))
                minInstanceCount = 1;

            // Use ClrMD as normal, but ONLY cache the copy of ClrRuntime (this.Runtime).  All other
            // types you get out of ClrMD (such as ClrHeap, ClrTypes, etc) should be discarded and
            // reobtained every run.
            ClrHeap heap = runtime.Heap;

            // Console.WriteLine now writes to the debugger.

            ClrMDHelper helper = new ClrMDHelper(runtime);

            try
            {
                var pinnedObjectsCount = 0;

                foreach (var generation in helper.ComputePinnedObjects())
                {
                    if (generation.Number != 3)
                    {
                        Console.WriteLine($"Gen{generation.Number}: {generation.HandleCount}");
                    }
                    else
                    {
                        Console.WriteLine($"LOH: {generation.HandleCount}");
                    }

                    foreach (var type in generation.Types)
                    {
                        var handles = generation.GetHandles(type).OrderBy(h => h.Object).ToList();
                        pinnedObjectsCount += handles.Count;

                        // show only types with more than a threshold instances just to avoid swamping the output with global pinned handles (especially for WPF applications)
                        if (handles.Count < minInstanceCount)
                            continue;

                        Console.WriteLine($"   {type} : {handles.Count}");
                        for (int i = 0; i < handles.Count; i++)
                        {
                            Console.WriteLine(string.Format("   - {0,11} | <link cmd=\"!do {1:x}\">{1:x}</link>", handles[i].HandleType, handles[i].Object));
                        }
                    }
                }
                Console.WriteLine("-------------------------------------------------------------------------");
                Console.WriteLine($"Total: {pinnedObjectsCount} pinned object");
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
            }
        }
    }
}
