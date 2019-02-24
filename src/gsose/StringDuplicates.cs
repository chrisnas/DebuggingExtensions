using ClrMDStudio;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using ClrMDExports;
using Microsoft.Diagnostics.Runtime;
using RGiesecke.DllExport;

namespace gsose
{
    public partial class DebuggerExtensions
    {
        [DllExport("sd")]
        public static void sd(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, OnStringDuplicates);
        }
        [DllExport("stringduplicates")]
        public static void stringduplicates(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, OnStringDuplicates);
        }
        [DllExport("StringDuplicates")]
        public static void StringDuplicates(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, OnStringDuplicates);
        }

        private static void OnStringDuplicates(ClrRuntime runtime, string args)
        {
            // Use ClrMD as normal, but ONLY cache the copy of ClrRuntime (this.Runtime).  All other
            // types you get out of ClrMD (such as ClrHeap, ClrTypes, etc) should be discarded and
            // reobtained every run.
            ClrHeap heap = runtime.Heap;

            // Console.WriteLine now writes to the debugger.

            ClrMDHelper helper = new ClrMDHelper(runtime);

            // extract the threshold (= min number of duplicates from which a string appears in the list)
            int minCountThreshold = 100;
            if (args != null)
            {
                string[] commands = args.Split(' ');
                int.TryParse(commands[0], out minCountThreshold);
            }

            try
            {
                var strings = helper.ComputeDuplicatedStrings();
                if (strings == null)
                {
                    Console.WriteLine("Impossible to enumerate strings...");
                    return;
                }

                int totalSize = 0;

                // sort by size taken by the instances of string
                foreach (var element in strings.Where(s => s.Value > minCountThreshold).OrderBy(s => 2 * s.Value * s.Key.Length))
                {
                    Console.WriteLine(string.Format(
                        "{0,8} {1,12} {2}",
                        element.Value.ToString(),
                        (2 * element.Value * element.Key.Length).ToString(),
                        element.Key.Replace("\n", "## ").Replace("\r", " ##")
                        ));
                    totalSize += 2 * element.Value * element.Key.Length;
                }

                Console.WriteLine("-------------------------------------------------------------------------");
                Console.WriteLine(string.Format("         {0,12} MB", (totalSize / (1024 * 1024)).ToString()));
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
            }
        }
    }
}