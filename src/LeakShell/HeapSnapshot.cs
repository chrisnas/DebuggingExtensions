using System;
using System.Collections.Generic;

namespace LeakShell
{
    public class HeapSnapshot
    {
        public HeapSnapshot()
        {
            TypeStats = new SortedDictionary<string, TypeEntry>();
            TimeStamp = DateTime.Now;
        }

        public DateTime TimeStamp { get; }
        public long Size { get; set; }
        public long ObjectCount { get; set; }
        public SortedDictionary<string, TypeEntry> TypeStats { get; }


        // Note: unfortunately, the class name is not enough
        //       so the key should contains the method table address
        public static string GetKey(TypeEntry entry)
        {
            return $"{entry.Name}|{entry.MethodTable}";
        }
        public static string GetKey(string typeName, string methodTable)
        {
            return $"{typeName}|{methodTable}";
        }
    }
}
