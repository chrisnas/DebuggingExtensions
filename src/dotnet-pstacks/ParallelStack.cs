using Microsoft.Diagnostics.Runtime;

namespace ParallelStacks;

public class ParallelStack
{
    public List<ParallelStack> Stacks { get; }
    public StackFrame? Frame { get; }
    public List<uint> ThreadIds { get; set; }

    private ParallelStack(ClrStackFrame? frame = null)
    {
        Stacks = new List<ParallelStack>();
        ThreadIds = new List<uint>();
        Frame = (frame == null) ? null : new StackFrame(frame);
    }

    public static ParallelStack Build(ClrRuntime runtime)
    {
        var ps = new ParallelStack();
        var stackFrames = new List<ClrStackFrame>(64);

        foreach (var thread in runtime.Threads)
        {
            stackFrames.Clear();

            foreach (var stackFrame in thread.EnumerateStackTrace().Reverse())
            {
                if (stackFrame.Kind != ClrStackFrameKind.ManagedMethod || stackFrame.Method == null)
                    continue;

                stackFrames.Add(stackFrame);
            }

            if (stackFrames.Count == 0)
                continue;

            ps.AddStack(thread.OSThreadId, stackFrames.ToArray());
        }

        return ps;
    }

    private void AddStack(uint threadId, ClrStackFrame[] frames, int index = 0)
    {
        ThreadIds.Add(threadId);

        var firstFrame = frames[index].Method?.Signature;
        var callstack = Stacks.FirstOrDefault(s => s.Frame?.Text == firstFrame);
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
