using System.Text;
using dstrings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

if (args.Contains("--mcp"))
{
    var mcpArgs = args.Where(a => a != "--mcp").ToArray();
    var host = Host.CreateDefaultBuilder(mcpArgs)
        .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning))
        .ConfigureServices(services =>
        {
            services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly()
                .WithPromptsFromAssembly();
        })
        .Build();

    await host.RunAsync();
    return;
}

int? pid = null;
string? dumpPath = null;
int countThreshold = 128;
int sizeThresholdKB = 100;
int stringLengthLimit = 64;
bool showGen = false;
bool showDup = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-p" when i + 1 < args.Length:
            if (!int.TryParse(args[++i], out int parsedPid))
            {
                Console.Error.WriteLine($"Error: '{args[i]}' is not a valid process ID.");
                return;
            }
            pid = parsedPid;
            break;

        case "-c" when i + 1 < args.Length:
            if (!int.TryParse(args[++i], out countThreshold) || countThreshold < 1)
            {
                Console.Error.WriteLine($"Error: '{args[i]}' is not a valid count threshold.");
                return;
            }
            break;

        case "-s" when i + 1 < args.Length:
            if (!int.TryParse(args[++i], out sizeThresholdKB) || sizeThresholdKB < 0)
            {
                Console.Error.WriteLine($"Error: '{args[i]}' is not a valid size threshold (KB).");
                return;
            }
            break;

        case "-l" when i + 1 < args.Length:
            if (!int.TryParse(args[++i], out stringLengthLimit) || stringLengthLimit < 1)
            {
                Console.Error.WriteLine($"Error: '{args[i]}' is not a valid string length limit.");
                return;
            }
            break;

        case "--gen":
            showGen = true;
            break;

        case "--dup":
            showDup = true;
            break;

        default:
            if (args[i].StartsWith('-'))
            {
                ShowUsage();
                return;
            }
            dumpPath = args[i];
            break;
    }
}

if (pid.HasValue == !string.IsNullOrEmpty(dumpPath))
{
    Console.Error.WriteLine("Error: provide either -p <pid> or a dump file path, but not both.");
    ShowUsage();
    return;
}

if (!showGen && !showDup)
{
    showGen = true;
    showDup = true;
}

try
{
    var analyzer = StringAnalyzer.Analyze(pid, dumpPath);
    ulong sizeThresholdBytes = (ulong)sizeThresholdKB * 1024;

    var sb = new StringBuilder();
    if (showGen)
        OutputFormatter.FormatGenerationStats(sb, analyzer.GenerationStats);
    if (showDup)
        OutputFormatter.FormatDuplicatedStrings(sb, analyzer.Duplicates, countThreshold, sizeThresholdBytes, stringLengthLimit);

    Console.Write(sb.ToString());
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}

static void ShowUsage()
{
    Console.Error.WriteLine("Usage: dotnet-dstrings [-p <pid> | <dumpfile>] [-c <count>] [-s <sizeKB>] [-l <length>] [--gen] [--dup]");
    Console.Error.WriteLine("       dotnet-dstrings --mcp");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Options:");
    Console.Error.WriteLine("  -p <pid>      Process ID to attach to");
    Console.Error.WriteLine("  <dumpfile>    Path to a memory dump file");
    Console.Error.WriteLine("  -c <count>    Minimum occurrence count to display (default: 128)");
    Console.Error.WriteLine("  -s <sizeKB>   Minimum cumulated size in KB to display (default: 100)");
    Console.Error.WriteLine("  -l <length>   Max string length to display (default: 64)");
    Console.Error.WriteLine("  --gen         Show only generation statistics");
    Console.Error.WriteLine("  --dup         Show only duplicated strings");
    Console.Error.WriteLine("  --mcp         Start as stdio MCP server");
}
