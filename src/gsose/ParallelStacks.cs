using System;
using System.Runtime.InteropServices;
using ClrMDExports;
using Microsoft.Diagnostics.Runtime;
using ParallelStacks;
using RGiesecke.DllExport;

namespace gsose
{
    public partial class DebuggerExtensions

    {
        [DllExport("pstacks")]
        public static void pstacks(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, OnParallelStacks);
        }
        [DllExport("parallelstacks")]
        public static void parallelstacks(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, OnParallelStacks);
        }
        [DllExport("ParallelStacks")]
        public static void ParallelStacks(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, OnParallelStacks);
        }

        private static void OnParallelStacks(ClrRuntime runtime, string args)
        {
            var ps = ParallelStack.Build(runtime);
            if (ps == null)
            {
                return;
            }

            // display parallel stacks
            Console.WriteLine();
            foreach (var stack in ps.Stacks)
            {
                Console.Write("________________________________________________");
                stack.WriteToConsole(useDml:true);
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
            }

            Console.WriteLine($"==> {ps.ThreadIds.Count} threads with {ps.Stacks.Count} roots{Environment.NewLine}");
        }
    }
}
