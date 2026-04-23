using Microsoft.Diagnostics.Runtime;

namespace dstrings;

public record StringDuplicateInfo(string Value, long Count, ulong TotalSize);

public struct GenerationStats
{
    public ulong TotalObjectCount;
    public ulong TotalObjectSize;
    public long StringCount;
    public long DuplicatedStringCount;
    public ulong StringSize;
    public ulong DuplicatedStringSize;
}

public class StringAnalyzer
{
    public const int Gen0 = 0;
    public const int Gen1 = 1;
    public const int Gen2 = 2;
    public const int LOH = 3;
    public const int POH = 4;
    public const int Frozen = 5;
    public const int HeapCount = 6;

    public static readonly string[] HeapLabels = { "gen0", "gen1", "gen2", "LOH", "POH", "frozen" };

    private readonly GenerationStats[] _generationStats;
    private readonly List<StringDuplicateInfo> _duplicates;

    private StringAnalyzer(GenerationStats[] generationStats, List<StringDuplicateInfo> duplicates)
    {
        _generationStats = generationStats;
        _duplicates = duplicates;
    }

    public GenerationStats[] GenerationStats => _generationStats;
    public List<StringDuplicateInfo> Duplicates => _duplicates;

    public static StringAnalyzer Analyze(int? pid, string? dumpPath)
    {
        using DataTarget dataTarget = pid.HasValue
            ? DataTarget.AttachToProcess(pid.Value, suspend: true)
            : DataTarget.LoadDump(dumpPath!);

        if (dataTarget.ClrVersions.Length == 0)
            throw new InvalidOperationException("No CLR runtime found in the target.");

        using ClrRuntime runtime = dataTarget.ClrVersions[0].CreateRuntime();
        ClrHeap heap = runtime.Heap;

        if (!heap.CanWalkHeap)
            throw new InvalidOperationException("The heap is not in a walkable state (GC may have been in progress).");

        var genStats = new GenerationStats[HeapCount];
        var stringCounts = new Dictionary<string, (long Count, ulong TotalSize)>();

        foreach (ClrObject obj in heap.EnumerateObjects())
        {
            if (!obj.IsValid || obj.IsFree)
                continue;

            int gen = MapGeneration(heap, obj);
            genStats[gen].TotalObjectCount++;
            genStats[gen].TotalObjectSize += obj.Size;

            if (obj.Type is not { IsString: true })
                continue;

            string? value = obj.AsString();
            if (value is null)
                continue;

            ulong size = obj.Size;
            genStats[gen].StringCount++;
            genStats[gen].StringSize += size;

            if (stringCounts.TryGetValue(value, out var existing))
            {
                stringCounts[value] = (existing.Count + 1, existing.TotalSize + size);
                genStats[gen].DuplicatedStringCount++;
                genStats[gen].DuplicatedStringSize += size;
            }
            else
            {
                stringCounts[value] = (1, size);
            }
        }

        var duplicates = stringCounts
            .Where(kv => kv.Value.Count > 1)
            .OrderBy(kv => kv.Value.TotalSize)
            .Select(kv => new StringDuplicateInfo(kv.Key, kv.Value.Count, kv.Value.TotalSize))
            .ToList();

        return new StringAnalyzer(genStats, duplicates);
    }

    public static string Sanitize(string value, int maxLength = 64)
    {
        string display = value.Length > maxLength
            ? value[..maxLength] + "..."
            : value;
        return display.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
    }

    private static int MapGeneration(ClrHeap heap, ClrObject obj)
    {
        ClrSegment? seg = heap.GetSegmentByAddress(obj);
        if (seg is null)
            return Gen2;

        return seg.GetGeneration(obj) switch
        {
            Generation.Generation0 => Gen0,
            Generation.Generation1 => Gen1,
            Generation.Generation2 => Gen2,
            Generation.Large => LOH,
            Generation.Pinned => POH,
            Generation.Frozen => Frozen,
            _ => Gen2
        };
    }
}
