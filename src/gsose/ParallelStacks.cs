using System;
using System.Runtime.InteropServices;
using ClrMDExports;
using Microsoft.Diagnostics.Runtime;
using ParallelStacks.Runtime;
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

        // Needed to add Show to avoid conflict with ParallelStack.Runtime
        [DllExport("ParallelStacks")]
        public static void ShowParallelStacks(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, OnParallelStacks);
        }

        private static void OnParallelStacks(ClrRuntime runtime, string args)
        {
            var ps = ParallelStacks.Runtime.ParallelStack.Build(runtime);
            if (ps == null)
            {
                return;
            }

            // display parallel stacks
            var visitor = new ConsoleRenderer(useDml: true);
            Console.WriteLine();
            foreach (var stack in ps.Stacks)
            {
                Console.Write("________________________________________________");
                stack.Render(visitor);
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
            }

            Console.WriteLine($"==> {ps.ThreadIds.Count} threads with {ps.Stacks.Count} roots{Environment.NewLine}");
        }
    }
}
