using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace dstrings;

[McpServerToolType]
public static class DuplicatedStringsTool
{
    [McpServerTool, Description("Lists duplicated managed strings sorted by total size. " +
        "Use GetGenerationStats first to get an overview, then this tool to find specific duplicates. " +
        "Provide either a process ID or a dump file path (not both).")]
    public static string GetDuplicatedStrings(
        [Description("Process ID to attach to (mutually exclusive with dumpPath)")] int? pid = null,
        [Description("Path to a memory dump file (mutually exclusive with pid)")] string? dumpPath = null,
        [Description("Minimum occurrence count to display (default: 128)")] int countThreshold = 128,
        [Description("Minimum cumulated size in KB to display (default: 100)")] int sizeThresholdKB = 100,
        [Description("Max string length to display (default: 64)")] int stringLengthLimit = 64)
    {
        if (pid.HasValue == !string.IsNullOrEmpty(dumpPath))
            return "Error: provide either a process ID or a dump file path, but not both.";

        try
        {
            var analyzer = StringAnalyzer.Analyze(pid, dumpPath);
            ulong sizeThresholdBytes = (ulong)sizeThresholdKB * 1024;

            var sb = new StringBuilder();
            OutputFormatter.FormatDuplicatedStrings(sb, analyzer.Duplicates, countThreshold, sizeThresholdBytes, stringLengthLimit);
            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
