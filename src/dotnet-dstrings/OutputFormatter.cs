using System.Text;

namespace dstrings;

public static class OutputFormatter
{
    private const string RowFormat = "{0,8}  {1,12}  {2,9}  {3,10}  {4,9}  {5,10}  {6,9}";

    public static void FormatGenerationStats(StringBuilder sb, GenerationStats[] stats)
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

    public static void FormatDuplicatedStrings(
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
