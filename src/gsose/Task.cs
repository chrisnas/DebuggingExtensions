using Microsoft.Diagnostics.Runtime;
using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClrMDStudio;

namespace gsose
{
    public partial class DebuggerExtensions
    {

        [DllExport("tks")]
        public static void tks(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnTkState(client, args);
        }
        [DllExport("tkstate")]
        public static void tkstate(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnTkState(client, args);
        }
        [DllExport("tkState")]
        public static void tkState(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnTkState(client, args);
        }
        public static void OnTkState(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            // Must be the first thing in our extension.
            if (!InitApi(client))
                return;

            // parse the command argument
            ulong address;
            ulong stateFlag;

            if (args.StartsWith("0x"))
            {
                // remove "0x" for parsing and remove the leading 0000 that WinDBG often add in 64 bit
                args = args.Substring(2).TrimStart('0');
                if (!ulong.TryParse(args, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out address))
                {
                    Console.WriteLine("numeric value expected; either a task address or StateFlag value");
                    return;
                }

                stateFlag = GetTaskStateFromAddress(address);
                if (stateFlag == 0)
                {
                    Console.WriteLine("either a task address or a valid StateFlag expected");
                    return;
                }
            }
            else
            {
                args = args.TrimStart('0');

                if (!ulong.TryParse(args, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out address))
                {
                    if (!ulong.TryParse(args, out address))
                    {
                        Console.WriteLine("numeric value expected; either a task address or StateFlag value");
                        return;
                    }

                    // check if it is a task address
                    stateFlag = GetTaskStateFromAddress(address);
                    if (stateFlag == 0)
                    {
                        // otherwise, it might be a valid StateFlag
                        stateFlag = address;
                    }
                }
                else
                {
                    // check if it is a task address
                    stateFlag = GetTaskStateFromAddress(address);
                    if (stateFlag == 0)
                    {
                        // otherwise, it might be a valid StateFlag
                        if (!ulong.TryParse(args, out stateFlag))
                        {
                            Console.WriteLine("numeric value expected; either a task address or StateFlag value");
                            return;
                        }
                    }
                }
            }

            string state = GetTaskState(stateFlag);
            if (state != null)
            {
                Console.WriteLine("Task state = " + state);
            }
            else
            {
                Console.WriteLine("either a task address or a valid StateFlag expected");
            }
        }

        [DllExport("gmn")]
        public static void gmn(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnGetMethodName(client, args);
        }
        [DllExport("getMethodName")]
        public static void getMethodName(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnGetMethodName(client, args);
        }
        [DllExport("getmethodname")]
        public static void getmethodname(IntPtr client, [MarshalAs(UnmanagedType.LPStr)] string args)
        {
            OnGetMethodName(client, args);
        }
        private static void OnGetMethodName(IntPtr client, string args)
        {
            // Must be the first thing in our extension.
            if (!InitApi(client))
                return;

            // Use ClrMD as normal, but ONLY cache the copy of ClrRuntime (this.Runtime).  All other
            // types you get out of ClrMD (such as ClrHeap, ClrTypes, etc) should be discarded and
            // reobtained every run.
            ClrHeap heap = Runtime.GetHeap();

            // Console.WriteLine now writes to the debugger.

            ClrMDHelper helper = new ClrMDHelper(Runtime);

            try
            {
                ulong methodPtr;
                if (!ulong.TryParse(args.Trim(), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out methodPtr))
                {
                    Console.WriteLine("numeric value expected as method pointer");
                    return;
                }


                ClrMethod method = Runtime.GetMethodByAddress(methodPtr);
                if (method != null)
                {
                    Console.WriteLine($"{method.Type.Name}.{method.Name}");
                }
                else
                {
                    Console.WriteLine("invalid method pointer.");
                }
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
            }
        }


        private static ulong GetTaskStateFromAddress(ulong address)
        {
            var type = Runtime.GetHeap().GetObjectType(address);
            if ((type != null) && (type.Name.StartsWith("System.Threading.Task")))
            {
                // try to get the m_stateFlags field value
                ClrInstanceField field = type.GetFieldByName("m_stateFlags");
                if (field != null)
                {
                    var val = field.GetValue(address);
                    if (val != null)
                    {
                        try
                        {
                            return (ulong)(int)val;
                        }
                        catch (InvalidCastException)
                        {
                        }
                    }
                }
            }

            return 0;
        }

        private static string GetTaskState(ulong flag)
        {
            TaskStatus rval;

            if ((flag & TASK_STATE_FAULTED) != 0) rval = TaskStatus.Faulted;
            else if ((flag & TASK_STATE_CANCELED) != 0) rval = TaskStatus.Canceled;
            else if ((flag & TASK_STATE_RAN_TO_COMPLETION) != 0) rval = TaskStatus.RanToCompletion;
            else if ((flag & TASK_STATE_WAITING_ON_CHILDREN) != 0) rval = TaskStatus.WaitingForChildrenToComplete;
            else if ((flag & TASK_STATE_DELEGATE_INVOKED) != 0) rval = TaskStatus.Running;
            else if ((flag & TASK_STATE_STARTED) != 0) rval = TaskStatus.WaitingToRun;
            else if ((flag & TASK_STATE_WAITINGFORACTIVATION) != 0) rval = TaskStatus.WaitingForActivation;
            else if (flag == 0) rval = TaskStatus.Created;
            else return null;

            return rval.ToString();
        }

        // from CLR implementation
        internal const int TASK_STATE_STARTED                       =      65536;
        internal const int TASK_STATE_DELEGATE_INVOKED              =     131072;
        internal const int TASK_STATE_DISPOSED                      =     262144;
        internal const int TASK_STATE_EXCEPTIONOBSERVEDBYPARENT     =     524288;
        internal const int TASK_STATE_CANCELLATIONACKNOWLEDGED      =    1048576;
        internal const int TASK_STATE_FAULTED                       =    2097152;
        internal const int TASK_STATE_CANCELED                      =    4194304;
        internal const int TASK_STATE_WAITING_ON_CHILDREN           =    8388608;
        internal const int TASK_STATE_RAN_TO_COMPLETION             =   16777216;
        internal const int TASK_STATE_WAITINGFORACTIVATION          =   33554432;
        internal const int TASK_STATE_COMPLETION_RESERVED           =   67108864;
        internal const int TASK_STATE_THREAD_WAS_ABORTED            =  134217728;
        internal const int TASK_STATE_WAIT_COMPLETION_NOTIFICATION  =  268435456;
        internal const int TASK_STATE_EXECUTIONCONTEXT_IS_NULL      =  536870912;
        internal const int TASK_STATE_TASKSCHEDULED_WAS_FIRED       = 1073741824;
    }
}
