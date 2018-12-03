using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime;
using RGiesecke.DllExport;

namespace gsose
{
    public partial class DebuggerExtensions
    {
        [DllExport("dcd")]
        public static void dcd(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnDumpConcurrentDictionary(client, args);
        }

        [DllExport("DumpConcurrentDictionary")]
        public static void DumpConcurrentDictionary(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnDumpConcurrentDictionary(client, args);
        }

        public static void OnDumpConcurrentDictionary(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            // Must be the first thing in our extension.
            if (!InitApi(client))
                return;

            // parse the command argument
            if (args.StartsWith("0x"))
            {
                // remove "0x" for parsing
                args = args.Substring(2).TrimStart('0');
            }

            // remove the leading 0000 that WinDBG often add in 64 bit
            args = args.TrimStart('0');

            if (!ulong.TryParse(args, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out var address))
            {
                Console.WriteLine("numeric address value expected");
                return;
            }

            ShowConcurrentDictionary(address);
        }

        private static void ShowConcurrentDictionary(ulong address)
        {
            var heap = Runtime.Heap;
            ClrType t = heap.GetObjectType(address);
            if (t == null)
            {
                Console.WriteLine("this is not a heap object");
                return;
            }

            try
            {
                var cd = heap.GetProxy(address);
                var buckets = cd.m_tables.m_buckets;
                Console.WriteLine($"{buckets.Length} buckets");

                foreach (var bucket in buckets)
                {
                    if (bucket == null) continue;

                    var key = (int)bucket.m_key;
                    var addr = (ulong)bucket.m_value;
                    var value = heap.GetObjectType(addr).GetValue(addr);

                    Console.WriteLine($"{key} = {value}");
                }
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("this is not a concurrent dictionary");
            }
        }
    }
}