using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace dstrings;

[McpServerPromptType]
public static class DstringsPrompts
{
    [McpServerPrompt(Name = "analyze_string_memory")]
    [Description("Full analysis workflow: get generation stats overview then find the worst duplicated strings")]
    public static IEnumerable<ChatMessage> AnalyzeStringMemory(
        [Description("Process ID to analyze")] int? pid = null,
        [Description("Path to a memory dump file")] string? dumpPath = null)
    {
        var target = FormatTarget(pid, dumpPath);

        return new[]
        {
            new ChatMessage(ChatRole.User,
                $"Analyze string memory usage in {target}.\n\n" +
                "Follow these steps:\n" +
                $"1. Call the GetGenerationStats tool ({target}) to get per-generation heap statistics.\n" +
                "2. Review the DupSize% and HeapSize% columns to identify which generations have the most string duplication.\n" +
                $"3. Call the GetDuplicatedStrings tool ({target}) to get the list of duplicated strings. " +
                "If the generation stats show low duplication, use a lower countThreshold (e.g. 32) and sizeThresholdKB (e.g. 10) to surface smaller issues.\n" +
                "4. Summarize the findings: overall duplication ratio, which generations are most affected, and the top duplicated strings by wasted memory.")
        };
    }

    [McpServerPrompt(Name = "check_generation_stats")]
    [Description("Check per-generation heap statistics to assess string duplication across Gen0, Gen1, Gen2, LOH, POH, and Frozen heaps")]
    public static IEnumerable<ChatMessage> CheckGenerationStats(
        [Description("Process ID to analyze")] int? pid = null,
        [Description("Path to a memory dump file")] string? dumpPath = null)
    {
        var target = FormatTarget(pid, dumpPath);

        return new[]
        {
            new ChatMessage(ChatRole.User,
                $"Call the GetGenerationStats tool ({target}) and interpret the results.\n\n" +
                "Pay attention to:\n" +
                "- DupSize%: the percentage of string memory wasted on duplicates in each generation. Values above 50% indicate significant waste.\n" +
                "- HeapSize%: how much of the total heap is wasted on duplicated strings. Even a small percentage in Gen2 can represent a large absolute amount.\n" +
                "- High duplication in Gen2 suggests long-lived strings being created repeatedly (e.g. configuration values, interned keys).\n" +
                "- High duplication in Gen0/Gen1 suggests short-lived temporary strings being created in hot paths.\n" +
                "- Duplication in LOH means strings over 85KB are being duplicated, which also fragments the large object heap.")
        };
    }

    [McpServerPrompt(Name = "find_duplicate_strings")]
    [Description("Find and prioritize duplicated strings with suggested remediation strategies")]
    public static IEnumerable<ChatMessage> FindDuplicateStrings(
        [Description("Process ID to analyze")] int? pid = null,
        [Description("Path to a memory dump file")] string? dumpPath = null,
        [Description("Minimum occurrence count (default: 128)")] int countThreshold = 128,
        [Description("Minimum cumulated size in KB (default: 100)")] int sizeThresholdKB = 100)
    {
        var target = FormatTarget(pid, dumpPath);

        return new[]
        {
            new ChatMessage(ChatRole.User,
                $"Call the GetDuplicatedStrings tool ({target}) with countThreshold={countThreshold} and sizeThresholdKB={sizeThresholdKB}.\n\n" +
                "Then analyze the results:\n" +
                "- Focus on entries with the largest Size (shown in KB) as they represent the most memory waste.\n" +
                "- Strings that appear thousands of times with small individual size can still waste significant total memory.\n" +
                "- For each top offender, suggest a remediation strategy:\n" +
                "  * Repeated configuration/constant strings: use string.Intern() or a static readonly field.\n" +
                "  * Repeated enum.ToString() results: cache in a dictionary or use a lookup array.\n" +
                "  * Repeated path/URL strings: normalize and deduplicate at ingestion time.\n" +
                "  * Serialization artifacts (e.g. property names): configure the serializer to reuse strings.")
        };
    }

    private static string FormatTarget(int? pid, string? dumpPath)
    {
        if (pid.HasValue)
            return $"with pid={pid.Value}";
        if (!string.IsNullOrEmpty(dumpPath))
            return $"with dumpPath=\"{dumpPath}\"";
        return "providing the appropriate pid or dumpPath";
    }
}
