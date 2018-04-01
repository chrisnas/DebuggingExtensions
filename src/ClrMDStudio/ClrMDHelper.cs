using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ClrMDStudio
{
    public class GenerationInSegment
    {
        private List<ClrHandle> _pinnedObjects;

        public GenerationInSegment()
        {
            _pinnedObjects = new List<ClrHandle>();
        }
        public int Generation { get; set; }
        public ulong Start { get; set; }
        public ulong End { get; set; }
        public ulong Length { get; set; }
        public IReadOnlyList<ClrHandle> PinnedObjects => _pinnedObjects.OrderBy(po => po.Object).ToList();

        internal void AddPinnedObject(ClrHandle pinnedObject)
        {
            _pinnedObjects.Add(pinnedObject);
        }
    }

    public class SegmentInfo
    {
        private List<GenerationInSegment> _generations;

        internal SegmentInfo(int number)
        {
            Number = number;

            _generations = new List<GenerationInSegment>();
        }

        // 0 if workstation GC
        // # processor/core if server GC
        public int Number { get; set; }
        public IEnumerable<GenerationInSegment> Generations => _generations;
        public GenerationInSegment GetGenerationFromAddress(ulong address)
        {
            for (int i = 0; i < _generations.Count; i++)
            {
                if (IsAddressInGeneration(address, _generations[i]))
                {
                    return _generations[i];
                }
            }

            return null;
        }

        private bool IsAddressInGeneration(ulong address, GenerationInSegment generationInSegment)
        {
            return (address >= generationInSegment.Start) && (address < generationInSegment.End);
        }
        internal void AddGenerationInSegment(int generation, ulong start, ulong end, ulong length)
        {
            _generations.Add(
                new GenerationInSegment()
                {
                    Generation = generation,
                    Start = start,
                    End = end,
                    Length = length
                }
            );
        }
    }

    public class PinnedObjectsGeneration
    {
        private Dictionary<string, List<ClrHandle>> _handles;
        private int _handleCount;

        public PinnedObjectsGeneration(int number)
        {
            Number = number;
            _handles = new Dictionary<string, List<ClrHandle>>();
        }

        public int Number { get; }
        public int HandleCount => _handleCount;
        public IEnumerable<string> Types => _handles.Keys;
        public IEnumerable<ClrHandle> GetHandles(string type)
        {
            List<ClrHandle> handles;
            if (!_handles.TryGetValue(type, out handles))
            {
                return null;
            }

            return handles;
        }

        internal void AddHandle(ClrHandle handle)
        {
            _handleCount++;

            var typeName = handle.Type.ToString();
            List<ClrHandle> handles;
            if (!_handles.TryGetValue(typeName, out handles))
            {
                handles = new List<ClrHandle>(2048);
                _handles[typeName] = handles;
            }
            handles.Add(handle);
        }
    }

    public enum ThreadRoot
    {
        Raw,
        Task,
        WorkItem
    }

    public class BlockingInfo
    {
        public ClrStackFrame Frame { get; set; }
        public ClrStackFrame LockingFrame { get; internal set; }
        public ulong ObjRef { get; set; }
        public string TypeName { get; set; }
    }

    public class RunningThreadInfo
    {
        public ThreadRoot RootType { get; set; }
        public string RootMethod { get; set; }
        public BlockingInfo BlockingDetails { get; set; }
    }

    public class ThreadPoolItem
    {
        public ThreadRoot Type { get; set; }
        public ulong Address { get; set; }
        public string MethodName { get; set; }
    }

    public class TimerInfo
    {
        public ulong TimerQueueTimerAddress { get; set; }
        public uint DueTime { get; set; }
        public uint Period { get; set; }
        public bool Cancelled { get; set; }
        public ulong StateAddress { get; set; }
        public string StateTypeName { get; set; }
        public ulong ThisAddress { get; set; }
        public string MethodName { get; set; }
    }

    public class ClrMDHelper
    {
        private ClrRuntime _clr;
        private ClrHeap _heap;

        public ClrMDHelper(ClrRuntime clr)
        {
            if (clr == null)
                throw new ArgumentNullException("clr");

            _clr = clr;
            _heap = clr.GetHeap();

            _eventTypes = new HashSet<string>();
            _eventTypes.Add("System.Threading.Mutex");
            _eventTypes.Add("System.Threading.Semaphore");
            _eventTypes.Add("System.Threading.ManualResetEvent");
            _eventTypes.Add("System.Threading.AutoResetEvent");
            _eventTypes.Add("System.Threading.WaitHandle");
            _eventTypes.Add("Microsoft.Win32.SafeHandles.SafeWaitHandle");
        }


    #region initialization
    #endregion

        // Some code from GitHub ClrMD implementation
        //   threadpool.cs
        //   lockinspection.cs
        private ClrType _rwType; // ReaderWriterLock type
        private ClrType _rwsType; // ReaderWriterLockSlim type
        private HashSet<string> _eventTypes;

        private bool IsReaderWriterLock(ulong obj, ClrType type)
        {
            if (type == null)
                return false;

            if (_rwType == null)
            {
                if (type.Name != "System.Threading.ReaderWriterLock")
                    return false;

                _rwType = type;
                return true;
            }

            return _rwType == type;
        }

        private bool IsReaderWriterSlim(ulong obj, ClrType type)
        {
            if (type == null)
                return false;

            if (_rwsType == null)
            {
                if (type.Name != "System.Threading.ReaderWriterLockSlim")
                    return false;

                _rwsType = type;
                return true;
            }

            return _rwsType == type;
        }

        private ulong FindLockObject(ulong start, ulong stop, Func<ulong, ClrType, bool> isCorrectType)
        {
            foreach (ulong ptr in EnumeratePointersInRange(start, stop))
            {
                ulong val = 0;
                if (_clr.ReadPointer(ptr, out val))
                {
                    if (isCorrectType(val, _heap.GetObjectType(val)))
                        return val;
                }
            }

            return 0;
        }

        private ulong FindWaitHandle(ulong start, ulong stop, HashSet<string> eventTypes)
        {
            foreach (ulong obj in EnumerateObjectsOfTypes(start, stop, eventTypes))
                return obj;

            return 0;
        }

        private ulong FindWaitObjects(ulong start, ulong stop, string typeName)
        {
            foreach (ulong obj in EnumerateObjectsOfType(start, stop, typeName))
                return obj;

            return 0;
        }

        private ulong FindMonitor(ulong start, ulong stop)
        {
            // This code from lockinspection requires too much internal code  :^(
            //
            //ulong obj = 0;
            //foreach (ulong ptr in EnumeratePointersInRange(start, stop))
            //{
            //    ulong tmp = 0;
            //    if (_clr.ReadPointer(ptr, out tmp))
            //    {
            //        if (_syncblks.TryGetValue(tmp, out tmp))
            //        {
            //            return tmp;
            //        }
            //    }
            //}

            return 0;
        }

        private IEnumerable<ulong> EnumerateObjectsOfTypes(ulong start, ulong stop, HashSet<string> types)
        {
            foreach (ulong ptr in EnumeratePointersInRange(start, stop))
            {
                ulong obj;
                if (_clr.ReadPointer(ptr, out obj))
                {
                    if (_heap.IsInHeap(obj))
                    {
                        ClrType type = null;

                        try
                        {
                            type = _heap.GetObjectType(obj);
                        }
                        catch (Exception)
                        {
                            // it happens sometimes   :^(
                        }

                        int sanity = 0;
                        while (type != null)
                        {
                            if (types.Contains(type.Name))
                            {
                                yield return obj;
                                break;
                            }

                            type = type.BaseType;

                            if (sanity++ == 16)
                                break;
                        }
                    }
                }
            }
        }
        private IEnumerable<ulong> EnumerateObjectsOfType(ulong start, ulong stop, string typeName)
        {
            foreach (ulong ptr in EnumeratePointersInRange(start, stop))
            {
                ulong obj;
                if (_clr.ReadPointer(ptr, out obj))
                {
                    if (_heap.IsInHeap(obj))
                    {
                        ClrType type = _heap.GetObjectType(obj);


                        int sanity = 0;
                        while (type != null)
                        {
                            if (type.Name == typeName)
                            {
                                yield return obj;
                                break;
                            }

                            type = type.BaseType;

                            if (sanity++ == 16)
                                break;
                        }
                    }
                }
            }
        }
        private IEnumerable<ulong> EnumeratePointersInRange(ulong start, ulong stop)
        {
            uint diff = (uint)_clr.PointerSize;

            if (start > stop)
                for (ulong ptr = stop; ptr <= start; ptr += diff)
                    yield return ptr;
            else
                for (ulong ptr = stop; ptr >= start; ptr -= diff)
                    yield return ptr;
        }
        public ClrModule GetMscorlib()
        {
            foreach (ClrModule module in _clr.Modules)
                if (module.AssemblyName.Contains("mscorlib.dll"))
                    return module;

            // Uh oh, this shouldn't have happened.  Let's look more carefully (slowly).
            foreach (ClrModule module in _clr.Modules)
                if (module.AssemblyName.ToLower().Contains("mscorlib"))
                    return module;

            // Ok...not sure why we couldn't find it.
            return null;
        }


    #region public API
    #endregion
        public IEnumerable<ThreadPoolItem> EnumerateGlobalThreadPoolItems()
        {
            // look for the ThreadPoolGlobals.workQueue static field
            ClrModule mscorlib = GetMscorlib();
            if (mscorlib == null)
                throw new InvalidOperationException("Impossible to find mscorlib.dll");

            ClrType queueType = mscorlib.GetTypeByName("System.Threading.ThreadPoolGlobals");
            if (queueType == null)
                yield break;

            ClrStaticField workQueueField = queueType.GetStaticFieldByName("workQueue");
            if (workQueueField == null)
                yield break;

            // the CLR keeps one static instance per application domain
            foreach (var appDomain in _clr.AppDomains)
            {
                object workQueueValue = workQueueField.GetValue(appDomain);
                ulong workQueueRef = (workQueueValue == null) ? 0L : (ulong)workQueueValue;
                if (workQueueRef == 0)
                    continue;

                // should be  System.Threading.ThreadPoolWorkQueue
                ClrType workQueueType = _heap.GetObjectType(workQueueRef);
                if (workQueueType == null)
                    continue;
                if (workQueueType.Name != "System.Threading.ThreadPoolWorkQueue")
                    continue;

                foreach (var item in EnumerateThreadPoolWorkQueue(workQueueRef))
                {
                    yield return item;
                }
            }
        }
        public IEnumerable<ThreadPoolItem> EnumerateLocalThreadPoolItems()
        {
            var queueType = GetMscorlib().GetTypeByName("System.Threading.ThreadPoolWorkQueue");
            if (queueType == null)
                yield break;

            ClrStaticField threadQueuesField = queueType.GetStaticFieldByName("allThreadQueues");
            if (threadQueuesField == null)
                yield break;

            foreach (ClrAppDomain domain in _clr.AppDomains)
            {
                ulong? threadQueueRef = (ulong?)threadQueuesField.GetValue(domain);
                if (!threadQueueRef.HasValue || threadQueueRef.Value == 0)
                    continue;

                var threadQueue = _heap.GetProxy((ulong)threadQueueRef);
                if (threadQueue == null)
                    continue;

                var sparseArray = threadQueue.m_array;
                if (sparseArray == null)
                    continue;

                foreach (var stealingQueue in sparseArray)
                {
                    if (stealingQueue == null)
                        continue;

                    foreach (var item in EnumerateThreadPoolStealingQueue(stealingQueue))
                    {
                        yield return item;
                    }
                }
            }
        }

        private const int MAX_FRAME_COUNT = 75;
        public RunningThreadInfo GetThreadInfo(ClrThread thread)
        {
            if (!thread.IsAlive)
                return null;

            // try to find some information about where the code is
            // 1. look for Task related details
            // ...
            // ThreadPoolUseCases.MainWindow.<OnStartThreadPool>b__10_0(System.Object)
            // System.Threading.Tasks.Task.Execute()  <-- look for this one so keep the previous frame
            // ...
            //
            //
            // 2. look for QueueUserWorkItem pattern
            //
            // ...
            // ThreadPoolUseCases.MainWindow.<OnStartThreadPool>b__13_4(System.Object)
            // System.Threading.ExecutionContext.RunInternal()  <-- look for this one so keep the previous frame
            // ...
            //
            //
            // 3. look for locking method calls
            //
            //
            // ? look for lock details if thread.LockCount > 0
            //

            // look for possible locking calls
            StringBuilder sb = new StringBuilder();
            int currentFrame = 0;
            ClrStackFrame lastFrame = null;
            BlockingInfo bi = null;
            RunningThreadInfo rti = new RunningThreadInfo();

            foreach (var frame in thread.EnumerateStackTrace())
            {
                if (currentFrame > MAX_FRAME_COUNT)
                {
                    // it is time to check if we got enough information about Task/WorkItem
                    break;
                }

                // figure out if there is any lock on the first frame
                // based on lockingInspection.cs | SetThreadWaiters()
                var method = frame.Method;
                if (method == null)
                    continue;
                var type = method.Type;
                if (type == null)
                    continue;

                if (bi == null)
                    switch (method.Name)
                    {
                        case "AcquireWriterLockInternal":
                        case "FCallUpgradeToWriterLock":
                        case "UpgradeToWriterLock":
                        case "AcquireReaderLockInternal":
                        case "AcquireReaderLock":
                            if (type.Name == "System.Threading.ReaderWriterLock")
                            {
                                bi = new BlockingInfo()
                                {
                                    Frame = frame,
                                    TypeName = "ReaderWriterLock"
                                };
                                bi.ObjRef = FindLockObject(
                                    thread.StackLimit,
                                    thread.StackTrace[currentFrame].StackPointer,
                                    IsReaderWriterLock
                                    );
                                if (bi.ObjRef == 0)
                                {
                                    bi.ObjRef = FindLockObject(
                                        thread.StackTrace[currentFrame].StackPointer,
                                        thread.StackBase,
                                        IsReaderWriterLock
                                        );
                                }
                            }
                            break;

                        case "TryEnterReadLockCore":
                        case "TryEnterReadLock":
                        case "TryEnterUpgradeableReadLock":
                        case "TryEnterUpgradeableReadLockCore":
                        case "TryEnterWriteLock":
                        case "TryEnterWriteLockCore":
                            if (type.Name == "System.Threading.ReaderWriterLockSlim")
                            {
                                bi = new BlockingInfo()
                                {
                                    Frame = frame,
                                    TypeName = "ReaderWriterLockSlim"
                                };
                                bi.ObjRef = FindLockObject(
                                    thread.StackLimit,
                                    thread.StackTrace[currentFrame].StackPointer,
                                    IsReaderWriterSlim
                                    );
                                if (bi.ObjRef == 0)
                                {
                                    bi.ObjRef = FindLockObject(
                                        thread.StackTrace[currentFrame].StackPointer,
                                        thread.StackBase,
                                        IsReaderWriterSlim
                                        );
                                }
                            }
                            break;

                        case "JoinInternal":
                        case "Join":
                            if (type.Name == "System.Threading.Thread")
                            {
                                bi = new BlockingInfo()
                                {
                                    Frame = frame,
                                    TypeName = "Thread"
                                };

                                // TODO: look for the thread
                            }
                            break;

                        case "Wait":
                        case "ObjWait":
                            if (type.Name == "System.Threading.Monitor")
                            {
                                bi = new BlockingInfo()
                                {
                                    Frame = frame,
                                    TypeName = "Monitor"
                                };

                                // TODO: look for the lock
                            }
                            break;

                        case "WaitAny":
                        case "WaitAll":
                            if (type.Name == "System.Threading.WaitHandle")
                            {
                                bi = new BlockingInfo()
                                {
                                    Frame = frame,
                                    TypeName = "WaitHandle"
                                };

                                bi.ObjRef = FindWaitObjects(
                                    thread.StackLimit,
                                    thread.StackTrace[currentFrame].StackPointer,
                                    "System.Threading.WaitHandle[]"
                                    );
                                if (bi.ObjRef == 0)
                                    bi.ObjRef = FindWaitObjects(
                                        thread.StackTrace[currentFrame].StackPointer,
                                        thread.StackBase,
                                        "System.Threading.WaitHandle[]"
                                        );
                            }
                            break;

                        case "WaitOne":
                        case "InternalWaitOne":
                        case "WaitOneNative":
                            if (type.Name == "System.Threading.WaitHandle")
                            {
                                bi = new BlockingInfo()
                                {
                                    Frame = frame,
                                    TypeName = "WaitHandle"
                                };

                                if (_eventTypes == null)
                                {
                                    _eventTypes = new HashSet<string>();
                                    _eventTypes.Add("System.Threading.Mutex");
                                    _eventTypes.Add("System.Threading.Semaphore");
                                    _eventTypes.Add("System.Threading.ManualResetEvent");
                                    _eventTypes.Add("System.Threading.AutoResetEvent");
                                    _eventTypes.Add("System.Threading.WaitHandle");
                                    _eventTypes.Add("Microsoft.Win32.SafeHandles.SafeWaitHandle");
                                }

                                bi.ObjRef = FindWaitHandle(
                                    thread.StackLimit,
                                    thread.StackTrace[currentFrame].StackPointer,
                                    _eventTypes
                                    );
                                if (bi.ObjRef == 0)
                                    bi.ObjRef = FindWaitHandle(
                                        thread.StackTrace[currentFrame].StackPointer,
                                        thread.StackBase,
                                        _eventTypes
                                        );
                            }
                            break;


                        case "TryEnter":
                        case "ReliableEnterTimeout":
                        case "TryEnterTimeout":
                        case "Enter":
                            if (type.Name == "System.Threading.Monitor")
                            {
                                bi = new BlockingInfo()
                                {
                                    Frame = frame,
                                    TypeName = "Monitor"
                                };

                                // NOTE: this method is not implemented yet
                                bi.ObjRef = FindMonitor(
                                    thread.StackLimit,
                                    thread.StackTrace[currentFrame].StackPointer
                                    );
                            }
                            break;

                        default:
                            break;
                    }
                else // keep track of the frame BEFORE locking
                {
                    if ((bi.LockingFrame == null) && (!frame.Method.Type.Name.Contains("System.Threading")))
                    {
                        bi.LockingFrame = frame;
                    }
                }

                // look for task/work item details
                if (frame.Kind != ClrStackFrameType.ManagedMethod)
                {
                    continue;
                }

                if (frame.Method.Type.Name == "System.Threading.Tasks.Task")
                {
                    if (frame.Method.Name == "Execute")
                    {
                        // the previous frame should contain the name of the method called by the task
                        if (lastFrame != null)
                        {
                            rti.RootType = ThreadRoot.Task;
                            rti.RootMethod = lastFrame.DisplayString;
                        }

                        break;
                    }
                }
                else
                if (frame.Method.Type.Name == "System.Threading.ExecutionContext")
                {
                    if (frame.Method.Name == "RunInternal")
                    {
                        // the previous frame should contain the name of the method called by QueueUserWorkItem
                        if (lastFrame != null)
                        {
                            rti.RootType = ThreadRoot.WorkItem;
                            rti.RootMethod = lastFrame.DisplayString;
                        }

                        break;
                    }
                }
                else
                {
                    lastFrame = frame;
                }

                currentFrame++;
            }
            rti.BlockingDetails = bi;

            return rti;
        }
        public IEnumerable<TimerInfo> EnumerateTimers()
        {
            var timerQueueType = GetMscorlib().GetTypeByName("System.Threading.TimerQueue");
            if (timerQueueType == null)
                yield break;

            ClrStaticField instanceField = timerQueueType.GetStaticFieldByName("s_queue");
            if (instanceField == null)
                yield break;

            foreach (ClrAppDomain domain in _clr.AppDomains)
            {
                ulong? timerQueue = (ulong?)instanceField.GetValue(domain);
                if (!timerQueue.HasValue || timerQueue.Value == 0)
                    continue;

                ClrType t = _heap.GetObjectType(timerQueue.Value);
                if (t == null)
                    continue;

                // m_timers is the start of the list of TimerQueueTimer
                var currentPointer = GetFieldValue(timerQueue.Value, "m_timers");

                while ((currentPointer != null) && (((ulong)currentPointer) != 0))
                {
                    // currentPointer points to a TimerQueueTimer instance
                    ulong currentTimerQueueTimerRef = (ulong)currentPointer;

                    TimerInfo ti = new TimerInfo()
                    {
                        TimerQueueTimerAddress = currentTimerQueueTimerRef
                    };

                    var val = GetFieldValue(currentTimerQueueTimerRef, "m_dueTime");
                    ti.DueTime = (uint)val;
                    val = GetFieldValue(currentTimerQueueTimerRef, "m_period");
                    ti.Period = (uint)val;
                    val = GetFieldValue(currentTimerQueueTimerRef, "m_canceled");
                    ti.Cancelled = (bool)val;
                    val = GetFieldValue(currentTimerQueueTimerRef, "m_state");
                    ti.StateTypeName = "";
                    if (val == null)
                    {
                        ti.StateAddress = 0;
                    }
                    else
                    {
                        ti.StateAddress = (ulong)val;
                        var stateType = _heap.GetObjectType(ti.StateAddress);
                        if (stateType != null)
                        {
                            ti.StateTypeName = stateType.Name;
                        }
                    }

                    // decypher the callback details
                    val = GetFieldValue(currentTimerQueueTimerRef, "m_timerCallback");
                    if (val != null)
                    {
                        ulong elementAddress = (ulong)val;
                        if (elementAddress == 0)
                            continue;

                        var elementType = _heap.GetObjectType(elementAddress);
                        if (elementType != null)
                        {
                            if (elementType.Name == "System.Threading.TimerCallback")
                            {
                                ti.MethodName = BuildTimerCallbackMethodName(elementAddress);
                            }
                            else
                            {
                                ti.MethodName = "<" + elementType.Name + ">";
                            }
                        }
                        else
                        {
                            ti.MethodName = "{no callback type?}";
                        }
                    }
                    else
                    {
                        ti.MethodName = "???";
                    }

                    yield return ti;

                    currentPointer = GetFieldValue(currentTimerQueueTimerRef, "m_next");
                }
            }
        }
        public Dictionary<string, int> ComputeDuplicatedStrings()
        {
            Dictionary<string, int> strings = new Dictionary<string, int>(1024 * 1024);
            if (!_heap.CanWalkHeap)
                return null;

            ClrModule mscorlib = GetMscorlib();
            if (mscorlib == null)
                return null;

            var stringType = mscorlib.GetTypeByName("System.String");
            if (stringType == null)
                return null;

            foreach (var address in _heap.EnumerateObjectAddresses())
            {
                try
                {
                    var objType = _heap.GetObjectType(address);
                    if (objType == null)
                        continue;

                    if (objType != stringType)
                        continue;

                    var obj = objType.GetValue(address);
                    string s = obj as string;
                    if (!strings.ContainsKey(s))
                    {
                        strings[s] = 0;
                    }

                    strings[s] = strings[s] + 1;
                }
                catch (Exception x)
                {
                    Debug.WriteLine(x);
                    // some InvalidOperationException seems to occur  :^(
                }
            }

            return strings;
        }

        public IReadOnlyList<PinnedObjectsGeneration> ComputePinnedObjects()
        {
            // gen0, 1, 2 and LOH
            const int MaxGenerations = 4;

            PinnedObjectsGeneration[] generations = new PinnedObjectsGeneration[MaxGenerations];
            for (int i = 0; i < MaxGenerations; i++)
            {
                generations[i] = new PinnedObjectsGeneration(i);
            }

            foreach (var gcHandle in _clr.EnumerateHandles())
            {
                if (!gcHandle.IsPinned)
                    continue;

                // address of the object pinned by the handle
                var address = gcHandle.Object;
                var segment = _heap.GetSegmentByAddress(address);
                if (segment != null)
                {
                    var generation = segment.GetGeneration(address);

                    // take care of LOH case
                    if ((generation == 2) && (segment.IsLarge))
                        generation = 3;

                    generations[generation].AddHandle(gcHandle);
                }
                else
                {
                    // should never occur
                }
            }

            return generations;
        }

        public IReadOnlyList<SegmentInfo> ComputeGCSegments()
        {
            // merge ClrSegments
            List<SegmentInfo> segments = new List<SegmentInfo>();
            var heapSegments = _heap.Segments;
            var segmentsInfo = new List<SegmentInfo>(heapSegments.Count);
            foreach (ClrSegment segment in heapSegments)
            {
                var number = segment.ProcessorAffinity;
                var segmentInfo = segments.FirstOrDefault(s => s.Number == number);
                if (segmentInfo == null)
                {
                    segmentInfo = new SegmentInfo(number);
                    segments.Add(segmentInfo);
                }

                MergeSegment(segment, segmentInfo);
            }

            // dispatch pinned objects to the right segment/generation
            var pinnedObjectsCount = 0;
            foreach (var gcHandle in _clr.EnumerateHandles())
            {
                if (!gcHandle.IsPinned)
                    continue;

                // address of the object pinned by the handle
                var address = gcHandle.Object;
                var segment = _heap.GetSegmentByAddress(address);
                if (segment != null)
                {
                    var generation = segment.GetGeneration(address);

                    // take care of LOH case
                    if ((generation == 2) && (segment.IsLarge))
                    {
                        generation = 3;
                    }

                    var genInSegment = GetGeneration(segments, address);

                    Debug.Assert(genInSegment != null);
                    Debug.Assert(genInSegment.Generation == generation);

                    pinnedObjectsCount++;
                    genInSegment.AddPinnedObject(gcHandle);
                }
                else
                {
                    // should never occur
                }
            }

            return segments;
        }


    #region internal helpers
    #endregion
        private GenerationInSegment GetGeneration(List<SegmentInfo> segments, ulong address)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var generation = segments[i].GetGenerationFromAddress(address);
                if (generation == null)
                    continue;

                return generation;
            }

            // should never happen
            Debug.Fail(string.Format("Impossible to find generation for {0,x}", address));
            return null;
        }

        private void MergeSegment(ClrSegment segment, SegmentInfo info)
        {
            // if LOH, just one generation in this segment
            if (segment.IsLarge)
            {
                // add only an LOH generation in segment info
                info.AddGenerationInSegment(3, segment.Gen2Start, segment.Gen2Start + segment.Gen2Length, segment.Gen2Length);
                return;
            }

            // contains gen0 and gen1 only if ephemeral
            if (segment.IsEphemeral)
            {
                info.AddGenerationInSegment(0, segment.Gen0Start, segment.Gen0Start + segment.Gen0Length, segment.Gen0Length);
                info.AddGenerationInSegment(1, segment.Gen1Start, segment.Gen1Start + segment.Gen1Length, segment.Gen1Length);
            }

            // always add gen2
            info.AddGenerationInSegment(2, segment.Gen2Start, segment.Gen2Start + segment.Gen2Length, segment.Gen2Length);
        }

        private IEnumerable<ThreadPoolItem> EnumerateThreadPoolWorkQueue(ulong workQueueRef)
        {
            // start from the tail and follow the Next
            var proxy = _heap.GetProxy(workQueueRef);
            var currentQueueSegment = proxy.queueTail;

            while (currentQueueSegment != null)
            {
                // get the System.Threading.ThreadPoolWorkQueue+QueueSegment nodes array
                var nodes = currentQueueSegment.nodes;
                if (nodes == null)
                    continue;

                foreach (var item in nodes)
                {
                    if (item == null)
                        continue;

                    yield return GetThreadPoolItem(item);
                }

                currentQueueSegment = currentQueueSegment.Next;
            }
        }
        private IEnumerable<ThreadPoolItem> EnumerateThreadPoolStealingQueue(dynamic stealingQueue)
        {
            var array = stealingQueue.m_array;
            if (array == null)
                yield break;

            foreach (var item in array)
            {
                if (item == null)
                    continue;

                yield return GetThreadPoolItem(item);
            }
        }
        private ThreadPoolItem GetThreadPoolItem(dynamic item)
        {
            // get the ClrType directly from the dynamic proxy
            ClrType itemType = item.GetClrType();
            if (itemType.Name == "System.Threading.Tasks.Task")
            {
                return GetTask(item);
            }
            else if (itemType.Name == "System.Threading.QueueUserWorkItemCallback")
            {
                return GetQueueUserWorkItemCallback(item);
            }
            else
            {
                // create a raw information
                ThreadPoolItem tpi = new ThreadPoolItem()
                {
                    Type = ThreadRoot.Raw,
                    Address = (ulong)item,
                    MethodName = itemType.Name
                };

                return tpi;
            }
        }
        private ThreadPoolItem GetTask(dynamic task)
        {
            ThreadPoolItem tpi = new ThreadPoolItem()
            {
                Address = (ulong)task,
                Type = ThreadRoot.Task
            };

            // look for the context in m_action._target
            var action = task.m_action;
            if (action == null)
            {
                tpi.MethodName = " [no action]";
                return tpi;
            }

            var target = action._target;
            if (target == null)
            {
                tpi.MethodName = " [no target]";
                return tpi;
            }

            tpi.MethodName = BuildDelegateMethodName(target.GetClrType(), action);

            // get the task scheduler if any
            var taskScheduler = task.m_taskScheduler;
            if (taskScheduler != null)
            {
                var schedulerType = taskScheduler.GetClrType().ToString();
                if ("System.Threading.Tasks.ThreadPoolTaskScheduler" != schedulerType)
                    tpi.MethodName = $"{tpi.MethodName} [{schedulerType}]";
            }
            return tpi;
        }
        private ThreadPoolItem GetQueueUserWorkItemCallback(dynamic element)
        {
            ThreadPoolItem tpi = new ThreadPoolItem()
            {
                Address = (ulong)element,
                Type = ThreadRoot.WorkItem
            };

            // look for the callback given to ThreadPool.QueueUserWorkItem()
            var callback = element.callback;
            if (callback == null)
            {
                tpi.MethodName = "[no callback]";
                return tpi;
            }

            var target = callback._target;
            if (target == null)
            {
                tpi.MethodName = "[no callback target]";
                return tpi;
            }

            ClrType targetType = target.GetClrType();
            if (targetType == null)
            {
                tpi.MethodName = $" [target=0x{(ulong)target}]";
            }
            else
            {
                // look for method name
                tpi.MethodName = BuildDelegateMethodName(targetType, callback);
            }

            return tpi;
        }
        internal string BuildDelegateMethodName(ClrType targetType, dynamic action)
        {
            var methodPtr = action._methodPtr;
            if (methodPtr != null)
            {
                ClrMethod method = _clr.GetMethodByAddress((ulong)methodPtr);
                if (method == null)
                {
                    // could happen in case of static method
                    methodPtr = action._methodPtrAux;
                    method = _clr.GetMethodByAddress((ulong)methodPtr);
                }

                if (method != null)
                {
                    // anonymous method
                    if (method.Type.Name == targetType.Name)
                    {
                        return $"{targetType.Name}.{method.Name}";
                    }
                    else
                    // method is implemented by an class inherited from targetType
                    // ... or a simple delegate indirection to a static/instance method
                    {
                        if (
                            (targetType.Name == "System.Threading.WaitCallback") ||
                             targetType.Name.StartsWith("System.Action<")
                            )
                        {
                            return $"{method.Type.Name}.{method.Name}";
                        }
                        else
                        {
                            return $"({targetType.Name}){method.Type.Name}.{method.Name}";
                        }
                    }
                }
            }

            return "";
        }

        private ThreadPoolItem GetTask(ulong taskRef)
        {
            ThreadPoolItem tpi = new ThreadPoolItem()
            {
                Address = taskRef,
                Type = ThreadRoot.Task
            };

            // look for the context in m_action._target
            ulong actionRef = (ulong)GetFieldValue(taskRef, "m_action");
            if (actionRef == 0)
            {
                tpi.MethodName = " [no action]";
                return tpi;
            }

            ulong targetRef = (ulong)GetFieldValue(actionRef, "_target");
            if (targetRef == 0)
            {
                tpi.MethodName = " [no target]";
                return tpi;
            }
            else
            {
                tpi.MethodName = BuildDelegateMethodName(_heap.GetObjectType(targetRef), actionRef);

                // look for the task scheduler
                var taskScheduler = GetFieldValue(targetRef, "m_taskScheduler");
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
                        tpi.MethodName = $"{tpi.MethodName} [ {_heap.GetObjectType(schedulerRef).ToString()}]";
                    }
                }
            }
            return tpi;
        }
        private ThreadPoolItem GetQueueUserWorkItemCallback(ulong elementRef)
        {
            ThreadPoolItem tpi = new ThreadPoolItem()
            {
                Address = elementRef,
                Type = ThreadRoot.WorkItem
            };

            // look for the callback given to ThreadPool.QueueUserWorkItem()
            ulong callbackRef = (ulong)GetFieldValue(elementRef, "callback");
            if (callbackRef == 0)
            {
                tpi.MethodName = "[no callback]";
                return tpi;
            }
            else
            {
                ulong targetRef = (ulong)GetFieldValue(callbackRef, "_target");
                if (targetRef == 0)
                {
                    tpi.MethodName = "[no callback target]";
                    return tpi;
                }
                else
                {
                    ClrType targetType = _heap.GetObjectType(targetRef);
                    if (targetType == null)
                    {
                        tpi.MethodName = $" [target=0x{targetRef}]";
                        return tpi;
                    }
                    else
                    {
                        // look for method name
                        tpi.MethodName = BuildDelegateMethodName(targetType, callbackRef);
                    }
                }
            }

            return tpi;
        }
        internal string BuildDelegateMethodName(ClrType targetType, ulong delegateRef)
        {
            var methodPtr = GetFieldValue(delegateRef, "_methodPtr");
            if (methodPtr != null)
            {
                ClrMethod method = _clr.GetMethodByAddress((ulong)(long)methodPtr);
                if (method != null)
                {
                    if (method.Type.Name == targetType.Name)
                    {
                        return $"{targetType.Name}.{method.Name}";
                    }
                    else  // method is implemented by an class inherited from targetType
                    {
                        return $"({targetType.Name}){method.Type.Name}.{method.Name}";
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
        private string BuildTimerCallbackMethodName(ulong timerCallbackRef)
        {
            var methodPtr = GetFieldValue(timerCallbackRef, "_methodPtr");
            if (methodPtr != null)
            {
                ClrMethod method = _clr.GetMethodByAddress((ulong)(long)methodPtr);
                if (method != null)
                {
                    // look for "this" to figure out the real callback implementor type
                    string thisTypeName = "?";
                    var thisPtr = GetFieldValue(timerCallbackRef, "_target");
                    if ((thisPtr != null) && ((ulong) thisPtr) != 0)
                    {
                        ulong thisRef = (ulong) thisPtr;
                        var thisType = _heap.GetObjectType(thisRef);
                        if (thisType != null)
                        {
                            thisTypeName = thisType.Name;
                        }
                    }
                    else
                    {
                        thisTypeName = (method.Type != null) ? method.Type.Name : "?";
                    }
                    return $"{thisTypeName}.{method.Name}";
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
        private object GetFieldValue(ulong address, string fieldName)
        {
            var type = _heap.GetObjectType(address);
            ClrInstanceField field = type.GetFieldByName(fieldName);
            if (field == null)
                return null;

            return field.GetValue(address);
        }
    }
}
