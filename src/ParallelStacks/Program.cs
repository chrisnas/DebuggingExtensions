using System;
using System.IO;
using ParallelStacks.Runtime;

namespace ParallelStacks
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp("Missing dump file path or process ID...");
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

            ParallelStack ps;

            var input = args[0];

            // attach to live process
            if (int.TryParse(input, out var pid))
            {
                try
                {
                    ps = ParallelStack.Build(pid, dacFilePath);
                }
                catch (InvalidOperationException x)
                {
                    Console.WriteLine($"Impossible to build call stacks: {x.Message}");
                    return;
                }
            }
            // open memory dump
            else 
            {
                if (!File.Exists(input))
                {
                    Console.WriteLine($"'{input}' does not exist...");
                    return;
                }

                try
                {
                    ps = ParallelStack.Build(input, dacFilePath);
                }
                catch (InvalidOperationException x)
                {
                    Console.WriteLine($"Impossible to build call stacks: {x.Message}");
                    return;
                }
            }

            int threadIDsCountlimit = 4;
            var visitor = new ConsoleRenderer(useDml: false, limit: threadIDsCountlimit);
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
"pstacks v1.3 - Parallel Stacks" + Environment.NewLine +
"by Christophe Nasarre" + Environment.NewLine +
            "Aggregate the threads callstacks a la Visual Studio 'Parallel Stacks'";
        private static string Help = 
"Usage:  pstacks <dump file path or process ID> [dac file path if any]" + Environment.NewLine +
            "";
    }
}
