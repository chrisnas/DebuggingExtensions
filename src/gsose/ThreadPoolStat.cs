using ClrMDStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.Runtime;
using RGiesecke.DllExport;

namespace gsose
{
    public partial class DebuggerExtensions
    {
        [DllExport("tpq")]
        public static void tpq(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnTpQueues(client, args);
        }
        [DllExport("tpQueues")]
        public static void tpQueues(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnTpQueues(client, args);
        }
        [DllExport("tpqueues")]
        public static void tpqueues(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnTpQueues(client, args);
        }
        public static void OnTpQueues(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            // Must be the first thing in our extension.
            if (!InitApi(client))
                return;

            Dictionary<string, WorkInfo> _workItems = new Dictionary<string, WorkInfo>();
            int _workItemCount = 0;
            Dictionary<string, WorkInfo> _tasks = new Dictionary<string, WorkInfo>();
            int _taskCount = 0;

            // Use ClrMD as normal, but ONLY cache the copy of ClrRuntime (this.Runtime).  All other
            // types you get out of ClrMD (such as ClrHeap, ClrTypes, etc) should be discarded and
            // reobtained every run.
            ClrThreadPool threadPool = Runtime.ThreadPool;
            ClrHeap heap = Runtime.Heap;

            // Console.WriteLine now writes to the debugger.

            ClrMDHelper helper = new ClrMDHelper(Runtime);

            // The ThreadPool is keeping track of the pending work items into two different areas:
            // - a global queue: stored by ThreadPoolWorkQueue instances of the ThreadPoolGlobals.workQueue static field
            // - several per thread (TLS) local queues: stored in SparseArray<ThreadPoolWorkQueue+WorkStealingQueue> linked from ThreadPoolWorkQueue.allThreadQueues static fields
            // both are using arrays of Task or QueueUserWorkItemCallback
            //
            // NOTE: don't show other thread pool related topics such as timer callbacks or wait objects
            //
            try
            {
                Console.WriteLine("global work item queue________________________________");
                foreach (var item in helper.EnumerateGlobalThreadPoolItems())
                {
                    switch (item.Type)
                    {
                        case ThreadRoot.Task:
                            Console.Write(string.Format("<link cmd=\"!do {0:X}\">0x{0:X16}</link> Task", item.Address));
                            Console.WriteLine(" | " + item.MethodName);
                            UpdateStats(_tasks, item.MethodName, ref _taskCount);
                            break;

                        case ThreadRoot.WorkItem:
                            Console.Write(string.Format("<link cmd=\"!do {0:X}\">0x{0:X16}</link> Work", item.Address));
                            Console.WriteLine(" | " + item.MethodName);
                            UpdateStats(_workItems, item.MethodName, ref _workItemCount);
                            break;

                        default:
                            Console.WriteLine(string.Format("<link cmd=\"!do {0:X}\">0x{0:X16}</link> {1}", item.Address, item.MethodName));
                            break;
                    }
                }

                // look into the local stealing queues in each thread TLS
                // hopefully, they are all stored in static (one per app domain) instance
                // of ThreadPoolWorkQueue.SparseArray<ThreadPoolWorkQueue.WorkStealingQueue>
                //
                Console.WriteLine("\r\nlocal per thread work items_____________________________________");
                try
                {
                    foreach (var item in helper.EnumerateLocalThreadPoolItems())
                    {
                        switch (item.Type)
                        {
                            case ThreadRoot.Task:
                                Console.Write(string.Format("<link cmd=\"!do {0:X}\">0x{0:X16}</link> Task", item.Address));
                                Console.WriteLine(" | " + item.MethodName);
                                UpdateStats(_tasks, item.MethodName, ref _taskCount);
                                break;

                            case ThreadRoot.WorkItem:
                                Console.Write(string.Format("<link cmd=\"!do {0:X}\">0x{0:X16}</link> Work", item.Address));
                                Console.WriteLine(" | " + item.MethodName);
                                UpdateStats(_workItems, item.MethodName, ref _workItemCount);
                                break;

                            default:
                                Console.WriteLine(string.Format("<link cmd=\"!do {0:X}\">0x{0:X16}</link> {1}", item.Address, item.MethodName));
                                break;
                        }
                    }
                }
                finally
                {
                    Console.WriteLine("");

                    // provide a summary sorted by count
                    // tasks first if any
                    if (_tasks.Values.Count > 0)
                    {
                        foreach (var item in _tasks.Values.OrderBy(wi => wi.Count))
                        {
                            Console.WriteLine(string.Format(" {0,4} Task  {1}", item.Count.ToString(), item.Name));
                        }
                        Console.WriteLine(" ----");
                        Console.WriteLine(string.Format(" {0,4}\r\n", _taskCount.ToString()));
                    }

                    // then QueueUserWorkItem next if any
                    if (_workItems.Values.Count > 0)
                    {
                        foreach (var item in _workItems.Values.OrderBy(wi => wi.Count))
                        {
                            Console.WriteLine(string.Format(" {0,4} Work  {1}", item.Count.ToString(), item.Name));
                        }
                        Console.WriteLine(" ----");
                        Console.WriteLine(string.Format(" {0,4}\r\n", _workItemCount.ToString()));
                    }
                }
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
            }
        }


