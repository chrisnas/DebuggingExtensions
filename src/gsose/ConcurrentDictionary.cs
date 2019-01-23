using System;
using System.Linq;
using System.Runtime.InteropServices;
using ClrMDStudio;
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
                // different implementations between .NET Core and .NET Framework
                var helper = new ClrMDHelper(Runtime);

                var cd = heap.GetProxy(address);
                Console.WriteLine($"{cd.GetClrType().Name}");
                var isNetCore = helper.IsNetCore();
                if (isNetCore)
                {
                    var buckets = cd._tables._buckets;
                    Console.WriteLine($" {buckets.Length} buckets");
                }
                else
                {
                    var buckets = cd.m_tables.m_buckets;
                    Console.WriteLine($" {buckets.Length} buckets");
                }

                foreach (var keyValuePair in ClrMDHelper.EnumerateConcurrentDictionary(cd, isNetCore))
                {
                    // in case of string, numbers, and bool, it is possible to marshal the key/value "value"
                    var key = keyValuePair.Item1;
                    var value = keyValuePair.Item2;

                    Console.WriteLine($"{GetOutput(key)} = {GetOutput(value)}");
                }
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("this is not a concurrent dictionary");
            }
        }

        private static string GetOutput(dynamic obj)
        {
            // try to get the value (numbers, bool and string)
            // look at GetValueAtAddress() in ClrMD to see how values are marshaled
            var marshalableTypesPrefixes = new string[]
            {
                "System.Int",
                "System.UInt",
                "System.Double",
                "System.Float",
                "System.Boolean",
                "System.Char",
                "System.String",
            };

            if (obj == null)
            {
                return $"null";
            }

            var typeName = obj.GetType().FullName;
            if (typeName == "DynaMD.DynamicProxy")
            {
                var value = (ulong)obj;
                ClrType type = obj.GetClrType();
                typeName = type.Name;
                return $"<link cmd =\"!do {value:X}\">0x{value:X16}</link> ({typeName})";
            }
            else
            {
                if (marshalableTypesPrefixes.Any(prefix => typeName.StartsWith(prefix)))
                {
                    return $"{obj}";
                }
                else
                {
                    // I can't imagine when this could happen but who knows
                    var address = obj;
                    return $"<link cmd =\"!do {address:X}\">0x{address:X16}</link>";
                }
            }
        }
    }
}