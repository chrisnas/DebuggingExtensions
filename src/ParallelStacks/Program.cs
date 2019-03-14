using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime;

namespace ParallelStacks
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp("Missing dump file path...");
                return;
            }

            var dumpFile = args[0];

            if (!File.Exists(dumpFile))
            {
                Console.WriteLine($"'{dumpFile}' does not exist...");
                return;
            }
            string dacFilePath = (args.Length >= 2) ? args[1] : null;
            if (dacFilePath != null)
            {
                if (!File.Exists(dacFilePath))
                {
                    Console.WriteLine($"{dacFilePath} file does not exist...");
                    return;
                }
            }

            // collapse stacks
            var ps = BuildParallelStacks(dumpFile, dacFilePath);
            if (ps == null)
            {
                return;
            }

            // display parallel stacks
            Console.WriteLine();
            foreach (var stack in ps.Stacks)
            {
                Console.Write("________________________________________________");
                stack.WriteToConsole();
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
            }

            Console.WriteLine($"==> {ps.ThreadIds.Count} threads with {ps.Stacks.Count} roots{Environment.NewLine}");
        }

        static ParallelStack BuildParallelStacks(string dumpFile, string dacFilePath)
        {
            DataTarget dataTarget = null;
            ParallelStack ps = null;
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    dataTarget = DataTarget.LoadCrashDump(dumpFile);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    dataTarget = DataTarget.LoadCoreDump(dumpFile);
                }
                else
                {
                    Console.WriteLine("Unsupported platform...");
                    return null;
                }

                var runtime = CreateRuntime(dataTarget, dacFilePath);
                if (runtime == null)
                {
                    return null;
                }

                ps = ParallelStack.Build(runtime);
            }
            finally
            {
                dataTarget?.Dispose();
            }

            return ps;
        }

        static ClrRuntime CreateRuntime(DataTarget dataTarget, string dacFilePath)
        {
            // check bitness first
            bool isTarget64Bit = (dataTarget.PointerSize == 8);
            if (Environment.Is64BitProcess != isTarget64Bit)
            {
                Console.WriteLine("Architecture mismatch:  This tool is {0} but target is {1}", Environment.Is64BitProcess ? "64 bit" : "32 bit", isTarget64Bit ? "64 bit" : "32 bit");
                return null;
            }

            var version = dataTarget.ClrVersions[0];
            var runtime = (dacFilePath != null) ? version.CreateRuntime(dacFilePath) : version.CreateRuntime();
            return runtime;
        }


        static void ShowHelp(string message)
        {
            Console.WriteLine(Header);
            if (!string.IsNullOrEmpty(message))
            {
                Console.WriteLine();
                Console.WriteLine(message);
            }
            Console.WriteLine();
            Console.WriteLine(Help);
        }

        private static string Header = 
"pstacks v1.0.1 - Parallel Stacks" + Environment.NewLine +
"by Christophe Nasarre" + Environment.NewLine +
            "Aggregate the threads callstacks a la Visual Studio 'Parallel Stacks'";
        private static string Help = 
"Usage:  pstacks <dump file path> [dac file path if any]" + Environment.NewLine +
            "";
    }
}
