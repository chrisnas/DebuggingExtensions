using ClrMDStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ClrMDExports;
using Microsoft.Diagnostics.Runtime;
using RGiesecke.DllExport;

namespace gsose
{
    public partial class DebuggerExtensions
    {
        [DllExport("ti")]
        public static void ti(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, OnTimerInfo);
        }
        [DllExport("timerinfo")]
        public static void timerinfo(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, OnTimerInfo);
        }
        [DllExport("TimerInfo")]
        public static void TimerInfo(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            DebuggingContext.Execute(client, args, OnTimerInfo);
        }


        class TimerStat
        {
            public uint Period { get; set; }
            public String Line { get; set; }
            public int Count { get; set; }
        }

        private static void OnTimerInfo(ClrRuntime runtime, string args)
        {
            // Use ClrMD as normal, but ONLY cache the copy of ClrRuntime (this.Runtime).  All other
            // types you get out of ClrMD (such as ClrHeap, ClrTypes, etc) should be discarded and
            // reobtained every run.
            ClrHeap heap = runtime.Heap;

            // Console.WriteLine now writes to the debugger.

            ClrMDHelper helper = new ClrMDHelper(runtime);

            try
            {
                Dictionary<string, TimerStat> stats = new Dictionary<string, TimerStat>(64);
                int totalCount = 0;
                foreach (var timer in helper.EnumerateTimers().OrderBy(t => t.Period))
                {
                    totalCount++;

                    string line = string.Intern(GetTimerString(timer));
                    string key = string.Intern(string.Format(
                        "@{0,8} ms every {1,8} ms | {2} ({3}) -> {4}",
                        timer.DueTime.ToString(),
                        (timer.Period == 4294967295) ? "  ------" : timer.Period.ToString(),
                        timer.StateAddress.ToString("X16"),
                        timer.StateTypeName,
                        timer.MethodName
                        ));

                    TimerStat stat;
                    if (!stats.ContainsKey(key))
                    {
                        stat = new TimerStat()
                        {
                            Count = 0,
                            Line = line,
                            Period = timer.Period
                        };
                        stats[key] = stat;
                    }
                    else
                    {
                        stat = stats[key];
                    }
                    stat.Count = stat.Count + 1;

                    Console.WriteLine(line);
                }

                // create a summary
                Console.WriteLine("\r\n   " + totalCount.ToString() + " timers\r\n-----------------------------------------------");
                foreach (var stat in stats.OrderBy(kvp => kvp.Value.Count))
                {
                    Console.WriteLine(string.Format(
                        "{0,4} | {1}",
                        stat.Value.Count.ToString(),
                        stat.Value.Line
                    ));
                }
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
            }
        }

        static string GetTimerString(TimerInfo timer)
        {
            if (timer.StateAddress != 0)
            {
                return string.Format(
                    "<link cmd=\"!do {0}\">0x{0}</link> @{1,8} ms every {2,8} ms |  <link cmd=\"!do {3}\">0x{3}</link> ({4}) -> {5}",
                    timer.TimerQueueTimerAddress.ToString("X16"),
                    timer.DueTime.ToString(),
                    (timer.Period == 4294967295) ? "  ------" : timer.Period.ToString(),
                    timer.StateAddress.ToString("X16"),
                    timer.StateTypeName,
                    timer.MethodName
                );
            }
            else
            {
                return string.Format(
                    "<link cmd=\"!do {0}\">0x{0}</link> @{1,8} ms every {2,8} ms |  0x{3} ({4}) -> {5}",
                    timer.TimerQueueTimerAddress.ToString("X16"),
                    timer.DueTime.ToString(),
                    (timer.Period == 4294967295) ? "  ------" : timer.Period.ToString(),
                    timer.StateAddress.ToString("X16"),
                    timer.StateTypeName,
                    timer.MethodName
                );
            }
        }
    }
}