# gsose

or **G**rand **S**on **O**f **S**trike **E**xtension for WinDBG

This extension provides several commands to analyze .NET applications memory dumps. Once loaded, feel free to call !help to get more details about the usage and output of each command.

```-------------------------------------------------------------------------------
gsose is a debugger extension DLL designed to dig into CLR data structures.
Functions are listed by category and shortcut names are listed in parenthesis.
Type "!help " for detailed info on that function.

Thread Pool                       Timers
-----------------------------     -----------------------------
TpQueue(tpq)                      TimerInfo (ti)
TpRunning(tpr)

Tasks                             Strings
-----------------------------     -----------------------------
TkState (tks)                     StringDuplicates (sd)
GetMethodName (gmn)

Data structures
-----------------------------
DumpConcurrentDictionary (dcd)
DumpConcurrentQueue (dcq)

Garbage Collector
-----------------------------
GCInfo (gci)
PinnedObjects (po)
```



## TpQueue (tpq)
```
    !TpQueue lists the enqueued workitems in the Clr Thread Pool followed by a summary of the different tasks/work items.
    The global queue is first iterated before local per-thread queues.
    The name of the method to be called (on which instance if any) is also provided when available.
    
    0:000> !tpq
    global work item queue________________________________
    0x00000265CC2A92C8 Task | ThreadPoolUseCases.MainWindow.b__10_0
    0x00000265CC2A9408 Task | ThreadPoolUseCases.MainWindow.b__10_0
    0x00000265CC2A9548 Task | ThreadPoolUseCases.MainWindow.b__10_0
    
    local per thread work items_____________________________________
    
        3 Task ThreadPoolUseCases.MainWindow.b__10_0
     ----
        3
```



## TpRunning (tpr)

```
!TpRunning lists the work items current run by the Clr Thread Pool threads followed by a summary of the different tasks/work items.
The consummed CPU is displayed with the number of running/dead/max threads in the Thread Pool.
For each thread, its ID, ThreadOBJ address, number of locks and details are provided.
The details contain the name of the callback method and the synchronization object address on which it is blocked it any (as a parameter of the corresponding method such as WaitHandle.WaitOneNative).

0:000> !tpr

CPU = 12% for 50 threads (#idle = 0 + #running = 50 | #dead = 0 | #max = 50)
-----------------------------------------------------------------------------------
   ID ThreadOBJ        Locks  Details

  24 000001DB2F549430  0001  Task | ThreadPoolUseCases.MainWindow.b__13_2(System.Object)
  34 000001DB359FE750  0001  Work | ThreadPoolUseCases.MainWindow.b__13_3(System.Object)
   4 000001DB2F516180        Task | ThreadPoolUseCases.MainWindow.b__13_0(System.Object)
                          => WaitHandle.WaitOneNative(0x2040489605328 : SafeWaitHandle
                          ...
  52 000001DB359CCCD0        Work | ThreadPoolUseCases.MainWindow.b__13_4(System.Object)
  53 000001DB359CF3E0        Work | ThreadPoolUseCases.MainWindow.b__13_4(System.Object)

____________________________________________________________________________________________________
Count Details
----------------------------------------------------------------------------------------------------
    1 Task | ThreadPoolUseCases.MainWindow.b__13_2(System.Object)
    1 Work | ThreadPoolUseCases.MainWindow.b__13_3(System.Object)
    9 Task | ThreadPoolUseCases.MainWindow.b__13_2(System.Object)
                              => Monitor.Enter

    9 Work | ThreadPoolUseCases.MainWindow.b__13_3(System.Object)
                              => Monitor.Enter


   10 Task | ThreadPoolUseCases.MainWindow.b__13_0(System.Object)
                                  => WaitHandle.WaitOneNative(0x2040489605328 : SafeWaitHandle

   10 Work | ThreadPoolUseCases.MainWindow.b__13_1(System.Object)
                                  => WaitHandle.WaitOneNative(0x2040489605328 : SafeWaitHandle

   10 Work | ThreadPoolUseCases.MainWindow.b__13_4(System.Object)
 ----
   50
```



## TimerInfo (ti)

```
!TimerInfo lists all the running timers followed by a summary of the different items.
The global queue is first iterated before local per-thread queues.
The name of the method to be called (on which instance if any) is also provided when available.

0:000> !ti
0x0000022836D57410 @    2000 ms every     2000 ms |  0000022836D573D8 (ThreadPoolUseCases.MainWindow+TimerInfo) -> ThreadPoolUseCases.MainWindow.OnTimerCallback
0x0000022836D575A0 @    5000 ms every     5000 ms |  0000022836D57568 (ThreadPoolUseCases.MainWindow+TimerInfo) -> ThreadPoolUseCases.MainWindow.OnTimerCallback

   2 timers
-----------------------------------------------
   1 | 0x0000022836D57410 @    2000 ms every     2000 ms |  0000022836D573D8 (ThreadPoolUseCases.MainWindow+TimerInfo) -> ThreadPoolUseCases.MainWindow.OnTimerCallback
   1 | 0x0000022836D575A0 @    5000 ms every     5000 ms |  0000022836D57568 (ThreadPoolUseCases.MainWindow+TimerInfo) -> ThreadPoolUseCases.MainWindow.OnTimerCallback

```



