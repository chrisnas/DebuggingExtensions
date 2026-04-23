using System.ComponentModel;
using Microsoft.Diagnostics.Runtime;
using ModelContextProtocol.Server;

namespace ParallelStacks;

[McpServerToolType]
public static class ParallelStacksTool
{
    [McpServerTool, Description("Displays merged call stacks (parallel stacks) from a .NET process or memory dump. " +
        "Provide either a process ID or a dump file path (not both).")]
    public static string GetParallelStacks(
        [Description("Process ID to attach to (mutually exclusive with dumpPath)")] int? pid = null,
        [Description("Path to a memory dump file (mutually exclusive with pid)")] string? dumpPath = null,
        [Description("Max number of thread IDs to display per stack group (default: 4, use -1 for all)")] int threadIdLimit = 4)
    {
        if (pid.HasValue == !string.IsNullOrEmpty(dumpPath))
            return "Error: provide either a process ID or a dump file path, but not both.";

        try
        {
            using var target = string.IsNullOrEmpty(dumpPath)
                ? DataTarget.AttachToProcess(pid!.Value, suspend: false)
                : DataTarget.LoadDump(dumpPath);

            var clrVersion = target.ClrVersions.FirstOrDefault()
                ?? throw new InvalidOperationException("No CLR found in the target.");
            using var runtime = clrVersion.CreateRuntime();

            var stacks = ParallelStack.Build(runtime);

            var renderer = new ParallelStackRenderer(threadIdLimit);
            var output = renderer.RenderToString(stacks);

            var threadCount = stacks.ThreadIds.Count;
            var rootCount = stacks.Stacks.Count;

            return $"{output}==> {threadCount} threads with {rootCount} roots{Environment.NewLine}";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
