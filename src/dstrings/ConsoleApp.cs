using ClrMDStudio;
using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dstrings
{
    public static class ConsoleApp
    {
        public static void Run(string name, string version, string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp(name, version, "Missing dump file path or process ID...");
                return;
            }

            int countThreshold = 128;
            int sizeThreshold = 100*1024;
            int lengthLimit = 64;
            string input = "";
            string dacFilename = "";
            for (int index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                if (arg == "-c")
                {
                    index++;
                    if (index == args.Length)
                    {
                        ShowHelp(name, version, "Missing string count threshold...");
                        return;
                    }

                    if (!int.TryParse(args[index], out countThreshold))
                    {
                        ShowHelp(name, version, $"Invalid string count threshold '{args[index]}'...");
                        return;
                    }
                }
                else
                if (arg == "-s")
                {
                    index++;
                    if (index == args.Length)
                    {
                        ShowHelp(name, version, "Missing string size threshold...");
                        return;
                    }

                    if (!int.TryParse(args[index], out sizeThreshold))
                    {
                        ShowHelp(name, version, $"Invalid string size threshold '{args[index]}'...");
                        return;
                    }
                }
                else
                if (arg == "-l")
                {
                    index++;
                    if (index == args.Length)
                    {
                        ShowHelp(name, version, "Missing string length threshold...");
                        return;
                    }

                    if (!int.TryParse(args[index], out lengthLimit))
                    {
                        ShowHelp(name, version, $"Invalid string length threshold '{args[index]}'...");
                        return;
                    }
                }
                else
                if (arg == "--dac")
                {
                    index++;
                    if (index == args.Length)
                    {
                        ShowHelp(name, version, "Missing dac file...");
                        return;
                    }

                    if (!File.Exists(args[index]))
                    {
                        ShowHelp(name, version, $"Dac file not found: '{args[index]}'...");
                        return;
                    }

                    dacFilename = args[index];
                }
                else
                {
                    input = arg;
                }
            }

            DataTarget target;
            // attach to live process
            if (int.TryParse(input, out var pid))
            {
                target = DataTarget.AttachToProcess(pid, 1000, AttachFlag.Passive);
            }
            // open memory dump
            else
            {
                if (!File.Exists(input))
                {
                    Console.WriteLine($"'{input}' dump file does not exist...");
                    return;
                }
                target = DataTarget.LoadCrashDump(input);
            }
            ClrRuntime runtime = (dacFilename == "") 
                ? target.ClrVersions[0].CreateRuntime()
                : target.ClrVersions[0].CreateRuntime(dacFilename)
                ;

            AnalyzeStrings(runtime, countThreshold, sizeThreshold, lengthLimit);
        }

        private static void AnalyzeStrings(ClrRuntime runtime, int countThreshold, int sizeThreshold, int lengthLimit)
        {
            var helper = new ClrMDHelper(runtime);
            var results = helper.ComputeDuplicatedStringsStatistics();
            ShowGenStats(results.Stats, results.TotalHeapSize);
            ShowDuplicatedString(results.Strings, countThreshold, sizeThreshold, lengthLimit);
        }

        static string[] rowHeaders = new string[4]
        {
            "0",
            "1",
            "2",
            "loh",
        };

        const string PercentFormat = "{0:##.#}%";
        private static void ShowGenStats(GenStatistics[] stats, ulong totalHeapSize)
        {
            Console.WriteLine("Gen     Size   DupSize%   GenSize%     Count   DupCount%   GenCount");
            Console.WriteLine("-------------------------------------------------------------------");
            for (int gen = 0; gen < stats.Length; gen++)
            {
                var size = stats[gen].Size / 1024;
                var sizeOutput = (size == 0) ? "-" : size.ToString() + "k";
                var dupSize = (stats[gen].Size == 0) ? 0 : stats[gen].DuplicatedSize * 100m / stats[gen].Size;
                var dupSizeOutput = (dupSize == 0) ? "-" : string.Format(PercentFormat, dupSize);
                var genSize = (stats[gen].TotalSize == 0) ? 0 : stats[gen].DuplicatedSize / stats[gen].TotalSize;
                var genSizeOutput = (genSize == 0) ? "-" : string.Format(PercentFormat, genSize);
                var count = stats[gen].Count;
                var countOutput = (count == 0) ? "-" : count.ToString();
                var dupCount = (stats[gen].Count == 0) ? 0 : stats[gen].DuplicatedCount * 100m / stats[gen].Count;
                var dupCountOutput = (dupCount == 0) ? "-" : string.Format(PercentFormat, dupCount);
                
                Console.WriteLine(
                    $"{rowHeaders[gen],3}  {sizeOutput,7}     {dupSizeOutput,5}    {genSizeOutput,5}      {countOutput,7}     {dupCountOutput,5}     {stats[gen].TotalCount}");
            }
            Console.WriteLine("-------------------------------------------------------------------");
            Console.WriteLine();
        }

        private static void ShowDuplicatedString(Dictionary<string, int> strings, int countThreshold, int sizeThreshold, int lengthLimit)
        {
            int totalSize = 0;

            // sort by size taken by the instances of string
            Console.WriteLine("_________________________________________________________________________________");
            Console.WriteLine("  Size   Count  String");
            foreach (var element in strings
                .Where(s => 
                    (s.Value > countThreshold) || 
                    (s.Key.Length * 2 * s.Value > sizeThreshold)
                    )
                .OrderBy(s => s.Value * s.Key.Length))
            {
                var size = (2 * element.Value * element.Key.Length)/1024;
                var count = element.Value;
                var s = element.Key.Substring(0, Math.Min(element.Key.Length, lengthLimit)).Replace("\n", "## ").Replace("\r", " ##");
                Console.WriteLine($"{size,5}k {count,7}  {s}");

                totalSize += 2 * element.Value * element.Key.Length;
            }
        }

        static void ShowHelp(string name, string version, string message)
        {
            Console.WriteLine(string.Format(Header, name, version));
            if (!string.IsNullOrEmpty(message))
            {
                Console.WriteLine();
                Console.WriteLine(message);
            }
            Console.WriteLine();
            Console.WriteLine(Help, name);
        }

        private static string Header =
            "{0} v{1} - Duplicated Strings" + Environment.NewLine +
            "by Christophe Nasarre" + Environment.NewLine +
            "Displays statistics about duplicated strings in a process (live or dump file)";
        private static string Help =
            "Usage:  {0} <dump file path or process ID>" + Environment.NewLine +
            "           [-c <count threshold for display>;  128 occurences by default]" + Environment.NewLine +
            "           [-s <size threshold for isplay>;   100KB by default]" + Environment.NewLine +
            "           [-l <string length limit to display>; 64 characters by default]" + Environment.NewLine +
            "           [--dac <file path>; not needed with pid]" + Environment.NewLine +
            "";
    }
}
