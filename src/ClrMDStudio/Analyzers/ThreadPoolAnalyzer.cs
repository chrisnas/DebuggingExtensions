using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ClrMDStudio
{
    public class ThreadPoolAnalyzer : IAnalyzer
    {
        readonly string UNKNOWN = "?";
        private class WorkInfo
        {
            public string Name { get; set; }
            public int Count { get; set; }
        }
        class ThreadDistributionItem
        {
            public string Key { get; set; }
            public int Count { get; set; }
        }

        private Dictionary<string, WorkInfo> _workItems;
        private int _workItemCount;
        private Dictionary<string, WorkInfo> _tasks;
        private int _taskCount;


        public ThreadPoolAnalyzer(IClrMDHost host)
        {
            _host = host;
        }


    #region IAnalyzer implementation
    #endregion
        private IClrMDHost _host;
        public IClrMDHost Host
        {
            get { return _host; }
            set { _host = value; }
        }

        public void Run(string args)
        {
            bool success = true;

            _workItems = new Dictionary<string, WorkInfo>();
            _workItemCount = 0;
            _tasks = new Dictionary<string, WorkInfo>();
            _taskCount = 0;

            ClrMDHelper helper = new ClrMDHelper(_host.Session.Clr);

            // The ThreadPool is keeping track of the pending work items into two different areas:
            // - a global queue: stored by ThreadPoolWorkQueue instances of the ThreadPoolGlobals.workQueue static field
            // - several per thread (TLS) local queues: stored in SparseArray<ThreadPoolWorkQueue+WorkStealingQueue> linked from ThreadPoolWorkQueue.allThreadQueues static fields
            // both are using arrays of Task or QueueUserWorkItemCallback
            //
            // NOTE: don't show other thread pool related topics such as timer callbacks or wait objects
            //
            try
            {
                _host.WriteLine("global work item queue________________________________");
                foreach (var item in helper.EnumerateGlobalThreadPoolItems())
                {
                    switch(item.Type)
                    {
                        case ThreadRoot.Task:
                            _host.Write(string.Format("0x{0:X16}\tTask", item.Address));
                            _host.WriteLine(" | " + item.MethodName);
                            UpdateStats(_tasks, item.MethodName, ref _taskCount);
                            break;

                        case ThreadRoot.WorkItem:
                            _host.Write(string.Format("0x{0:X16}\tWork", item.Address));
                            _host.WriteLine(" | " + item.MethodName);
                            UpdateStats(_workItems, item.MethodName, ref _workItemCount);
                            break;

                        default:
                            _host.WriteLine(string.Format("0x{0:X16}\t{1}", item.Address, item.MethodName));
                            break;
                    }
                }

                // look into the local stealing queues in each thread TLS
                // hopefully, they are all stored in static (one per app domain) instance
                // of ThreadPoolWorkQueue.SparseArray<ThreadPoolWorkQueue.WorkStealingQueue>
                //
                _host.WriteLine("\r\nlocal per thread work items_____________________________________");
                try
                {
                    foreach (var item in helper.EnumerateLocalThreadPoolItems())
                    {
                        switch (item.Type)
                        {
                            case ThreadRoot.Task:
                                _host.Write(string.Format("0x{0:X16}\tTask", item.Address));
                                _host.WriteLine(" | " + item.MethodName);
                                UpdateStats(_tasks, item.MethodName, ref _taskCount);
                                break;

                            case ThreadRoot.WorkItem:
                                _host.Write(string.Format("0x{0:X16}\tWork", item.Address));
                                _host.WriteLine(" | " + item.MethodName);
                                UpdateStats(_workItems, item.MethodName, ref _workItemCount);
                                break;

                            default:
                                _host.WriteLine(string.Format("0x{0:X16}\t{1}", item.Address, item.MethodName));
                                break;
                        }
                    }
                }
                finally
                {
                    // provide a summary sorted by count
                    if ((_tasks.Values.Count > 0) || (_workItems.Values.Count > 0))
                    {
                        _host.WriteLine("\r\n____________________________________________________________________________________________________\r\nCount Details\r\n----------------------------------------------------------------------------------------------------");
                    }

                    // tasks first if any
                    StringBuilder sb = new StringBuilder(Math.Max(_tasks.Values.Count, _workItems.Count) * 16);
                    if (_tasks.Values.Count > 0)
                    {
                        foreach (var item in _tasks.Values.OrderBy(wi => wi.Count))
                        {
                            sb.AppendFormat(" {0,4} Task  {1}\r\n", item.Count.ToString(), item.Name);
                        }
                        _host.Write(sb.ToString());
                        _host.WriteLine(" ----");
                        _host.WriteLine(string.Format(" {0,4}\r\n", _taskCount.ToString()));
                    }

                    // then QueueUserWorkItem next if any
                    if (_workItems.Values.Count > 0)
                    {
                        sb.Clear();
                        foreach (var item in _workItems.Values.OrderBy(wi => wi.Count))
                        {
                            sb.AppendFormat(" {0,4} Work  {1}\r\n", item.Count.ToString(), item.Name);
                        }
                        _host.Write(sb.ToString());
                        _host.WriteLine(" ----");
                        _host.WriteLine(string.Format(" {0,4}\r\n", _workItemCount.ToString()));
                    }

                    var threadPool = _host.Session.Clr.ThreadPool;
                    _host.WriteLine(string.Format(
                        "\r\nCPU = {0}% for {1} threads (#idle = {2} + #running = {3} | #dead = {4} | #max = {5})",
                        threadPool.CpuUtilization.ToString(),
                        threadPool.TotalThreads.ToString(),
                        threadPool.IdleThreads.ToString(),
                        threadPool.RunningThreads.ToString(),
                        _host.Session.Clr.Threads.Count(t => t.IsThreadpoolWorker && !t.IsThreadpoolCompletionPort && !t.IsAlive && !t.IsThreadpoolGate && !t.IsThreadpoolTimer && !t.IsThreadpoolWait).ToString(),
                        threadPool.MaxThreads.ToString()
                        ));

                    // show the running worker threads
                    DumpRunningThreadpoolThreads(helper);
                }
            }
            catch (Exception x)
            {
                _host.WriteLine(x.Message);
                success = false;
            }
            finally
            {
                _host.OnAnalysisDone(success);
            }
        }


        private void DumpRunningThreadpoolThreads(ClrMDHelper helper)
        {
            Dictionary<string, ThreadDistributionItem> distribution = new Dictionary<string, ThreadDistributionItem>(8 * 1024);

            StringBuilder sb = new StringBuilder(8 * 1024 * 1024);

            _host.WriteLine("\r\n  ID ThreadOBJ        Locks  Details");
            _host.WriteLine("-----------------------------------------------------------------------------------");
            foreach (var thread in _host.Session.Clr.Threads
                .Where(t => t.IsThreadpoolWorker)
                .OrderBy(t => (t.LockCount > 0) ? -1 : (!t.IsAlive ? t.ManagedThreadId + 10000 : t.ManagedThreadId)))
            {
                string details = GetCallStackInfo(helper, thread);
                sb.AppendFormat("{0,4} {1}  {2}  {3}\r\n",
                    thread.ManagedThreadId.ToString(),
                    thread.Address.ToString("X16"),
                    ((thread.LockCount > 0) ? thread.LockCount.ToString("D4") : "    "),
                    details
                );

                // the details might have different forms
                // 155 000000CC08EF5030        Thread
                //                                  + [method signature]
                //                                  => WaitHandle.WaitOneNative(0x000000CFC388EBA0 : SafeWaitHandle)
                // 156 000000CC08EFD050        Task | [method signature]
                //                                  + [method signature]
                //                                  => WaitHandle.WaitOneNative(0x000000CF40B7F480 : SafeWaitHandle)
                // but we just want to group by the first TWO lines (not the "=> ..." that contains pointers values that will break the grouping
                // so the key is computed up to "=>"
                string key = details;
                int pos = details.IndexOf("=>");
                if (pos == -1)
                {
                    key = string.Intern(details);
                }
                else
                {
                    int lastLineMarkerPos = details.IndexOf("\r\n");
                    if (lastLineMarkerPos == -1)
                    {
                        Debug.Fail("unexpected item format" + details);
                        lastLineMarkerPos = pos;
                    }
                    key = string.Intern(details.Substring(0, lastLineMarkerPos + "\r\n".Length));
                }

                ThreadDistributionItem state;
                if (distribution.ContainsKey(key))
                {
                    state = distribution[key];
                }
                else
                {
                    state = new ThreadDistributionItem()
                    {
                        Key = key,
                        Count = 0
                    };
                    distribution[key] = state;
                }
                state.Count++;
            }
            _host.WriteLine(sb.ToString());

            // build a summary
            if (distribution.Values.Count > 0)
            {
                sb.Clear();
                sb.AppendLine("\r\n____________________________________________________________________________________________________\r\nCount Details\r\n----------------------------------------------------------------------------------------------------");
                int total = 0;
                foreach (var item in distribution.Values.OrderBy(t => t.Count))
                {
                    sb.AppendLine(string.Format(" {0,4} {1}", item.Count.ToString(), item.Key));
                    total += item.Count;
                }
                sb.AppendLine(" ----");
                sb.AppendLine(string.Format(" {0,4}\r\n", total.ToString()));

                _host.WriteLine(sb.ToString());
            }
        }
        private string GetCallStackInfo(ClrMDHelper helper, ClrThread thread)
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
            switch(rti.RootType)
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
                    ClrType type = _host.Session.Clr.Heap.GetObjectType(bi.ObjRef);
                    if (type != null)
                    {
                        string typeName = type.Name;
                        int pos = typeName.LastIndexOf('.');
                        if (pos != -1)
                            shortTypeName = typeName.Substring(pos + 1);
                    }
                }
                sb.Append(
                    ((bi.LockingFrame != null) ? "\r\n                                  + " + bi.LockingFrame.DisplayString : "") +
                    "\r\n                                  => " +
                    bi.TypeName + "." + bi.Frame.Method.Name +
                    ((bi.ObjRef != 0) ?
                        "(0x" + bi.ObjRef.ToString("X16") + " : " + shortTypeName + ")" :
                        ""
                        )
                    );
            }

            return sb.ToString();
        }
        private string BuildMethodName(ClrType targetType, ulong delegateRef)
        {
            var methodPtr = _host.Session.GetFieldValue(delegateRef, "_methodPtr");
            if (methodPtr != null)
            {
                ClrMethod method = _host.Session.Clr.GetMethodByAddress((ulong)(long)methodPtr);
                if (method != null)
                {
                    if (method.Type.Name == targetType.Name)
                    {
                        return string.Format("{0}.{1}", targetType.Name, method.Name);
                    }
                    else  // method is implemented by an class inherited from targetType
                    {
                        return string.Format("({0}){1}.{2}", targetType.Name, method.Type.Name, method.Name);
                    }
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return "";
            }
        }
        private void UpdateStats(Dictionary<string, WorkInfo> stats, string statName, ref int count)
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

        public void RunDone()
        {
            _workItems = new Dictionary<string, WorkInfo>();
            _workItemCount = 0;
            _tasks = new Dictionary<string, WorkInfo>();
            _taskCount = 0;

            ClrMDHelper helper = new ClrMDHelper(_host.Session.Clr);

            // The ThreadPool is keeping track of the pending work items into two different areas:
            // - a global queue: stored by ThreadPoolWorkQueue instances of the ThreadPoolGlobals.workQueue static field
            // - several per thread (TLS) local queues: stored in SparseArray<ThreadPoolWorkQueue+WorkStealingQueue> linked from ThreadPoolWorkQueue.allThreadQueues static fields
            // both are using arrays of Task or QueueUserWorkItemCallback
            //
            // NOTE: don't show other thread pool related topics such as timer callbacks or wait objects
            //
            try
            {
                var heap = _host.Session.Clr.Heap;
                var clr = _host.Session.Clr;

                _host.WriteLine("global work item queue________________________________");
                // look for the ThreadPoolGlobals.workQueue static field
                ClrModule mscorlib = helper.GetMscorlib();
                if (mscorlib == null)
                    return;

                ClrType queueType = mscorlib.GetTypeByName("System.Threading.ThreadPoolGlobals");
                if (queueType == null)
                    return;

                ClrStaticField workQueueField = queueType.GetStaticFieldByName("workQueue");
                if (workQueueField == null)
                    return;

                // the CLR keeps one static instance per application domain
                foreach (var appDomain in clr.AppDomains)
                {
                    object workQueueValue = workQueueField.GetValue(appDomain);
                    ulong workQueueRef = (workQueueValue == null) ? 0L : (ulong)workQueueValue;
                    if (workQueueRef == 0)
                        continue;

                    // should be  System.Threading.ThreadPoolWorkQueue
                    ClrType workQueueType = heap.GetObjectType(workQueueRef);
                    if (workQueueType == null)
                        continue;
                    if (workQueueType.Name != "System.Threading.ThreadPoolWorkQueue")
                        continue;

                    if (!DumpThreadPoolWorkQueue(workQueueType, workQueueRef))
                    {
                        _host.WriteLine("Impossible to dump thread pool work queue @ 0x" + workQueueRef.ToString("X"));
                    }
                }



                // look into the local stealing queues in each thread TLS
                // hopefully, they are all stored in static (one per app domain) instance
                // of ThreadPoolWorkQueue.SparseArray<ThreadPoolWorkQueue.WorkStealingQueue>
                //
                _host.WriteLine("\r\nlocal per thread work items_____________________________________");
                try
                {
                    queueType = mscorlib.GetTypeByName("System.Threading.ThreadPoolWorkQueue");
                    if (queueType == null)
                        return;

                    ClrStaticField threadQueuesField = queueType.GetStaticFieldByName("allThreadQueues");
                    if (threadQueuesField == null)
                        return;

                    foreach (ClrAppDomain domain in clr.AppDomains)
                    {
                        ulong? threadQueue = (ulong?)threadQueuesField.GetValue(domain);
                        if (!threadQueue.HasValue || threadQueue.Value == 0)
                            continue;

                        ClrType threadQueueType = heap.GetObjectType(threadQueue.Value);
                        if (threadQueueType == null)
                            continue;

                        var sparseArrayRef = _host.Session.GetFieldValue(threadQueue.Value, "m_array");
                        _host.Session.ForEach((ulong)sparseArrayRef, stealingQueue =>
                        {
                            if (stealingQueue != 0)
                            {
                                var arrayRef = _host.Session.GetFieldValue(stealingQueue, "m_array");
                                DumpThreadPoolWorkItems((ulong)arrayRef);
                            }
                        });
                    }
                }
                finally
                {
                    // provide a summary sorted by count
                    // tasks first if any
                    if (_tasks.Values.Count > 0)
                    {
                        foreach (var item in _tasks.Values.OrderBy(wi => wi.Count))
                        {
                            _host.WriteLine(string.Format(" {0,4} Task  {1}", item.Count.ToString(), item.Name));
                        }
                        _host.WriteLine(" ----");
                        _host.WriteLine(string.Format(" {0,4}\r\n", _taskCount.ToString()));
                    }

                    // then QueueUserWorkItem next if any
                    if (_workItems.Values.Count > 0)
                    {
                        foreach (var item in _workItems.Values.OrderBy(wi => wi.Count))
                        {
                            _host.WriteLine(string.Format(" {0,4} Work  {1}", item.Count.ToString(), item.Name));
                        }
                        _host.WriteLine(" ----");
                        _host.WriteLine(string.Format(" {0,4}\r\n", _workItemCount.ToString()));
                    }

                    var threadPool = _host.Session.Clr.ThreadPool;
                    _host.WriteLine(string.Format(
                        "\r\nCPU = {0}% for {1} threads (#idle = {2} + #running = {3} | #dead = {4} | #max = {5})",
                        threadPool.CpuUtilization.ToString(),
                        threadPool.TotalThreads.ToString(),
                        threadPool.IdleThreads.ToString(),
                        threadPool.RunningThreads.ToString(),
                        _host.Session.Clr.Threads.Count(t => t.IsThreadpoolWorker && !t.IsThreadpoolCompletionPort && !t.IsAlive && !t.IsThreadpoolGate && !t.IsThreadpoolTimer && !t.IsThreadpoolWait).ToString(),
                        threadPool.MaxThreads.ToString()
                        ));

                    // show the running worker threads
                    DumpRunningThreadpoolThreads(helper);
                }
            }
            catch (Exception x)
            {
                _host.WriteLine(x.Message);
            }
        }
        private bool DumpThreadPoolWorkQueue(ClrType tpwqType, ulong tpRef)
        {
            // start from the tail and follow the Next
            var currentQueueRef = _host.Session.GetFieldValue(tpRef, "queueTail");
            while ((currentQueueRef != null) && ((ulong)currentQueueRef != 0))
            {
                DumpQueueSegment((ulong)currentQueueRef);

                currentQueueRef = _host.Session.GetFieldValue((ulong)currentQueueRef, "Next");
            }

            return true;
        }
        private void DumpQueueSegment(ulong queueRef)
        {
            // get the System.Threading.ThreadPoolWorkQueue+QueueSegment nodes array
            var nodesRef = _host.Session.GetFieldValue((ulong)queueRef, "nodes");
            DumpThreadPoolWorkItems((ulong)nodesRef);
        }
        private void DumpThreadPoolWorkItems(ulong arrayOfWorkItems)
        {
            // get each IThreadPoolWorkItem --> handle when it is a Task
            _host.Session.ForEach(arrayOfWorkItems, elementRef =>
            {
                var elementType = _host.Session.ManagedHeap.GetObjectType(elementRef);
                if (elementType.Name == "System.Threading.Tasks.Task")
                {
                    DumpTask(elementRef);
                }
                else if (elementType.Name == "System.Threading.QueueUserWorkItemCallback")
                {
                    DumpQueueUserWorkItemCallback(elementRef);
                }
                else
                {
                    _host.WriteLine(string.Format("0x{0:X}\t{1}", elementRef.ToString(), elementType.Name));
                }
            });
        }
        private void DumpQueueUserWorkItemCallback(ulong elementRef)
        {
            string statName = UNKNOWN;

            _host.Write(string.Format("0x{0:X}\tWork", elementRef.ToString()));

            ulong callbackRef = (ulong)_host.Session.GetFieldValue(elementRef, "callback");
            if (callbackRef == 0)
            {
                _host.WriteLine(" [no callback]");
            }
            else
            {
                ulong targetRef = (ulong)_host.Session.GetFieldValue(callbackRef, "_target");
                if (targetRef == 0)
                {
                    _host.WriteLine(" [no callback target]");
                }
                else
                {
                    ClrType targetType = _host.Session.ManagedHeap.GetObjectType(targetRef);
                    if (targetType == null)
                    {
                        _host.WriteLine(string.Format(" [target=0x{0}]", targetRef.ToString()));
                    }
                    else
                    {
                        // look for method name
                        string methodName = BuildMethodName(targetType, callbackRef);
                        _host.WriteLine(" | " + methodName);

                        statName = methodName;
                    }
                }
            }

            UpdateStats(_workItems, statName, ref _workItemCount);
        }
        private void DumpTask(ulong taskRef)
        {
            string statName = UNKNOWN;

            _host.Write(string.Format("0x{0:X}\tTask", taskRef.ToString()));

            // look for the context in m_action._target
            ulong actionRef = (ulong)_host.Session.GetFieldValue(taskRef, "m_action");
            if (actionRef == 0)
            {
                _host.WriteLine(" [no action]");
                return;
            }

            ulong targetRef = (ulong)_host.Session.GetFieldValue(actionRef, "_target");
            if (targetRef == 0)
            {
                _host.WriteLine(" [no target]");
            }
            else
            {
                string methodName = BuildMethodName(_host.Session.ManagedHeap.GetObjectType(targetRef), actionRef);
                _host.WriteLine(" | " + methodName);
                statName = methodName;

                // look for the task scheduler
                var taskScheduler = _host.Session.GetFieldValue(targetRef, "m_taskScheduler");
                if (taskScheduler == null)
                {
                    // Note: no need to polluate the output
                    //_host.WriteLine(" [no task scheduler]");
                }
                else
                {
                    ulong schedulerRef = (ulong)taskScheduler;
                    if (schedulerRef != 0)
                    {
                        _host.Write(" [ " + _host.Session.ManagedHeap.GetObjectType(schedulerRef).ToString() + "]");
                    }
                    _host.WriteLine("");
                }
            }

            UpdateStats(_tasks, statName, ref _taskCount);
        }

        public void RunWithAPI()
        {
            _workItems = new Dictionary<string, WorkInfo>();
            _workItemCount = 0;
            _tasks = new Dictionary<string, WorkInfo>();
            _taskCount = 0;

            try
            {
                var threadPool = _host.Session.Clr.ThreadPool;

                foreach (var work in threadPool.EnumerateManagedWorkItems())
                {
                    var elementType = work.Type;
                    if (elementType.Name == "System.Threading.Tasks.Task")
                    {
                        DumpTask(work.Object);
                    }
                    else if (elementType.Name == "System.Threading.QueueUserWorkItemCallback")
                    {
                        DumpQueueUserWorkItemCallback(work.Object);
                    }
                    else
                    {
                        _host.WriteLine(string.Format("0x{0:X}\t{1}", work.Object, elementType.Name));
                    }
                }

                // provide a summary sorted by count
                _host.WriteLine(string.Format(
                    "CPU = {0}% for {1} threads (#idle = {2} + #running = {3} | #dead = {4} | #max = {5})",
                    threadPool.CpuUtilization.ToString(),
                    threadPool.TotalThreads.ToString(),
                    threadPool.IdleThreads.ToString(),
                    threadPool.RunningThreads.ToString(),
                    _host.Session.Clr.Threads.Count(t => t.IsThreadpoolWorker && !t.IsThreadpoolCompletionPort && !t.IsAlive && !t.IsThreadpoolGate && !t.IsThreadpoolTimer && !t.IsThreadpoolWait).ToString(),
                    threadPool.MaxThreads.ToString()
                    ));

                _host.WriteLine("\r\n");
                foreach (var item in _tasks.Values.OrderBy(wi => wi.Count))
                {
                    _host.WriteLine(string.Format(" {0,4} Task  {1}", item.Count.ToString(), item.Name));
                }
                _host.WriteLine(" ----");
                _host.WriteLine(string.Format(" {0,4}\r\n", _taskCount.ToString()));

                foreach (var item in _workItems.Values.OrderBy(wi => wi.Count))
                {
                    _host.WriteLine(string.Format(" {0,4} Work  {1}", item.Count.ToString(), item.Name));
                }
                _host.WriteLine(" ----");
                _host.WriteLine(string.Format(" {0,4}\r\n", _workItemCount.ToString()));
            }
            catch (Exception x)
            {
                _host.WriteLine(x.Message);
            }
        }
        public void RunOriginal()
        {
            _workItems = new Dictionary<string, WorkInfo>();
            _workItemCount = 0;
            _tasks = new Dictionary<string, WorkInfo>();
            _taskCount = 0;

            try
            {
                // look into global thread pool queues
                List<ulong> tpRefs = _host.Session.GetInstancesOf("System.Threading.ThreadPoolWorkQueue");
                if (tpRefs == null)
                {
                    _host.WriteLine("no ThreadPoolWorkQueue");
                    return;
                }

                int count = tpRefs.Count;
                _host.WriteLine(count.ToString() + " global" + ((count > 1) ? " threadpools" : " threadpool"));
                ClrType threadpoolWorkQueueType = _host.Session.ManagedHeap.GetTypeByName("System.Threading.ThreadPoolWorkQueue");
                int currentTp = 1;
                foreach (var tpRef in tpRefs)
                {
                    _host.WriteLine("ThreadPool #" + currentTp.ToString() + " ______________________________________________");
                    if (!DumpThreadPoolWorkQueue(threadpoolWorkQueueType, tpRef))
                    {
                        _host.WriteLine("Impossible to dump thread pool work queue @ 0x" + tpRef.ToString("X"));
                    }

                    currentTp++;
                }


                // look into the local stealing queues in each thread TLS
                // hopefully, they are all stored in static (one per app domain) instance
                // of ThreadPoolWorkQueue.SparseArray<ThreadPoolWorkQueue.WorkStealingQueue>
                //
                _host.WriteLine("\r\nlocal per thread work items________________________________");

                var instanceCount = _host.Session.ForEachInstancesOf("System.Threading.ThreadPoolWorkQueue+SparseArray<System.Threading.ThreadPoolWorkQueue+WorkStealingQueue>", sa =>
                {
                    var sparseArrayRef = _host.Session.GetFieldValue(sa, "m_array");
                    _host.Session.ForEach((ulong)sparseArrayRef, stealingQueue =>
                    {
                        if (stealingQueue != 0)
                        {
                            var arrayRef = _host.Session.GetFieldValue(stealingQueue, "m_array");
                            DumpThreadPoolWorkItems((ulong)arrayRef);
                        }
                    });
                });
                if (instanceCount == 0)
                {
                    _host.WriteLine("Impossible to find per thread work stealing queues  :^(");
                }


                // provide a summary sorted by count
                var threadPool = _host.Session.Clr.ThreadPool;
                _host.WriteLine(string.Format(
                    "CPU = {0}% for {1} threads (#idle = {2} + #running = {3} | #dead = {4} | #max = {5})",
                    threadPool.CpuUtilization.ToString(),
                    threadPool.TotalThreads.ToString(),
                    threadPool.IdleThreads.ToString(),
                    threadPool.RunningThreads.ToString(),
                    _host.Session.Clr.Threads.Count(t => t.IsThreadpoolWorker && !t.IsThreadpoolCompletionPort && !t.IsAlive && !t.IsThreadpoolGate && !t.IsThreadpoolTimer && !t.IsThreadpoolWait).ToString(),
                    threadPool.MaxThreads.ToString()
                    ));

                _host.WriteLine("\r\n");
                foreach (var item in _tasks.Values.OrderBy(wi => wi.Count))
                {
                    _host.WriteLine(string.Format(" {0,4} Task  {1}", item.Count.ToString(), item.Name));
                }
                _host.WriteLine(" ----");
                _host.WriteLine(string.Format(" {0,4}\r\n", _taskCount.ToString()));

                foreach (var item in _workItems.Values.OrderBy(wi => wi.Count))
                {
                    _host.WriteLine(string.Format(" {0,4} Work  {1}", item.Count.ToString(), item.Name));
                }
                _host.WriteLine(" ----");
                _host.WriteLine(string.Format(" {0,4}\r\n", _workItemCount.ToString()));
            }
            catch (Exception x)
            {
                _host.WriteLine(x.Message);
            }
        }
        private string GetFrameInformation(ClrThread thread, ClrStackFrame frame, ClrStackFrame firstFrame)
        {
            // get the method call from the given frame
            string info = frame.DisplayString;

            // look for locking information
            if (firstFrame.Method.Name.Contains("Wait") || (firstFrame.Method.Name == "Enter") && (firstFrame.Method.Type.Name == "System.Threading.Monitor"))
            {
                // special case for MonitorEnter --> not a WaitHandle to wait on
                bool isMonitorEnter = (firstFrame.Method.Name == "Enter") && (firstFrame.Method.Type.Name == "System.Threading.Monitor");

                info = info + "\r\n                             => " + firstFrame.Method.Type.Name + "." + firstFrame.Method.Name;

                // look for object used as lock
                int maxStackObjects = 10;
                foreach (var so in thread.EnumerateStackObjects(true))
                {
                    if (so == null)
                        continue;

                    var type = so.Type;
                    if (so.Type == null)
                        continue;

                    string typeName = so.Type.Name;
                    if (typeName.Contains("WaitHandle") || isMonitorEnter)
                    {
                        info = info + string.Format("({0} = 0x{1:X16})\r\n", typeName, so.Object);
                        break;
                    }
                    else
                    {

                    }

                    maxStackObjects--;
                    if (maxStackObjects == 0)
                        break;
                }
            }

            return info;
        }
    }
}
