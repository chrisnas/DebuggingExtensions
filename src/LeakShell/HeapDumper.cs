using System;
using Microsoft.Diagnostics.Runtime;

namespace LeakShell
{
    public static class HeapDumper
    {
        public static HeapSnapshot Compute(string dumpFilename)
        {
            DataTarget dataTarget = null;
            try
            {
                dataTarget = DataTarget.LoadCrashDump(dumpFilename);
                var runtime = CreateRuntime(dataTarget);
                if (runtime == null)
                {
                    throw new InvalidOperationException($"Impossible to create ClrRuntime for {dumpFilename}");
                }

                var heap = runtime.Heap;
                if (!heap.CanWalkHeap)
                {
                    throw new InvalidOperationException($"Impossible to walk the heap in {dumpFilename}: maybe taken during a GC.");
                }

                var result = Compute(heap);
                return result;
            }
            catch (Exception x)
            {
                // UI is only expecting InvalidOperationException
                throw new InvalidOperationException($"Error when dumping {dumpFilename} heap: {x.Message}", x);
            }
            finally
            {
                dataTarget?.Dispose();
            }
        }

        private static HeapSnapshot Compute(ClrHeap heap)
        {
            var snapshot = new HeapSnapshot();

            foreach (ulong objAddress in heap.EnumerateObjectAddresses())
            {
                ClrType type = heap.GetObjectType(objAddress);
                if (type == null) continue;

                var mt = type.MethodTable.ToString();
                var name = type.Name;
                var key = HeapSnapshot.GetKey(name, mt);

                if (!snapshot.TypeStats.TryGetValue(key, out var entry))
                {
                    entry = new TypeEntry()
                    {
                        Name = name,
                        MethodTable = mt
                    };

                    snapshot.TypeStats[key] = entry;
                }
                entry.Count = entry.Count + 1;
                snapshot.ObjectCount = snapshot.ObjectCount + 1;

                var size = type.GetSize(objAddress);
                entry.TotalSize = entry.TotalSize + (long)size;
                snapshot.Size = snapshot.Size + (long)size;
            }

            return snapshot;
        }


        private static ClrRuntime CreateRuntime(DataTarget dataTarget)
        {
            // check bitness first
            bool isTarget64Bit = (dataTarget.PointerSize == 8);
            if (Environment.Is64BitProcess != isTarget64Bit)
            {
                throw new InvalidOperationException(
                    $"Architecture mismatch:  trying to load {(isTarget64Bit ? "64 bit" : "32 bit")} dump in {(Environment.Is64BitProcess ? "64 bit" : "32 bit")} application");
            }

            var version = dataTarget.ClrVersions[0];
            var runtime = version.CreateRuntime();
            return runtime;
        }
    }
}
