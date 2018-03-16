using System;
using System.Runtime.InteropServices;
using RGiesecke.DllExport;

namespace gsose
{
    public partial class DebuggerExtensions
    {
        [DllExport("Help")]
        public static void Help(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnHelp(client, args);
        }
        [DllExport("help")]
        public static void help(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnHelp(client, args);
        }

        const string _help =
        "-------------------------------------------------------------------------------\r\n"+
        "gsose is a debugger extension DLL designed to dig into CLR data structures.\r\n" +
        "Functions are listed by category and shortcut names are listed in parenthesis.\r\n" +
        "Type \"!help <functionname>\" for detailed info on that function.\r\n"+
        "\r\n"+
        "Thread Pool                       Timers\r\n"+
        "-----------------------------     -----------------------------\r\n"+
        "TpQueue(tpq)                      TimerInfo (ti)\r\n"+
        "TpRunning(tpr)\r\n"+
        "\r\n"+
        "Tasks                             Strings\r\n"+
        "-----------------------------     -----------------------------\r\n"+
        "TkState (tks)                     StringDuplicates (sd)\r\n" +
        "GetMethodName (gmn)                                    \r\n";
        //
        //
        const string _tpqHelp =
        "-------------------------------------------------------------------------------\r\n" +
        "!TpQueue \r\n" +
        "\r\n" +
        "!TpQueue lists the enqueued workitems in the Clr Thread Pool followed by a summary of\r\n"+
        "the different tasks/work items.\r\n" +
        "The global queue is first iterated before local per-thread queues.\r\n" +
        "The name of the method to be called (on which instance if any) is also provided when available.\r\n" +
        "\r\n" +
        "    0:000> !tpq\r\n" +
        "    global work item queue________________________________\r\n" +
        "    0x00000265CC2A92C8 Task | ThreadPoolUseCases.MainWindow.b__10_0\r\n" +
        "    0x00000265CC2A9408 Task | ThreadPoolUseCases.MainWindow.b__10_0\r\n" +
        "    0x00000265CC2A9548 Task | ThreadPoolUseCases.MainWindow.b__10_0\r\n" +
        "\r\n" +
        "    local per thread work items_____________________________________\r\n" +
        "\r\n" +
        "        3 Task ThreadPoolUseCases.MainWindow.b__10_0\r\n" +
        "     ----\r\n" +
        "        3\r\n";
        //
        //
        const string _tprHelp =
        "-------------------------------------------------------------------------------\r\n" +
        "!TpRunning \r\n" +
        "\r\n" +
        "!TpRunning lists the work items current run by the Clr Thread Pool threads followed by\r\n" +
        "a summary of the different tasks/work items.\r\n" +
        "The consummed CPU is displayed with the number of running/dead/max threads in the Thread Pool.\r\n" +
        "For each thread, its ID, ThreadOBJ address, number of locks and details are provided.\r\n" +
        "The details contain the name of the callback method and the synchronization object address on which\r\n"+
        "it is blocked it any (as a parameter of the corresponding method such as WaitHandle.WaitOneNative).\r\n" +
        "\r\n" +
        "0:000> !tpr\r\n"+
        "\r\n" +
        "CPU = 12%% for 50 threads (#idle = 0 + #running = 50 | #dead = 0 | #max = 50)\r\n" +
        "\r\n" +
        "  ID ThreadOBJ        Locks  Details\r\n" +
        "-----------------------------------------------------------------------------------\r\n" +
        "  24 000001DB2F549430  0001  Task | ThreadPoolUseCases.MainWindow.b__13_2(System.Object)\r\n" +
        "  34 000001DB359FE750  0001  Work | ThreadPoolUseCases.MainWindow.b__13_3(System.Object)\r\n" +
        "   4 000001DB2F516180        Task | ThreadPoolUseCases.MainWindow.b__13_0(System.Object)\r\n" +
        "                          => WaitHandle.WaitOneNative(0x2040489605328 : SafeWaitHandle\r\n" +
        "                          ...\r\n" +
        "  52 000001DB359CCCD0        Work | ThreadPoolUseCases.MainWindow.b__13_4(System.Object)\r\n" +
        "  53 000001DB359CF3E0        Work | ThreadPoolUseCases.MainWindow.b__13_4(System.Object)\r\n" +
        "\r\n" +
        "\r\n" +
        "____________________________________________________________________________________________________\r\n" +
        "Count Details\r\n" +
        "----------------------------------------------------------------------------------------------------\r\n" +
        "    1 Task | ThreadPoolUseCases.MainWindow.b__13_2(System.Object)\r\n" +
        "    1 Work | ThreadPoolUseCases.MainWindow.b__13_3(System.Object)\r\n" +
        "    9 Task | ThreadPoolUseCases.MainWindow.b__13_2(System.Object)\r\n" +
        "                                  => Monitor.Enter\r\n" +
        "\r\n" +
        "    9 Work | ThreadPoolUseCases.MainWindow.b__13_3(System.Object)\r\n" +
        "                                  => Monitor.Enter\r\n" +
        "\r\n" +
        "   10 Task | ThreadPoolUseCases.MainWindow.b__13_0(System.Object)\r\n" +
        "                                  => WaitHandle.WaitOneNative(0x2040489605328 : SafeWaitHandle\r\n" +
        "\r\n" +
        "   10 Work | ThreadPoolUseCases.MainWindow.b__13_1(System.Object)\r\n" +
        "                                  => WaitHandle.WaitOneNative(0x2040489605328 : SafeWaitHandle\r\n" +
        "\r\n" +
        "   10 Work | ThreadPoolUseCases.MainWindow.b__13_4(System.Object)\r\n" +
        " ----\r\n" +
        "   50\r\n" +
        "\r\n";
        //
        //
        const string _tiHelp =
        "-------------------------------------------------------------------------------\r\n" +
        "!TimerInfo \r\n" +
        "\r\n" +
        "!TimerInfo lists all the running timers followed by a summary of the different items.\r\n" +
        "The global queue is first iterated before local per-thread queues.\r\n" +
        "The name of the method to be called (on which instance if any) is also provided when available.\r\n" +
        "\r\n" +
        "0:000> !ti\r\n" +
        "0x0000022836D57410 @    2000 ms every     2000 ms |  0000022836D573D8 (ThreadPoolUseCases.MainWindow+TimerInfo) -> ThreadPoolUseCases.MainWindow.OnTimerCallback\r\n" +
        "0x0000022836D575A0 @    5000 ms every     5000 ms |  0000022836D57568 (ThreadPoolUseCases.MainWindow+TimerInfo) -> ThreadPoolUseCases.MainWindow.OnTimerCallback\r\n" +
        "\r\n" +
        "   2 timers\r\n" +
        "-----------------------------------------------\r\n" +
        "   1 | 0x0000022836D57410 @    2000 ms every     2000 ms |  0000022836D573D8 (ThreadPoolUseCases.MainWindow+TimerInfo) -> ThreadPoolUseCases.MainWindow.OnTimerCallback\r\n" +
        "   1 | 0x0000022836D575A0 @    5000 ms every     5000 ms |  0000022836D57568 (ThreadPoolUseCases.MainWindow+TimerInfo) -> ThreadPoolUseCases.MainWindow.OnTimerCallback\r\n";
        //
        //
        const string _tksHelp =
        "-------------------------------------------------------------------------------\r\n" +
        "!TkState [hexa address]\r\n" +
        "         [decimal state value]\r\n" +
        "\r\n" +
        "!TkState translates a Task m_stateFlags field value into text.\r\n" +
        "It supports direct decimal value or hexacimal address correspondig to a task instance.\r\n" +
        "\r\n" +
        "0:000> !tkstate 000001db16cf98f0\r\n" +
        "Task state = Running\r\n" +
        "0:000> !tkstate 204800\r\n" +
        "Task state = Running\r\n";
        //
        //
        const string _sdHelp =
        "-------------------------------------------------------------------------------\r\n" +
        "!StringDuplicates [duplication threshold]\r\n" +
        "\r\n" +
        "!StringDuplicates lists strings duplicated more than the given threshold (100 by default)" +
        "sorted by memory consumption.\r\n" +
        "Note that new lines are replaced by '##' to keep each string on one line.\r\n" +
        "\r\n" +
        "0:000> !sd 5\r\n" +
        "       6           24 fr\r\n" +
        "       6           60 Color\r\n" +
        "       9           90 fr-FR\r\n" +
        "      10          100 Value\r\n" +
        "       6          120 Background\r\n" +
        "      10          220 application\r\n" +
        "      35          280 Name\r\n" +
        "       8         1968 System.Configuration.IgnoreSection, System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\r\n" +
        "-------------------------------------------------------------------------\r\n" +
        "                    0 MB\r\n";
        //
        //
        const string _gmnHelp =
            "-------------------------------------------------------------------------------\r\n" +
            "!GetMethodName [hexa address]\r\n" +
            "\r\n" +
            "!GetMethodName translates an address to a method into this method name (namespaces.type.method)" +
            "\r\n" +
            "0:000> !gmn 7fe886000b0\r\n" +
            "TestTimers.TimerTester.ValidateScore\r\n" +
            "-------------------------------------------------------------------------\r\n" +
            "\r\n";
        //
        private static void OnHelp(IntPtr client, string args)
        {
            // Must be the first thing in our extension.
            if (!InitApi(client))
                return;

            string command = args;
            if (args != null)
                command = args.ToLower();

            switch (command)
            {
                case "tpr":
                case "tprunning":
                    Console.WriteLine(_tprHelp);
                    break;

                case "tpq":
                case "tpQueue":
                    Console.WriteLine(_tpqHelp);
                    break;

                case "ti":
                case "timerinfo":
                    Console.WriteLine(_tiHelp);
                    break;

                case "tks":
                case "taskstate":
                    Console.WriteLine(_tksHelp);
                    break;

                case "sd":
                case "stringduplicates":
                    Console.WriteLine(_sdHelp);
                    break;

                case "gmn":
                case "getmethodname":
                    Console.WriteLine(_gmnHelp);
                    break;

                default:
                    Console.WriteLine(_help);
                    break;
            }
        }
    }
}