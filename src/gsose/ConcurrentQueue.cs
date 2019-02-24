using System;
using System.Linq;
using System.Runtime.InteropServices;
using ClrMDExports;
using ClrMDStudio;
using Microsoft.Diagnostics.Runtime;
using RGiesecke.DllExport;

namespace gsose
{
    public partial class DebuggerExtensions
    {
        [DllExport("dcq")]
        public static void dcq(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, OnDumpConcurrentQueue);
        }

        [DllExport("DumpConcurrentQueue")]
        public static void DumpConcurrentQueue(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, OnDumpConcurrentQueue);
        }

        public static void OnDumpConcurrentQueue(ClrRuntime runtime, string args)
        {
            // parse the command argument
            if (string.IsNullOrEmpty(args))
            {
                Console.WriteLine("Missing address of a ConcurrentQueue");
                return;
            }

            var arguments = args.Split(' ');
            var address = arguments[0];
            if (address.StartsWith("0x"))
            {
                // remove "0x" for parsing
                address = address.Substring(2).TrimStart('0');
            }

            // remove the leading 0000 that WinDBG often add in 64 bit
            address = address.TrimStart('0');

            if (!ulong.TryParse(address, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out var reference))
            {
                Console.WriteLine("numeric address value expected");
                return;
            }

            // check parameters
            // -t: show item type
            // 
            var showItemType = false;
            if (arguments.Length > 1)
            {
                showItemType = arguments.Any(arg => arg == "-t");
            }
            ShowConcurrentQueue(runtime, reference, showItemType);
        }

        private static void ShowConcurrentQueue(ClrRuntime runtime, ulong address, bool showItemType)
        {
            var heap = runtime.Heap;
            ClrType t = heap.GetObjectType(address);
            if (t == null)
            {
                Console.WriteLine("this is not a heap object");
                return;
            }

            try
            {
                // different implementations between .NET Core and .NET Framework
                var helper = new ClrMDHelper(runtime);
                var cq = heap.GetProxy(address);
                int count = 0;
                foreach (var item in ClrMDHelper.EnumerateConcurrentQueue(cq, helper.IsNetCore()))
                {
                    count++;
                    var itemAddress = (ulong)item;
                    var type = heap.GetObjectType(itemAddress);
                    var typeName = (type == null) ? "?" : type.Name;
                    if (showItemType)
                        Console.WriteLine($"{count,4} - <link cmd =\"!do {itemAddress:X}\">0x{itemAddress:X16}</link> | {typeName}");
                    else
                        Console.WriteLine($"{count,4} - <link cmd =\"!do {itemAddress:X}\">0x{itemAddress:X16}</link>");
                }
                //Console.WriteLine("---------------------------------------------" + Environment.NewLine + $"{count} items");
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("this is not a ConcurrentQueue");
            }
        }
    }
}
