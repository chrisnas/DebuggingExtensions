using ClrMDStudio;
using RGiesecke.DllExport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace gsose
{
    public partial class DebuggerExtensions
    {
        [DllExport("gci")]
        public static void gci(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnGCInfo(client, args);
        }
        [DllExport("gcinfo")]
        public static void gcinfo(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnGCInfo(client, args);
        }
        [DllExport("GCInfo")]
        public static void GCInfo(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnGCInfo(client, args);
        }

        private static void OnGCInfo(IntPtr client, string args)
        {
            // Must be the first thing in our extension.
            if (!InitApi(client))
                return;

            var helper = new ClrMDHelper(Runtime);
            var segments = helper.ComputeGCSegments();

            ListGenerations(segments);
        }

        private static void ListGenerations(IReadOnlyList<SegmentInfo> segments)
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

        private static string GetGenerationType(GenerationInSegment generation)
        {
            return GetGenerationType(generation.Generation);
        }

        private static string GetGenerationType(int generation)
        {
            return (generation == 3) ? " LOH" : $"gen{generation}";
        }
    }
}
