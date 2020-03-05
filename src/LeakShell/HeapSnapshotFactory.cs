using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LeakShell
{
    public class HeapSnapshotFactory
    {
        private static readonly InvalidOperationException InvalidFormatException =
            new InvalidOperationException(
                "Invalid !heapstat -stat list format.\r\n   Don't forget to copy the whole output starting from \"MT    Count    TotalSize Class Name\"; including the final \"Total ... objects\" line.");

        /*
        !dumpheap -stat
        PDB symbol for clr.dll not loaded
        total 0 objects
        Statistics:
                MT    Count    TotalSize Class Name
        651d7ffc        1           12 System.Configuration.Internal.ConfigurationManagerInternal
        651d7e2c        1           12 System.Configuration.ConfigurationPermission
        651d7ddc        1           12 System.Configuration.BaseConfigurationRecord+IndirectLocationInputComparer
        651d7bd8        1           12 System.Configuration.ConfigurationValues+EmptyCollection+EmptyCollectionEnumerator
        5fc06c28     7111       300980 System.Object[]
        5fc4f9ac     6581       381508 System.String
        Total 46832 objects
        */

        private const string Header = "MT    Count    TotalSize Class Name";
        public static HeapSnapshot CreateFromHeapStat(string dumpheap)
        {
            // sanity checks
            if (string.IsNullOrEmpty(dumpheap))
            {
                throw InvalidFormatException;
            }

            if (!dumpheap.Contains(Header))
            {
                throw InvalidFormatException;
            }

            DumpHeapParsingState state = DumpHeapParsingState.Init;
            using (TextReader reader = new StringReader(dumpheap))
            {
                HeapSnapshot snapshot = new HeapSnapshot();
                while (true)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line))
                    {
                        if (state == DumpHeapParsingState.End)
                            return snapshot;

                        throw InvalidFormatException;
                    }

                    switch (state)
                    {
                        case DumpHeapParsingState.Init:
                            if (line.Contains(Header))
                            {
                                state = DumpHeapParsingState.TypeEntries;
                            }
                            break;

                        case DumpHeapParsingState.TypeEntries:
                            state = ParseAndAddTypeEntry(line, snapshot);
                            break;

                        case DumpHeapParsingState.End:
                            return snapshot;

                        case DumpHeapParsingState.Error:
                            return null;

                        default:
                            throw new InvalidOperationException($"Invalid parsing state {(int)state}.");
                    }
                }
            }
        }

        public static HeapSnapshot CreateFromDumpFile(string filename)
        {
            return HeapDumper.Compute(filename);
        }

        public static HeapSnapshot Compare(HeapSnapshot reference, HeapSnapshot current)
        {
            HeapSnapshot compare = new HeapSnapshot();

            // loop on each entry in the current snapshot and compare with the reference
            foreach (var item in current.TypeStats)
            {
                // look for this class in the reference
                if (reference.TypeStats.ContainsKey(item.Key))
                {
                    TypeEntry referenceEntry = reference.TypeStats[item.Key];
                    if (referenceEntry.Count < item.Value.Count)
                    {
                        var entry = new TypeEntry();
                        entry.Name = string.Intern(referenceEntry.Name);
                        entry.MethodTable = referenceEntry.MethodTable;
                        entry.Count = item.Value.Count - referenceEntry.Count;
                        entry.TotalSize = item.Value.TotalSize - referenceEntry.TotalSize;

                        compare.TypeStats.Add(HeapSnapshot.GetKey(entry), entry);

                        compare.ObjectCount += entry.Count;
                        compare.Size += entry.TotalSize;
                    }
                }
                else  // don't forget the case where brand new type instances appear in current snapshot
                {
                    var entry = new TypeEntry();
                    entry.Name = string.Intern(item.Value.Name);
                    entry.MethodTable = item.Value.MethodTable;
                    entry.Count = item.Value.Count;
                    entry.TotalSize = item.Value.TotalSize;

                    compare.TypeStats.Add(HeapSnapshot.GetKey(entry), entry);

                    compare.ObjectCount += entry.Count;
                    compare.Size += entry.TotalSize;
                }
            }

            return compare;
        }

        [Conditional("DEBUG")]
        public static void DumpSnapshot(HeapSnapshot snapshot)
        {
            Debug.WriteLine("HeapSnapshot at {0}", snapshot.TimeStamp);
            Debug.WriteLine("  {0} objects for {1} Mb", snapshot.ObjectCount, snapshot.Size / 1024 / 1024);
            var entries =
                from e in snapshot.TypeStats.AsParallel()
                orderby e.Value.Name
                select e.Value
                ;
            foreach (var entry in entries)
            {
                Debug.WriteLine("    {0}", entry);
            }
        }

        private static readonly string SPACE = " ";
        private static readonly char SPACECHAR = ' ';

        private static DumpHeapParsingState ParseAndAddTypeEntry(string line, HeapSnapshot snapshot)
        {
            if (line.StartsWith("Total"))
            {
                return DumpHeapParsingState.End;
            }

            //       MT    Count    TotalSize Class Name
            // 651d7ffc        1           12 System.Configuration.Internal.ConfigurationManagerInternal
            //
            // or in x64
            //
            // 0x000007ff01221408    1,448       57,920 System.Xml.XmlQualifiedName
            //
            TypeEntry entry = new TypeEntry();
            var pos = 0;
            var end = 0;
            string field;

            // 1. look for the MT
            end = line.IndexOf(SPACE);
            if (end == -1)
            {
                Debug.WriteLine("impossible to find the end of the MT field");
                return DumpHeapParsingState.Error;
            }
            field = line.Substring(pos, end - pos);
            entry.MethodTable = field;

            if (!SkipSpaces(line, ref end))
            {
                Debug.WriteLine("impossible to find the start of the Count field");
                return DumpHeapParsingState.Error;
            }
            pos = end;

            // 2. look for the count
            end = line.IndexOf(SPACE, pos);
            if (end == -1)
            {
                Debug.WriteLine("impossible to find the end of the Count field");
                return DumpHeapParsingState.Error;
            }
            field = line.Substring(pos, end - pos);

            if (!long.TryParse(GetNumberFromString(field), out var count))
            {
                Debug.WriteLine("invalid decimal value for the Count field");
                return DumpHeapParsingState.Error;
            }
            entry.Count = count;

            if (!SkipSpaces(line, ref end))
            {
                Debug.WriteLine("impossible to find the start of the TotalSize field");
                return DumpHeapParsingState.Error;
            }
            pos = end;


            // 3. look for the total size
            end = line.IndexOf(SPACE, pos);
            if (end == -1)
            {
                Debug.WriteLine("impossible to find the end of the MT field");
                return DumpHeapParsingState.Error;
            }
            field = line.Substring(pos, end - pos);
            if (!long.TryParse(GetNumberFromString(field), out var totalSize))
            {
                Debug.WriteLine("invalid decimal value for the TotalSize field");
                return DumpHeapParsingState.Error;
            }
            entry.TotalSize = totalSize;

            if (!SkipSpaces(line, ref end))
            {
                Debug.WriteLine("impossible to find the start of the TotalSize field");
                return DumpHeapParsingState.Error;
            }
            pos = end;


            // 4. look for the class name
            field = line.Substring(pos);
            entry.Name = string.Intern(field);

            snapshot.ObjectCount += entry.Count;
            snapshot.Size += entry.TotalSize;

            string key = HeapSnapshot.GetKey(entry);
            if (!snapshot.TypeStats.ContainsKey(key))
            {
                snapshot.TypeStats.Add(key, entry);
            }
            else
            {
                Debug.Fail("This should never happen");
                throw new InvalidOperationException($"Impossible to find {key} for the entry");
            }

            return DumpHeapParsingState.TypeEntries;
        }

        private static bool SkipSpaces(string line, ref int end)
        {
            while (true)
            {
                if (end == line.Length - 1)
                {
                    return false;
                }
                if (line[end] == SPACECHAR)
                {
                    end++;
                }
                else
                {
                    break;
                }
            }

            return true;
        }

        private static string GetNumberFromString(string field)
        {
            // remove any non numeric character
            StringBuilder sb = new StringBuilder();
            foreach (char c in field)
            {
                if (char.IsDigit(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        enum DumpHeapParsingState
        {
            Init = 0,
            TypeEntries,
            End,
            Error,
        }
    }
}