        [DllExport("tpr")]
        public static void tpr(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnTpRunning(client, args);
        }
        [DllExport("tpRunning")]
        public static void tpRunning(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnTpRunning(client, args);
        }
        [DllExport("tprunning")]
        public static void tprunning(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnTpRunning(client, args);
        }
        private static void OnTpRunning(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            // Must be the first thing in our extension.
            if (!InitApi(client))
                return;

            // Use ClrMD as normal, but ONLY cache the copy of ClrRuntime (this.Runtime).  All other
            // types you get out of ClrMD (such as ClrHeap, ClrTypes, etc) should be discarded and
            // reobtained every run.
            ClrThreadPool threadPool = Runtime.ThreadPool;
            ClrHeap heap = Runtime.Heap;

            // Console.WriteLine now writes to the debugger.

            ClrMDHelper helper = new ClrMDHelper(Runtime);


            try
            {
                Console.WriteLine(string.Format(
                    "\r\nCPU = {0}%% for {1} threads (#idle = {2} + #running = {3} | #dead = {4} | #max = {5})",
                    threadPool.CpuUtilization.ToString(),
                    threadPool.TotalThreads.ToString(),
                    threadPool.IdleThreads.ToString(),
                    threadPool.RunningThreads.ToString(),
                    Runtime.Threads.Count(t => t.IsThreadpoolWorker && !t.IsThreadpoolCompletionPort && !t.IsAlive && !t.IsThreadpoolGate && !t.IsThreadpoolTimer && !t.IsThreadpoolWait).ToString(),
                    threadPool.MaxThreads.ToString()
                ));

                // show the running worker threads
                DumpRunningThreadpoolThreads(helper);
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
            }
        }


        private class WorkInfo
        {
            public string Name { get; set; }
            public int Count { get; set; }
        }
        private class ThreadDistributionItem
        {
            public string Key { get; set; }
            public int Count { get; set; }
        }

