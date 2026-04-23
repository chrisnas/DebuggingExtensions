using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace dstrings;

[McpServerToolType]
public static class DuplicatedStringsTool
{
    [McpServerTool, Description("Lists duplicated managed strings sorted by total size with per-generation statistics. " +
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
            FormatGenerationStats(sb, analyzer.GenerationStats);
            FormatDuplicatedStrings(sb, analyzer.Duplicates, countThreshold, sizeThresholdBytes, stringLengthLimit);
            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private const string RowFormat = "{0,8}  {1,12}  {2,9}  {3,10}  {4,9}  {5,10}  {6,9}";

    private static void FormatGenerationStats(StringBuilder sb, GenerationStats[] stats)
    {
        sb.AppendLine(string.Format(RowFormat, "Heap", "StringSize", "DupSize%", "HeapSize%", "Count", "DupCount%", "HeapCount"));
        sb.AppendLine(new string('-', 79));

        for (int i = 0; i < StringAnalyzer.HeapCount; i++)
        {
            if (stats[i].TotalObjectCount == 0)
                continue;

            ulong sizeKB = stats[i].StringSize / 1024;
            string sizeOutput = sizeKB == 0 ? "-" : sizeKB + "k";

            decimal dupSizePct = stats[i].StringSize == 0
                ? 0
                : stats[i].DuplicatedStringSize * 100m / stats[i].StringSize;
            string dupSizeOutput = dupSizePct == 0 ? "-" : $"{dupSizePct:##.#}%";

            decimal heapSizePct = stats[i].TotalObjectSize == 0
                ? 0
                : stats[i].DuplicatedStringSize * 100m / stats[i].TotalObjectSize;
            string heapSizeOutput = heapSizePct == 0 ? "-" : $"{heapSizePct:##.#}%";

            long count = stats[i].StringCount;
            string countOutput = count == 0 ? "-" : count.ToString();

            decimal dupCountPct = stats[i].StringCount == 0
                ? 0
                : stats[i].DuplicatedStringCount * 100m / stats[i].StringCount;
            string dupCountOutput = dupCountPct == 0 ? "-" : $"{dupCountPct:##.#}%";

            sb.AppendLine(string.Format(RowFormat,
                StringAnalyzer.HeapLabels[i], sizeOutput, dupSizeOutput, heapSizeOutput,
                countOutput, dupCountOutput, stats[i].TotalObjectCount));
        }

        sb.AppendLine(new string('-', 79));
        sb.AppendLine();
    }

    private static void FormatDuplicatedStrings(
        StringBuilder sb,
        List<StringDuplicateInfo> duplicates,
        int countThreshold,
        ulong sizeThresholdBytes,
        int stringLengthLimit)
    {
        var filtered = duplicates
            .Where(e => e.Count >= countThreshold || e.TotalSize >= sizeThresholdBytes)
            .ToList();

        if (filtered.Count == 0)
        {
            sb.AppendLine("No duplicated strings matching the thresholds.");
            return;
        }

        sb.AppendLine($"{"Size",10} {"Count",8}  String");
        sb.AppendLine(new string('_', 80));

        foreach (var entry in filtered)
        {
            ulong sizeKB = entry.TotalSize / 1024;
            string display = StringAnalyzer.Sanitize(entry.Value, stringLengthLimit);
            sb.AppendLine($"{sizeKB,9}k {entry.Count,8}  {display}");
        }
    }
}
