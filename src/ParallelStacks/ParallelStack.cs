using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Runtime;

namespace ParallelStacks
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

                ps.AddStack(thread.ManagedThreadId, stackFrames.ToArray());
            }

            return ps;
        }

        private bool _useDml;

        private ParallelStack(ClrStackFrame frame = null)
        {
            Stacks = new List<ParallelStack>();
            ThreadIds = new List<int>();
            Frame = (frame == null) ? null : new StackFrame(frame);
        }

        public List<ParallelStack> Stacks { get; }

        public StackFrame Frame { get; }

        public List<int> ThreadIds { get; set; }

        public void WriteToConsole(bool useDml = false)
        {
            _useDml = useDml;
            WriteStack(this);
        }

        private const int padding = 5;
        private void WriteStack(ParallelStack stack, int increment = 0)
        {
            var alignment = new string(' ', padding * increment);
            if (stack.Stacks.Count == 0)
            {
                var lastFrame = stack.Frame;
                Console.Write($"{Environment.NewLine}{alignment}");
                WriteFrameSeparator(" ~~~~");
                //Console.Write($"{Environment.NewLine}{alignment}{stack.ThreadIds.Count,padding} ");
                WriteCount($"{Environment.NewLine}{alignment}{stack.ThreadIds.Count,padding} ");
                WriteFrame(lastFrame);
                return;
            }

            foreach (var nextStackFrame in stack.Stacks.OrderBy(s => s.ThreadIds.Count))
            {
                WriteStack(nextStackFrame,
                    (nextStackFrame.ThreadIds.Count == stack.ThreadIds.Count) ? increment : increment + 1);
            }

            var currentFrame = stack.Frame;
            //Console.Write($"{Environment.NewLine}{alignment}{stack.ThreadIds.Count,padding} ");
            WriteCount($"{Environment.NewLine}{alignment}{stack.ThreadIds.Count,padding} ");
            WriteFrame(currentFrame);
        }

        private void WriteFrame(StackFrame frame)
        {
            if (!string.IsNullOrEmpty(frame.TypeName))
            {
                var namespaces = frame.TypeName.Split('.');
                for (int i = 0; i < namespaces.Length - 1; i++)
                {
                    WriteNamespace(namespaces[i]);
                    WriteSeparator(".");
                }
                WriteMethodType(namespaces[namespaces.Length - 1]);
                WriteSeparator(".");
            }

            WriteMethod(frame.MethodName);
            WriteSeparator("(");

            var parameters = frame.Signature;
            for (int current = 0; current < parameters.Count; current++)
            {
                var parameter = parameters[current];
                // handle byref case
                var pos = parameter.LastIndexOf(" ByRef");
                if (pos != -1)
                {
                    WriteType(parameter.Substring(0, pos));
                    WriteDark(" ByRef");
                }
                else
                {
                    WriteType(parameter);
                }
                if (current < parameters.Count - 1) WriteSeparator(", ");
            }
            WriteSeparator(")");
        }

        private void WriteCount(string count)
        {
            if (_useDml)
            {
                Console.Write($"<col fg=\"srcpair\" bg=\"wbg\">{count}</col>");
                return;
            }

            WriteWithColor(count, ConsoleColor.Cyan);
        }
        private void WriteNamespace(string ns)
        {
            WriteWithColor(ns, ConsoleColor.DarkCyan);
        }
        private void WriteType(string type)
        {
            WriteWithColor(type, ConsoleColor.Gray);
        }
        private void WriteSeparator(string separator)
        {
            WriteWithColor(separator, ConsoleColor.White);
        }
        private void WriteDark(string separator)
        {
            WriteWithColor(separator, ConsoleColor.DarkGray);
        }
        private void WriteMethod(string method)
        {
            if (_useDml)
            {
                Console.Write($"<col fg=\"srckw\" bg=\"wbg\">{method}</col>");
                return;
            }

            WriteWithColor(method, ConsoleColor.Cyan);
        }
        private void WriteMethodType(string type)
        {
            if (_useDml)
            {
                Console.Write($"<b><col fg=\"srckw\" bg=\"wbg\">{type}</col></b>");
                return;
            }

            WriteWithColor(type, ConsoleColor.DarkCyan);
        }
        private void WriteFrameSeparator(string text)
        {
            if (_useDml)
            {
                Console.Write($"<b><col fg=\"srcpair\" bg=\"wbg\">{text}</col></b>");
                return;
            }

            WriteWithColor(text, ConsoleColor.Yellow);
        }

        private void WriteWithColor(string text, ConsoleColor color)
        {
            var current = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = current;
        }

        private void AddStack(int threadId, ClrStackFrame[] frames, int index = 0)
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