        private static void DumpRunningThreadpoolThreads(ClrMDHelper helper)
        {
            Dictionary<string, ThreadDistributionItem> distribution = new Dictionary<string, ThreadDistributionItem>(8 * 1024);

            Console.WriteLine("\r\n  ID ThreadOBJ        Locks  Details");
            Console.WriteLine("-----------------------------------------------------------------------------------");
            foreach (var thread in Runtime.Threads.Where(t => t.IsThreadpoolWorker).OrderBy(t => (t.LockCount > 0) ? -1 : (!t.IsAlive ? t.ManagedThreadId + 10000 : t.ManagedThreadId)))
            {
                string details = string.Intern(GetCallStackInfo(helper, thread));

                if (thread.IsAlive)
                {
                    Console.WriteLine(string.Format(
                        "{0,4} <link cmd=\"~~[{1}]e!ClrStack\">{2}</link>  {3}  {4}",
                        thread.ManagedThreadId.ToString(),
                        thread.OSThreadId.ToString("X"),
                        thread.Address.ToString("X16"),
                        ((thread.LockCount > 0) ? thread.LockCount.ToString("D4") : "    "),
                        details
                    ));
                }
                else
                {
                    Console.WriteLine(string.Format(
                        "{0,4} {1}  {2}  {3}",
                        thread.ManagedThreadId.ToString(),
                        thread.Address.ToString("X16"),
                        ((thread.LockCount > 0) ? thread.LockCount.ToString("D4") : "    "),
                        details
                    ));
                }

                ThreadDistributionItem state;
                if (distribution.ContainsKey(details))
                {
                    state = distribution[details];
                }
                else
                {
                    state = new ThreadDistributionItem()
                    {
                        Key = details,
                        Count = 0
                    };
                    distribution[details] = state;
                }
                state.Count++;
            }

            // build a summary
            if (distribution.Values.Count > 0)
            {
                Console.WriteLine("\r\n____________________________________________________________________________________________________\r\nCount Details\r\n----------------------------------------------------------------------------------------------------");
                int total = 0;
                foreach (var item in distribution.Values.OrderBy(t => t.Count))
                {
                    Console.WriteLine(string.Format(" {0,4} {1}", item.Count.ToString(), item.Key));
                    total += item.Count;
                }
                Console.WriteLine(" ----");
                Console.WriteLine(string.Format(" {0,4}\r\n", total.ToString()));
            }

        }
        private static string GetCallStackInfo(ClrMDHelper helper, ClrThread thread)
        {
            // must be a running thread
            if (!thread.IsAlive)
                return "Dead";


            // look for exception first
            var exception = thread.CurrentException;
            if (exception != null)
            {
                return exception.Type.Name + ": " + exception.Message;
            }

            // try to find some information about where the code is called from
            StringBuilder sb = new StringBuilder();
            RunningThreadInfo rti = helper.GetThreadInfo(thread);
            switch (rti.RootType)
            {
                case ThreadRoot.Task:
                    sb.Append("Task | " + rti.RootMethod);
                    break;

                case ThreadRoot.WorkItem:
                    sb.Append("Work | " + rti.RootMethod);
                    break;

                default:
                    sb.Append("Thread");
                    break;
            }

            // add lock related details if any
            if (rti.BlockingDetails != null)
            {
                var bi = rti.BlockingDetails;

                // show the blocking details
                string shortTypeName = "";
                if (bi.ObjRef != 0)
                {
                    ClrType type = Runtime.Heap.GetObjectType(bi.ObjRef);
                    if (type != null)
                    {
                        string typeName = type.Name;
                        int pos = typeName.LastIndexOf('.');
                        if (pos != -1)
                            shortTypeName = typeName.Substring(pos + 1);
                    }
                }
                sb.AppendLine(
                    "\r\n                                  => " +
                    bi.TypeName + "." + bi.Frame.Method.Name +
                    ((bi.ObjRef != 0) ?
                        string.Format("(<link cmd=\"!do 0x{0:X}\">0x{0:X16}</link> : {1}", bi.ObjRef.ToString(), shortTypeName) :
                        //"(0x" + bi.ObjRef.ToString("X16") + " : " + shortTypeName + ")" :
                        ""
                        )
                    );
            }

            return sb.ToString();
        }
        private static void UpdateStats(Dictionary<string, WorkInfo> stats, string statName, ref int count)
        {
            count++;

            WorkInfo wi;
            if (!stats.ContainsKey(statName))
            {
                wi = new WorkInfo()
                {
                    Name = statName,
                    Count = 0
                };
                stats[statName] = wi;
            }
            else
            {
                wi = stats[statName];
            }

            wi.Count++;
        }
    }
}
