using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace dstrings;

[McpServerToolType]
public static class GenerationStatsTool
{
    [McpServerTool, Description("Shows per-generation heap statistics including string size, duplication ratios, and object counts " +
        "for Gen0, Gen1, Gen2, LOH, POH, and Frozen heaps. " +
        "Provide either a process ID or a dump file path (not both).")]
    public static string GetGenerationStats(
        [Description("Process ID to attach to (mutually exclusive with dumpPath)")] int? pid = null,
        [Description("Path to a memory dump file (mutually exclusive with pid)")] string? dumpPath = null)
    {
        if (pid.HasValue == !string.IsNullOrEmpty(dumpPath))
            return "Error: provide either a process ID or a dump file path, but not both.";

        try
        {
            var analyzer = StringAnalyzer.Analyze(pid, dumpPath);
            var sb = new StringBuilder();
            OutputFormatter.FormatGenerationStats(sb, analyzer.GenerationStats);
            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
