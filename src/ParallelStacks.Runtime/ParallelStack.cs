using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime;

namespace ParallelStacks.Runtime
{
    public class ParallelStack
    {
        public static ParallelStack Build(ClrRuntime runtime)
        {
            var ps = new ParallelStack();
            var stackFrames = new List<ClrStackFrame>(64);
            foreach (var thread in runtime.Threads)
            {
                stackFrames.Clear();

                foreach (var stackFrame in thread.StackTrace.Reverse())
                {
                    if (stackFrame.Kind != ClrStackFrameType.ManagedMethod)
                        continue;

                    stackFrames.Add(stackFrame);
                }

                if (stackFrames.Count == 0)
                    continue;

                ps.AddStack(thread.OSThreadId, stackFrames.ToArray());
            }

            return ps;
        }

        public static ParallelStack Build(string dumpFile, string dacFilePath)
        {
            DataTarget dataTarget = null;
            ParallelStack ps = null;
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    dataTarget = DataTarget.LoadCrashDump(dumpFile);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    dataTarget = DataTarget.LoadCoreDump(dumpFile);
                }
                else
                {
                    throw new InvalidOperationException("Unsupported platform...");
                }

                var runtime = CreateRuntime(dataTarget, dacFilePath);
                if (runtime == null)
                {
                    return null;
                }

                ps = ParallelStack.Build(runtime);
            }
            finally
            {
                dataTarget?.Dispose();
            }

            return ps;
        }

        public static ParallelStack Build(int pid, string dacFilePath)
        {
            DataTarget dataTarget = null;
            ParallelStack ps = null;
            try
            {
                const uint msecTimeout = 2000;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    dataTarget = DataTarget.AttachToProcess(pid, msecTimeout, AttachFlag.NonInvasive);
                }
                else
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // ClrMD implementation for Linux is available only for Passive
                    dataTarget = DataTarget.AttachToProcess(pid, msecTimeout, AttachFlag.Passive);
                }
                else
                {
                    throw new InvalidOperationException("Unsupported platform...");
                }

                var runtime = CreateRuntime(dataTarget, dacFilePath);
                if (runtime == null)
                {
                    return null;
                }

                ps = ParallelStack.Build(runtime);
            }
            finally
            {
                dataTarget?.Dispose();
            }

            return ps;
        }

        private static ClrRuntime CreateRuntime(DataTarget dataTarget, string dacFilePath)
        {
            // check bitness first
            bool isTarget64Bit = (dataTarget.PointerSize == 8);
            if (Environment.Is64BitProcess != isTarget64Bit)
            {
                throw new InvalidOperationException(
                    $"Architecture mismatch:  This tool is {(Environment.Is64BitProcess ? "64 bit" : "32 bit")} but target is {(isTarget64Bit ? "64 bit" : "32 bit")}");
            }

            var version = dataTarget.ClrVersions[0];
            var runtime = (dacFilePath != null) ? version.CreateRuntime(dacFilePath) : version.CreateRuntime();
            return runtime;
        }
        
        private ParallelStack(ClrStackFrame frame = null)
        {
            Stacks = new List<ParallelStack>();
            ThreadIds = new List<uint>();
            Frame = (frame == null) ? null : new StackFrame(frame);
        }

        public List<ParallelStack> Stacks { get; }

        public StackFrame Frame { get; }

        public List<uint> ThreadIds { get; set; }

        private void AddStack(uint threadId, ClrStackFrame[] frames, int index = 0)
        {
            ThreadIds.Add(threadId);
            var firstFrame = frames[index].DisplayString;
            var callstack = Stacks.FirstOrDefault(s => s.Frame.Text == firstFrame);
            if (callstack == null)
            {
                callstack = new ParallelStack(frames[index]);
                Stacks.Add(callstack);
            }

            if (index == frames.Length - 1)
            {
                callstack.ThreadIds.Add(threadId);
                return;
            }

            callstack.AddStack(threadId, frames, index + 1);
        }
    }
}
