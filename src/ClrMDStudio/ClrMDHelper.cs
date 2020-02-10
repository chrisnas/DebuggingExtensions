using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ClrMDStudio
{
    public class FreeBlock
    {
        public FreeBlock(ulong address, ulong size)
        {
            Address = address;
            Size = size;
        }
        public ulong Address;
        public ulong Size;
    }

    public class GenerationInSegment
    {
        private List<(ClrHandle handle, string typeDescription)> _pinnedObjects;
        
        public GenerationInSegment(ulong[] instances, IReadOnlyList<FreeBlock> freeBlocks, int count, ulong size)
        {
            FreeBlocks = freeBlocks;
            FreeBlocksCount = count;
            FreeBlocksSize = size;
            InstancesAddresses = instances;
            _pinnedObjects = new List<(ClrHandle, string)>();
        }

        public int Generation { get; set; }
        public ulong Start { get; set; }
        public ulong End { get; set; }
        public ulong Length { get; set; }
        public ulong[] InstancesAddresses { get; }
        public IReadOnlyList<(ClrHandle handle, string typeDescription)> PinnedObjects =>
            _pinnedObjects.OrderBy(po => po.handle.Object).ToList();
        public IReadOnlyList<FreeBlock> FreeBlocks { get; }
        public int FreeBlocksCount { get; }
        public ulong FreeBlocksSize { get; }

        internal void AddPinnedObject(ClrHandle pinnedObject)
        {
            // we need to be in the right thread to call GetArrayLength()
            // --> it would fail in the UI thread
            _pinnedObjects.Add((pinnedObject, GetTypeDescription(pinnedObject)));
        }
        private string GetTypeDescription(ClrHandle clrHandle)
        {
            var clrType = clrHandle.Type;
            if (clrType.IsArray)
            {
                // show the array size
                return $"{clrType.ComponentType}[{clrType.GetArrayLength(clrHandle.Object).ToString()}]";
            }

            return clrHandle.Type.ToString();
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
        internal void AddGenerationInSegment(
            int generation, ulong start, ulong end, ulong length,
            ulong[] instances, int count, ulong size,
            IReadOnlyList<FreeBlock> freeBlocks
            )
        {
            _generations.Add(
                new GenerationInSegment(instances, freeBlocks, count, size)
                {
                    Generation = generation,
                    Start = start,
                    End = end,
                    Length = length,
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

    public class StringStatistics
    {
        public readonly ulong TotalHeapSize;
        public readonly Dictionary<string, int> Strings;
        public readonly GenStatistics[] Stats;

        private const int MaxGen = 4;
        public StringStatistics(ClrHeap heap)
        {
            TotalHeapSize = heap.TotalHeapSize;
            Strings = new Dictionary<string, int>(1024 * 1024);
            Stats = new GenStatistics[MaxGen];

            for (int gen = 0; gen < MaxGen; gen++) Stats[gen] = new GenStatistics(heap.GetSizeByGen(gen));
        }
    }
    public struct GenStatistics
    {
        public ulong Count;
        public ulong DuplicatedCount;
        public ulong TotalCount;
        public ulong Size;
        public ulong DuplicatedSize;
        public readonly ulong TotalSize;

        public GenStatistics(ulong totalSize)
        {
            TotalSize = totalSize;
            Count = 0;
            DuplicatedCount = 0;
            TotalCount = 0;
            Size = 0;
            DuplicatedSize = 0;
        }
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
            _heap = clr.Heap;

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
            {
                if (string.IsNullOrEmpty(module.AssemblyName))
                    continue;

                var name = module.AssemblyName.ToLower();

                // in .NET Framework
                if (name.Contains("mscorlib"))
                    return module;

                // in .NET Core
                if (name.Contains("corelib"))
                    return module;
            }

            // Ok...not sure why we couldn't find it.
            return null;
        }


    #region public API
    #endregion
        public static IEnumerable<(dynamic key, dynamic value)> EnumerateConcurrentDictionary(dynamic concurrentDictionary, bool isNetCore)
        {
            return (isNetCore) ? EnumerateConcurrentDictionaryCore(concurrentDictionary) : EnumerateConcurrentDictionaryFramework(concurrentDictionary);
        }

        private static IEnumerable<(dynamic key, dynamic value)> EnumerateConcurrentDictionaryCore(dynamic concurrentDictionary)
        {
            var buckets = concurrentDictionary._tables._buckets;
            foreach (var bucket in buckets)
            {
                if (bucket == null) continue;

                var key = bucket._key;
                var value = bucket._value;

                yield return (key, value);
            }
        }

        private static IEnumerable<(dynamic key, dynamic value)> EnumerateConcurrentDictionaryFramework(dynamic concurrentDictionary)
        {
            var buckets = concurrentDictionary.m_tables.m_buckets;
            foreach (var bucket in buckets)
            {
                if (bucket == null) continue;

                var key = bucket.m_key;
                var value = bucket.m_value;

                yield return (key, value);
            }
        }


        public static IEnumerable<dynamic> EnumerateConcurrentQueue(dynamic concurrentQueue, bool isNetCore)
        {
            return (isNetCore) ? EnumerateConcurrentQueueCore(concurrentQueue) : EnumerateConcurrentQueueFramework(concurrentQueue);
        }

        public static IEnumerable<dynamic> EnumerateConcurrentQueueFramework(dynamic concurrentQueue)
        {
            var currentSegment = concurrentQueue.m_head;
            while (currentSegment != null)
            {
                // a segment contains an array of T stored in m_array
                var items = currentSegment.m_array;
                var count = items.Length;
                for (int current = 0; current < count; current++)
                {
                    var item = items[current];
                    if (item == null)
                        continue;

                    yield return item;
                }

                currentSegment = currentSegment.m_next;
            }
        }

        public static IEnumerable<dynamic> EnumerateConcurrentQueueCore(dynamic concurrentQueue)
        {
            var currentSegment = concurrentQueue._head;
            while (currentSegment != null)
            {
                // a segment contains an array of T stored in _slots
                var slots = currentSegment._slots;
                var count = slots.Length;
                for (int current = 0; current < count; current++)
                {
                    var slot = slots[current];
                    if (slot == null)
                        continue;

                    var item = slot.Item;
                    if (item == null)
                        continue;

                    yield return item;
                }

                currentSegment = currentSegment._nextSegment;
            }
        }

        public bool IsNetCore()
        {
            var coreLib = GetMscorlib();
            if (coreLib == null)
                throw new InvalidOperationException("Impossible to find core library");

            return (coreLib.FileName.ToLower().Contains("corelib"));
        }

       public IEnumerable<ThreadPoolItem> EnumerateGlobalThreadPoolItems()
        {
            ClrModule mscorlib = GetMscorlib();
            if (mscorlib == null)
                throw new InvalidOperationException("Impossible to find core library");

            // switch when .NET Core implementation
            if (mscorlib.FileName.ToLower().Contains("corelib"))
            {
                return EnumerateGlobalThreadPoolItemsInNetCore(mscorlib);
            }
            else
            {
                return EnumerateGlobalThreadPoolItemsInNetFramework(mscorlib);
            }
        }

        public IEnumerable<ThreadPoolItem> EnumerateLocalThreadPoolItems()
        {
            ClrModule mscorlib = GetMscorlib();
            if (mscorlib == null)
                throw new InvalidOperationException("Impossible to find core library");

            // switch when .NET Core implementation
            if (mscorlib.FileName.ToLower().Contains("corelib"))
            {
                return EnumerateLocalThreadPoolItemsInNetCore(mscorlib);
            }
            else
            {
                return EnumerateLocalThreadPoolItemsInNetFramework(mscorlib);
            }

        }


        // here is the code to enumerate thread pool items from ThreadPool.cs
        //      internal static IEnumerable<IThreadPoolWorkItem> GetQueuedWorkItems()
        //      {
        //          // Enumerate global queue
        //          foreach (IThreadPoolWorkItem workItem in ThreadPoolGlobals.workQueue.workItems)
        //          {
        //              yield return workItem;
        //          }
        //
        //          // Enumerate each local queue
        //          foreach (ThreadPoolWorkQueue.WorkStealingQueue wsq in ThreadPoolWorkQueue.WorkStealingQueueList.Queues)
        //          {
        //              if (wsq != null && wsq.m_array != null)
        //              {
        //                  IThreadPoolWorkItem[] items = wsq.m_array;
        //                  for (int i = 0; i < items.Length; i++)
        //                  {
        //                      IThreadPoolWorkItem item = items[i];
        //                      if (item != null)
        //                      {
        //                          yield return item;
        //                      }
        //                  }
        //              }
        //          }
        //      }
        // 
        // unfortunately, we can't access static fields value with ClrMD yet
        // so we need to look for instances in the whole heap
        //
        private IEnumerable<ThreadPoolItem> EnumerateGlobalThreadPoolItemsInNetCore(ClrModule mscorlib)
        {
            // in .NET Core, global queue is stored in ThreadPoolGlobals.workQueue (a ThreadPoolWorkQueue)
            // and each thread has a dedicated WorkStealingQueue stored in WorkStealingQueueList._queues (a WorkStealingQueue[])
            // 
            // until we can access static fields values in .NET Core with ClrMD, we need to browse the whole heap...
            var heap = _clr.Heap;
            if (!heap.CanWalkHeap)
                yield break;

            foreach (var objAddress in heap.EnumerateObjectAddresses())
            {
                var type = heap.GetObjectType(objAddress);
                if (type == null)
                    continue;

                if (type.Name == "System.Threading.ThreadPoolWorkQueue")
                {
                    var workQueue = heap.GetProxy(objAddress);
                    var workItems = workQueue.workItems;
                    foreach (var workItem in EnumerateConcurrentQueueCore(workItems))
                    {
                        yield return GetThreadPoolItem(workItem);
                    }
                    break;
                }
            }
        }

        private IEnumerable<ThreadPoolItem> EnumerateLocalThreadPoolItemsInNetCore(ClrModule corelib)
        {
            // in .NET Core, each thread has a dedicated WorkStealingQueue stored in WorkStealingQueueList._queues (a WorkStealingQueue[])
            // 
            // until we can access static fields values in .NET Core with ClrMD, we need to browse the whole heap...
            var heap = _clr.Heap;
            if (!heap.CanWalkHeap)
                yield break;

            foreach (var objAddress in heap.EnumerateObjectAddresses())
            {
                var type = heap.GetObjectType(objAddress);
                if (type == null)
                    continue;

                if (type.Name == "System.Threading.ThreadPoolWorkQueue+WorkStealingQueue")
                {
                    var stealingQueue = heap.GetProxy(objAddress);
                    var workItems = stealingQueue.m_array;
                    if (workItems == null)
                        continue;

                    for (int current = 0; current < workItems.Length; current++)
                    {
                        var workItem = workItems[current];
                        if (workItem == null)
                            continue;

                        yield return GetThreadPoolItem(workItem);
                    }
                }
            }
        }


        // The ThreadPool is keeping track of the pending work items into two different areas:
        // - a global queue: stored by ThreadPoolWorkQueue instances of the ThreadPoolGlobals.workQueue static field
        // - several per thread (TLS) local queues: stored in SparseArray<ThreadPoolWorkQueue+WorkStealingQueue> linked from ThreadPoolWorkQueue.allThreadQueues static fields
        // both are using arrays of Task or QueueUserWorkItemCallback
        //
        // NOTE: don't show other thread pool related topics such as timer callbacks or wait objects
        //
        private IEnumerable<ThreadPoolItem> EnumerateGlobalThreadPoolItemsInNetFramework(ClrModule mscorlib)
        {
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

        private IEnumerable<ThreadPoolItem> EnumerateLocalThreadPoolItemsInNetFramework(ClrModule mscorlib)
        {
            // look into the local stealing queues in each thread TLS
            // hopefully, they are all stored in static (one per app domain) instance
            // of ThreadPoolWorkQueue.SparseArray<ThreadPoolWorkQueue.WorkStealingQueue>
            //
            var queueType = mscorlib.GetTypeByName("System.Threading.ThreadPoolWorkQueue");
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
            // the implementation is different between .NET Framework/.NET Core 2.0 and .NET Core 2.1/2.2
            // - the former is relying on a single static TimerQueue.s_queue 
            // - the latter uses an array of TimerQueue (static TimerQueue.Instances field)
            // - TODO: the next version 2.2+ seems to have another implementation based on long/short timers
            // each queue refers to TimerQueueTimer linked list via its m_timers field
            //
            var timerQueueType = GetMscorlib().GetTypeByName("System.Threading.TimerQueue");
            if (timerQueueType == null)
                yield break;

            // .NET Core 2.1/2.2 case
            ClrStaticField instancesField = timerQueueType.GetStaticFieldByName("<Instances>k__BackingField");
            if (instancesField != null)
            {
                // should have only 1 app domain in .NET Core
                // ... but it is not working: GetValue returns null
                //foreach (ClrAppDomain domain in _clr.AppDomains)
                //{
                //    var address = instancesField.GetAddress(domain);
                //    ulong? timerQueues = (ulong?) instancesField.GetValue(domain);
                //    if (!timerQueues.HasValue || timerQueues.Value == 0)
                //        continue;

                //    ClrType t = _heap.GetObjectType(timerQueues.Value);
                //    if (t == null)
                //        continue;

                //    if (!t.IsArray)
                //        continue;

                //    var numberOfQueues = t.GetArrayLength(timerQueues.Value);
                //    for (int currentQueue = 0; currentQueue < numberOfQueues; currentQueue++)
                //    {
                //        var queueAddress = t.GetArrayElementAddress(timerQueues.Value, currentQueue);

                //        // m_timers is the start of the list of TimerQueueTimer
                //        var currentPointer = GetFieldValue(queueAddress, "m_timers");

                //        while ((currentPointer != null) && (((ulong)currentPointer) != 0))
                //        {
                //            // currentPointer points to a TimerQueueTimer instance
                //            ulong currentTimerQueueTimerRef = (ulong)currentPointer;

                //            var ti = GetTimerInfo(currentTimerQueueTimerRef);
                //            currentPointer = GetFieldValue(currentTimerQueueTimerRef, "m_next");
                //            if (ti == null)
                //                continue;

                //            yield return ti;
                //        }
                //    }
                //}

                // until the ClrMD bug to get static field value is fixed, iterate on each object of the heap
                // to find each TimerQueue and iterate on 
                foreach (var timerQueue in _heap.GetProxies("System.Threading.TimerQueue"))
                {
                    var timerQueueTimer = timerQueue.m_timers;
                    while (timerQueueTimer != null)
                    {
                        var ti = GetTimerInfo((ulong)timerQueueTimer);
                        timerQueueTimer = timerQueueTimer.m_next;

                        if (ti == null)
                            continue;

                        yield return ti;
                    }
                }
            }
            else
            {
                // .NET Framework implementation
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

                        var ti = GetTimerInfo(currentTimerQueueTimerRef);
                        if (ti == null)
                            continue;

                        yield return ti;

                        currentPointer = GetFieldValue(currentTimerQueueTimerRef, "m_next");
                    }
                }
            }
        }

        private TimerInfo GetTimerInfo(ulong currentTimerQueueTimerRef)
        {
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
                    return null;

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


            return ti;
        }


        public StringStatistics ComputeDuplicatedStringsStatistics()
        {
            if (!_heap.CanWalkHeap)
                return null;

            ClrModule mscorlib = GetMscorlib();
            if (mscorlib == null)
                return null;

            var stringType = mscorlib.GetTypeByName("System.String");
            if (stringType == null)
                return null;

            var stats = new StringStatistics(_heap);
            var strings = stats.Strings;
            foreach (var address in _heap.EnumerateObjectAddresses())
            {
                try
                {
                    var objType = _heap.GetObjectType(address);
                    if (objType == null) continue;
                    if (objType == _heap.Free) continue;

                    // can't use ClrHeap.GetGeneration because it considers loh as gen 2 
                    var gen = GetObjectGeneration(address);
                    stats.Stats[gen].TotalCount++;

                    if (objType != stringType) continue;
                    var obj = objType.GetValue(address);
                    string s = obj as string;
                    var stringSize = (ulong)s.Length * 2;  // UTF16 strings require 2 bytes per character

                    if (!strings.TryGetValue(s, out var currentCount))
                    {
                        currentCount = 0;
                    }
                    else
                    {
                        // this is a duplicated string so count it in the duplicated statistics
                        stats.Stats[gen].DuplicatedCount++;
                        stats.Stats[gen].DuplicatedSize += stringSize;
                    }
                    strings[s] = currentCount + 1;

                    stats.Stats[gen].Count++;
                    stats.Stats[gen].Size += stringSize;
                }
                catch (Exception x)
                {
                    Debug.WriteLine(x);
                    // some InvalidOperationException seems to occur  :^(
                }
            }

            return stats;
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

        private int GetObjectGeneration(ulong address)
        {
            var segment = _heap.GetSegmentByAddress(address);
            if (segment != null)
            {
                var generation = segment.GetGeneration(address);

                // take care of LOH case
                if ((generation == 2) && (segment.IsLarge))
                    generation = 3;

                return generation;
            }
            else
            {
                // should never occur if the address is in the heap
                // don't check it at the beginning of the method because
                // it is supposed to be called internally with valid 
                // addresses
                return -1;
            }
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
        public IReadOnlyList<SegmentInfo> ComputeGCSegments(bool needPinned)
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
            if (needPinned)
            {
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
            var freeObjects = 
                DispatchInstances(segment, out var freeBlocksCount, out var freeBlocksSize, out var instances);

            // if LOH, just one generation in this segment
            if (segment.IsLarge)
            {
                // add only an LOH generation in segment info
                info.AddGenerationInSegment(
                    3, segment.Gen2Start, segment.Gen2Start + segment.Gen2Length, segment.Gen2Length,
                    instances, freeBlocksCount, freeBlocksSize,
                    FilterFreeBlocks(freeObjects, segment.Gen2Start, segment.Gen2Start + segment.Gen2Length)
                    );
                return;
            }

            // contains gen0 and gen1 only if ephemeral
            if (segment.IsEphemeral)
            {
                info.AddGenerationInSegment(
                    0, segment.Gen0Start, segment.Gen0Start + segment.Gen0Length, segment.Gen0Length,
                    instances, freeBlocksCount, freeBlocksSize,
                    FilterFreeBlocks(freeObjects, segment.Gen0Start, segment.Gen0Start + segment.Gen0Length)
                    );
                info.AddGenerationInSegment(
                    1, segment.Gen1Start, segment.Gen1Start + segment.Gen1Length, segment.Gen1Length,
                    instances, freeBlocksCount, freeBlocksSize,
                    FilterFreeBlocks(freeObjects, segment.Gen1Start, segment.Gen1Start + segment.Gen1Length)
                    );
            }

            // always add gen2
            info.AddGenerationInSegment(
                2, segment.Gen2Start, segment.Gen2Start + segment.Gen2Length, segment.Gen2Length,
                instances, freeBlocksCount, freeBlocksSize,
                FilterFreeBlocks(freeObjects, segment.Gen2Start, segment.Gen2Start + segment.Gen2Length)
                );
        }

        private IReadOnlyList<FreeBlock> DispatchInstances(ClrSegment segment, 
            out int freeBlocksCount, out ulong freeBlocksSize, out ulong[] instanceAddresses)
        {
            freeBlocksSize = 0;
            freeBlocksCount = 0;
            var freeBlocks = new List<FreeBlock>(128);
            var instances = new List<ulong>(128);
            for (ulong obj = segment.FirstObject; obj != 0; obj = segment.NextObject(obj))
            {
                var type = segment.Heap.GetObjectType(obj);
                if (type.IsFree)
                {
                    var blockSize = type.GetSize(obj);
                    freeBlocksSize += blockSize;
                    freeBlocksCount++;

                    freeBlocks.Add(new FreeBlock(obj, blockSize));
                }
                else
                {
                    instances.Add(obj);
                }
            }

            instanceAddresses = instances.ToArray();
            return freeBlocks;
        }
        private IReadOnlyList<FreeBlock> FilterFreeBlocks(
            IReadOnlyList<FreeBlock> freeObjects,
            ulong start, ulong end)
        {
            return freeObjects
                    .Where(freeBlock => (freeBlock.Address >= start) && (freeBlock.Address + freeBlock.Size < end))
                    .ToList();
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
            else if ((itemType.Name == "System.Threading.QueueUserWorkItemCallback") ||
                     // new to .NET Core 
                     (itemType.Name == "System.Threading.QueueUserWorkItemCallbackDefaultContext"))
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
            // for .NET Core, the callback is stored in a different field _callback
            ClrType elementType = element.GetClrType();
            var callback = (elementType.GetFieldByName("_callback") != null)
                ? element._callback
                : (elementType.GetFieldByName("callback") != null)
                    ? element.callback
                    : null;
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
