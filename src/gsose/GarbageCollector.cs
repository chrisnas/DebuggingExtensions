using ClrMDStudio;
using RGiesecke.DllExport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ClrMDExports;
using Microsoft.Diagnostics.Runtime;

namespace gsose
{
    public partial class DebuggerExtensions
    {
        [DllExport("gci")]
        public static void gci(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, GCCommands.OnGCInfo);
        }
        [DllExport("gcinfo")]
        public static void gcinfo(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, GCCommands.OnGCInfo);
        }
        [DllExport("GCInfo")]
        public static void GCInfo(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, GCCommands.OnGCInfo);
        }
    }

    internal class GCCommands
    {
        public static void OnGCInfo(ClrRuntime runtime, string args)
        {
            var command = new GCCommands();
            command.GetGCInfo(runtime, args);
        }

        private void GetGCInfo(ClrRuntime runtime, string args)
        {
            if (!runtime.Heap.CanWalkHeap)
            {
                Console.WriteLine("Impossible to walk managed heap...");
                return;
            }

            var showPinned = (args.Contains("-pinned"));
            var showStats = (args.Contains("-stat")) || !showPinned;  // show it by default if nothing else has been asked
            var helper = new ClrMDHelper(runtime);
            var segments = helper.ComputeGCSegments(showPinned);

            ListGenerations(runtime.Heap, segments, showStats, showPinned);
        }

        private void ListGenerations(ClrHeap heap, IReadOnlyList<SegmentInfo> segments, bool showStats, bool showPinned)
        {
            var sb = new StringBuilder(8 * 1024 * 1024);
            for (int currentSegment = 0; currentSegment < segments.Count; currentSegment++)
            {
                var segment = segments[currentSegment];
                var generations = segment.Generations.OrderBy(g => g.Start).ToList();
                Console.WriteLine($"\r\n{segment.Number} - {generations.Count} generations");

                for (int currentGeneration = 0; currentGeneration < generations.Count; currentGeneration++)
                {
                    var generation = generations[currentGeneration];                                                                                                                                //      V---- up to 99 GB
                    Console.WriteLine($"   {GetGenerationType(generation)} | {generation.Start.ToString("X")} - {generation.End.ToString("X")} ({(generation.End - generation.Start).ToString("N0").PadLeft(14)})");

                    if (showStats)
                    {
                        sb.Clear();

                        ShowStatsForGenerationInSegment(heap, generation, sb);
                        Console.WriteLine(sb.ToString());
                    }
                    if (showPinned)
                    {
                        sb.Clear();
                        var pinnedObjects = generation.PinnedObjects;
                        for (int currentPinnedObject = 0; currentPinnedObject < pinnedObjects.Count; currentPinnedObject++)
                        {
                            var pinnedObject = pinnedObjects[currentPinnedObject];
                            sb.AppendFormat("          {0,11} | <link cmd=\"!do {1:x}\">{1:x}</link> {2}\r\n",
                                pinnedObject.handle.HandleType,
                                pinnedObject.handle.Object,
                                pinnedObject.typeDescription
                            );
                        }
                        Console.WriteLine(sb.ToString());
                    }
                }
            }
        }


        class TypeEntry
        {
            public string TypeName;
            public int Count;
            public ulong Size;
        }

        private void ShowStatsForGenerationInSegment(ClrHeap heap, GenerationInSegment generation, StringBuilder sb)
        {
            var statistics = new Dictionary<string, TypeEntry>(128);
            int objectCount = 0;
            for (int i = 0; i < generation.InstancesAddresses.Length; i++)
            {
                var address = generation.InstancesAddresses[i];
                var type = heap.GetObjectType(address);
                var name = GetTypeName(type);

                ulong size = type.GetSize(address);

                if (!statistics.TryGetValue(name, out var entry))
                {
                    entry = new TypeEntry()
                    {
                        TypeName = type.Name,
                        Size = 0
                    };
                    statistics[name] = entry;
                }
                entry.Count++;
                entry.Size += size;
                objectCount++;
            }

            var sortedStatistics =
                from entry in statistics.Values
                orderby entry.Size descending
                select entry;
            Console.WriteLine("         {0,12} {1,12} {2}", "Count", "TotalSize", "Class Name");
            foreach (var entry in sortedStatistics)
                Console.WriteLine($"         {entry.Size,12:D} {entry.Count,12:D} {entry.TypeName}");
            Console.WriteLine($"         Total {objectCount} objects");
        }

        readonly Dictionary<ClrType, string> TypeNames = new Dictionary<ClrType, string>();
        private string GetTypeName(ClrType type)
        {
            if (!TypeNames.TryGetValue(type, out var typeName))
            {
                typeName = type.Name;
                TypeNames[type] = typeName;
            }

            return typeName;
        }

        private string GetGenerationType(GenerationInSegment generation)
        {
            return GetGenerationType(generation.Generation);
        }

        private string GetGenerationType(int generation)
        {
            return (generation == 3) ? " LOH" : $"gen{generation}";
        }
    }
}