## TkState (tks)

```
!TkState [hexa address]
         [decimal state value]

!TkState translates a Task m_stateFlags field value into text.
It supports direct decimal value or hexacimal address correspondig to a task instance.

0:000> !tkstate 000001db16cf98f0
Task state = Running
0:000> !tkstate 204800
Task state = Running
```



## GetMethodName (gmn)

```
!GetMethodName [hexa address]

!GetMethodName translates an address to a method into this method name (namespaces.type.method)
0:000> !gmn 7fe886000b0
TestTimers.TimerTester.ValidateScore
```



## StringDuplicates (sd)

```
!StringDuplicates [duplication threshold]

!StringDuplicates lists strings duplicated more than the given threshold (100 by default)sorted by memory consumption.
Note that new lines are replaced by '##' to keep each string on one line.

0:000> !sd 5
       6           24 fr
       6           60 Color
       9           90 fr-FR
      10          100 Value
       6          120 Background
      10          220 application
      35          280 Name
       8         1968 System.Configuration.IgnoreSection, System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
-------------------------------------------------------------------------
                    0 MB
```


## DumpConcurrentDictionary (dcd)

```
!DumpConcurrentDictionary

!dcd lists all items in the given concurrent dictionary
0:000> !dcd 000001d10df6daa0
System.Collections.Concurrent.ConcurrentDictionary<System.Int32,NetCoreConsoleApp.InstanceInConcurrentDataStructures>
 2237 buckets
0 = 0x000001D10DF5B420 (NetCoreConsoleApp.InstanceInConcurrentDataStructures)
1 = 0x000001D10DF5B438 (NetCoreConsoleApp.InstanceInConcurrentDataStructures)
2 = 0x000001D10DF5B450 (NetCoreConsoleApp.InstanceInConcurrentDataStructures)
3 = 0x000001D10DF5B468 (NetCoreConsoleApp.InstanceInConcurrentDataStructures)
4 = 0x000001D10DF5B480 (NetCoreConsoleApp.InstanceInConcurrentDataStructures)
...
```


## DumpConcurrentQueue (dcq)

```
!DumpConcurrentQueue

!dcq lists all items in the given concurrent queue. Show each item type with -t as parameter
0:000> !dcq 000001d10df67420 -t
   1 - 0x000001D10DF5B420 | NetCoreConsoleApp.InstanceInConcurrentDataStructures
   2 - 0x000001D10DF5B438 | NetCoreConsoleApp.InstanceInConcurrentDataStructures
   3 - 0x000001D10DF5B450 | NetCoreConsoleApp.InstanceInConcurrentDataStructures
   4 - 0x000001D10DF5B468 | NetCoreConsoleApp.InstanceInConcurrentDataStructures
   5 - 0x000001D10DF5B480 | NetCoreConsoleApp.InstanceInConcurrentDataStructures
   ...
```


## GCInfo (gci)

```
!GCInfo

!GCInfo lists generations per segments. Show pinned objects with -pinned and object instances count/size with -stat (by default)
0:000> !gci -pinned
13 - 7 generations
    LOH | 9F06001000 - 9F0CDDB8C8 (   115,189,960)

   gen2 | A26FA81000 - A282F860C0 (   324,030,656)
          AsyncPinned | a2785c9308 System.Byte[4096]
          AsyncPinned | a2785c9520 System.Byte[4096]
          ...
          AsyncPinned | a27896ebd8 System.Threading.OverlappedData
          AsyncPinned | a278970a78 System.Threading.OverlappedData

   gen1 | A282F860C0 - A2838F2208 (     9,879,880)
          AsyncPinned | a283311f08 System.Byte[4096]

   gen0 | A2838F2208 - A287958850 (    67,528,264)
          AsyncPinned | a28392fd78 System.Byte[4096]
          AsyncPinned | a2839f9a20 System.Byte[4096]
          AsyncPinned | a283aeca60 System.Byte[4096]

    LOH | A563A81000 - A566FD5968 (    55,921,000)
          AsyncPinned | a565405fe0 System.Byte[96000]
          AsyncPinned | a566f38918 System.Byte[96000]
          AsyncPinned | a566fbc950 System.Byte[96000]

    LOH | A5C7131000 - A5CB131038 (    67,108,920)

    LOH | A674221000 - A679992490 (    91,690,128)
   ...
```

## PinnedObjects (po)

```
!PinnedObjects [minimum instance count threshold to be listed]

!PinnedObjects lists pinned objects (Pinned/asyncPinned) per generation sorted by type name
0:000> !po 3
Gen0: 64
   System.String : 64
   -      Pinned | 1c50235d7c0
   ...
   -      Pinned | 1c5024d5c60
Gen1: 0
Gen2: 115
LOH: 71
   System.Object[] : 7
   - AsyncPinned | 1c512231038
   ...
   - AsyncPinned | 1c5122518f8
   System.String : 64
   -      Pinned | 1c512252130
   ...
   -      Pinned | 1c512c8abe0
-------------------------------------------------------------------------
Total: 250 pinned object
```