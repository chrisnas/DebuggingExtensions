using Microsoft.Diagnostics.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ParallelStacks;

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
                .WithToolsFromAssembly();
        })
        .Build();

    await host.RunAsync();
    return;
}

int? pid = null;
string? dumpPath = null;
int threadIdLimit = 4;

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

        case "-t" when i + 1 < args.Length:
            if (!int.TryParse(args[++i], out threadIdLimit))
            {
                Console.Error.WriteLine($"Error: '{args[i]}' is not a valid thread ID limit.");
                return;
            }
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
    renderer.RenderToConsole(stacks);

    Console.WriteLine($"==> {stacks.ThreadIds.Count} threads with {stacks.Stacks.Count} roots");
    Console.WriteLine();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}

static void ShowUsage()
{
    Console.Error.WriteLine("Usage: dotnet-pstacks [-p <pid> | <dumpfile>] [-t <threadIdLimit>]");
    Console.Error.WriteLine("       dotnet-pstacks --mcp");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Options:");
    Console.Error.WriteLine("  -p <pid>            Process ID to attach to");
    Console.Error.WriteLine("  <dumpfile>          Path to a memory dump file");
    Console.Error.WriteLine("  -t <threadIdLimit>  Max thread IDs per group (default: 4, -1 for all)");
    Console.Error.WriteLine("  --mcp               Start as stdio MCP server");
}
