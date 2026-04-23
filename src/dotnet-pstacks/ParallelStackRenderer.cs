using System.Text;

namespace ParallelStacks;

public class ParallelStackRenderer
{
    private const int Padding = 5;

    private readonly int _threadIdLimit;

    public ParallelStackRenderer(int threadIdLimit = 4)
    {
        _threadIdLimit = threadIdLimit;
    }

    public void RenderToConsole(ParallelStack stacks)
    {
        Console.WriteLine();
        foreach (var root in stacks.Stacks)
        {
            Console.Write("________________________________________________");
            RenderStackConsole(root, 0);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
        }
    }

    public string RenderToString(ParallelStack stacks)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        foreach (var root in stacks.Stacks)
        {
            sb.Append("________________________________________________");
            RenderStackText(root, sb, 0);
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private void RenderStackConsole(ParallelStack stack, int increment)
    {
        var alignment = new string(' ', Padding * increment);

        if (stack.Stacks.Count == 0)
        {
            if (stack.Frame == null) return;

            Console.Write($"{Environment.NewLine}{alignment}");
            WriteConsoleColor($" ~~~~ {FormatThreadIdListHex(stack.ThreadIds)}", ConsoleColor.Yellow);
            WriteConsoleColor($"{Environment.NewLine}{alignment}{stack.ThreadIds.Count,Padding} ", ConsoleColor.Cyan);
            RenderFrameConsole(stack.Frame);
            return;
        }

        foreach (var child in stack.Stacks.OrderBy(s => s.ThreadIds.Count))
        {
            int nextIncrement = (child.ThreadIds.Count == stack.ThreadIds.Count) ? increment : increment + 1;
            RenderStackConsole(child, nextIncrement);
        }

        if (stack.Frame != null)
        {
            WriteConsoleColor($"{Environment.NewLine}{alignment}{stack.ThreadIds.Count,Padding} ", ConsoleColor.Cyan);
            RenderFrameConsole(stack.Frame);
        }
    }

    private void RenderFrameConsole(StackFrame frame)
    {
        if (!string.IsNullOrEmpty(frame.TypeName))
        {
            var namespaces = frame.TypeName.Split('.');
            for (int i = 0; i < namespaces.Length - 1; i++)
            {
                WriteConsoleColor(namespaces[i], ConsoleColor.DarkCyan);
                WriteConsoleColor(".", ConsoleColor.White);
            }
            WriteConsoleColor(namespaces[namespaces.Length - 1], ConsoleColor.DarkCyan);
            WriteConsoleColor(".", ConsoleColor.White);
        }

        WriteConsoleColor(frame.MethodName, ConsoleColor.Cyan);
        WriteConsoleColor("(", ConsoleColor.White);

        var parameters = frame.Signature;
        for (int current = 0; current < parameters.Count; current++)
        {
            var parameter = parameters[current];
            var pos = parameter.LastIndexOf(" ByRef");
            if (pos != -1)
            {
                WriteConsoleColor(parameter.Substring(0, pos), ConsoleColor.Gray);
                WriteConsoleColor(" ByRef", ConsoleColor.DarkGray);
            }
            else
            {
                WriteConsoleColor(parameter, ConsoleColor.Gray);
            }
            if (current < parameters.Count - 1)
                WriteConsoleColor(", ", ConsoleColor.White);
        }
        WriteConsoleColor(")", ConsoleColor.White);
    }

    private static void WriteConsoleColor(string text, ConsoleColor color)
    {
        var current = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = current;
    }

    private string FormatThreadIdListHex(List<uint> threadIds)
    {
        var count = threadIds.Count;
        var limit = Math.Min(count, _threadIdLimit);
        if (_threadIdLimit < 0)
            return string.Join(",", threadIds.Select(tid => tid.ToString("x")));

        var result = string.Join(",", threadIds.GetRange(0, limit).Select(tid => tid.ToString("x")));
        if (count > limit)
            result += "...";
        return result;
    }

    private void RenderStackText(ParallelStack stack, StringBuilder sb, int increment)
    {
        var alignment = new string(' ', Padding * increment);

        if (stack.Stacks.Count == 0)
        {
            if (stack.Frame == null) return;

            sb.Append($"{Environment.NewLine}{alignment}");
            sb.Append($" ~~~~ {FormatThreadIdListDecimal(stack.ThreadIds)}");
            sb.Append($"{Environment.NewLine}{alignment}{stack.ThreadIds.Count,Padding} ");
            RenderFrameText(stack.Frame, sb);
            return;
        }

        foreach (var child in stack.Stacks.OrderBy(s => s.ThreadIds.Count))
        {
            int nextIncrement = (child.ThreadIds.Count == stack.ThreadIds.Count) ? increment : increment + 1;
            RenderStackText(child, sb, nextIncrement);
        }

        if (stack.Frame != null)
        {
            sb.Append($"{Environment.NewLine}{alignment}{stack.ThreadIds.Count,Padding} ");
            RenderFrameText(stack.Frame, sb);
        }
    }

    private static void RenderFrameText(StackFrame frame, StringBuilder sb)
    {
        if (!string.IsNullOrEmpty(frame.TypeName))
        {
            sb.Append(frame.TypeName);
            sb.Append('.');
        }

        sb.Append(frame.MethodName);
        sb.Append('(');

        var parameters = frame.Signature;
        for (int current = 0; current < parameters.Count; current++)
        {
            sb.Append(parameters[current]);
            if (current < parameters.Count - 1)
                sb.Append(", ");
        }
        sb.Append(')');
    }

    private string FormatThreadIdListDecimal(List<uint> threadIds)
    {
        var count = threadIds.Count;
        var limit = Math.Min(count, _threadIdLimit);
        if (_threadIdLimit < 0)
            return string.Join(",", threadIds);

        var result = string.Join(",", threadIds.GetRange(0, limit));
        if (count > limit)
            result += "...";
        return result;
    }
}
