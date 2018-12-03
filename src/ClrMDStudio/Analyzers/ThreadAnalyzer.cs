using System.Linq;
using System.Text;
using Microsoft.Diagnostics.Runtime;
using System;

namespace ClrMDStudio
{
    public class ThreadAnalyzer : IAnalyzer
    {
        public ThreadAnalyzer(IClrMDHost host)
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
            try
            {
                var clr = _host.Session.Clr;
                var threadPool = clr.ThreadPool;
                _host.WriteLine(string.Format(
                    "ThreadPool: {0} threads (#idle = {1} + #running = {2} | #dead = {3} | #max = {4})",
                    threadPool.TotalThreads.ToString(),
                    threadPool.IdleThreads.ToString(),
                    threadPool.RunningThreads.ToString(),
                    _host.Session.Clr.Threads.Count(t => t.IsThreadpoolWorker && !t.IsThreadpoolCompletionPort && !t.IsAlive && !t.IsThreadpoolGate && !t.IsThreadpoolTimer && !t.IsThreadpoolWait).ToString(),
                    threadPool.MaxThreads.ToString()
                    ));

                // Note: show threads with locks first, then alive then dead
                _host.WriteLine("  ID ThreadOBJ        Locks  Dead Abort Details");
                StringBuilder sb = new StringBuilder(8 * 1024 * 16);
                foreach (var thread in _host.Session.Clr.Threads.OrderBy(t => (t.LockCount > 0) ? -1 : (!t.IsAlive ? t.ManagedThreadId + 10000 : t.ManagedThreadId)))
                {
                    if (sb.Length >= 160)
                    {
                        _host.Write(sb.ToString());
                        sb.Clear();
                    }

                    sb.AppendFormat("{0,4} {1}  {2,4}    {3} {4,6} {5}\r\n",
                        thread.ManagedThreadId.ToString(),
                        thread.Address.ToString("X16"),
                        ((thread.LockCount > 0) ? thread.LockCount.ToString("####") : "    "),
                        (!thread.IsAlive ? "X" : " "),
                        thread.IsAborted ? "A" : (thread.IsAbortRequested ? "R" : " "),
                        (thread.IsThreadpoolCompletionPort ? "IO " : thread.IsThreadpoolWorker ? "worker " : ((thread.IsFinalizer) ? "Finalizer " : " ")) + GetExceptionIfAny(thread)
                    );
                }

                _host.Write(sb.ToString());
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


    #region internal helpers
    #endregion
        private string GetExceptionIfAny(ClrThread thread)
        {
            var exception = thread.CurrentException;
            if (exception != null)
            {
                return string.Format("Exception: '{0}'", exception.Message);
            }
            return "";
        }
        private string GetBlockingObjectsIfAny(ClrThread thread)
        {
            var blockingObjects = thread.BlockingObjects;
            if (blockingObjects == null)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            foreach (var blockinObject in thread.BlockingObjects)
            {
                sb.Append(blockinObject.Reason);
                if (blockinObject.HasSingleOwner)
                {
                    sb.AppendFormat("waiting for {0} on 0x{1}", blockinObject.Owner.ManagedThreadId, blockinObject.Object.ToString("X16"));
                }
                else
                {
                    sb.AppendFormat("waiting for ");
                    int last = blockinObject.Owners.Count - 1;
                    int current = 0;
                    foreach (var ownerThread in blockinObject.Owners)
                    {
                        if (current == last)
                            sb.AppendFormat("{0}", ownerThread.ManagedThreadId);
                        else
                            sb.AppendFormat("{0} | ", ownerThread.ManagedThreadId);
                    }
                    sb.AppendFormat(" on 0x{0}", blockinObject.Object.ToString("X16"));
                }
            }

            return sb.ToString();
        }
    }
}
