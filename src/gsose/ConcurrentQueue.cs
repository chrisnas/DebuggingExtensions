using System;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime;
using RGiesecke.DllExport;

namespace gsose
{
    public partial class DebuggerExtensions
    {
        [DllExport("dcq")]
        public static void dcq(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnDumpConcurrentQueue(client, args);
        }

        [DllExport("DumpConcurrentQueue")]
        public static void DumpConcurrentQueue(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnDumpConcurrentQueue(client, args);
        }

        public static void OnDumpConcurrentQueue(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
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

            ShowConcurrentQueue(address);
        }

        private static void ShowConcurrentQueue(ulong address)
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
                var cq = heap.GetProxy(address);
                var head = cq.m_head;

                // TODO: implement concurrent queue dump
                Console.WriteLine("Not yet implemented...");
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("this is not a concurrent dictionary");
            }
        }
    }
}
