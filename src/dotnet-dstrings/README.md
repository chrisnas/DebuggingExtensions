# dotnet-dstrings

A .NET global CLI tool and MCP server to analyze duplicated strings in a .NET application (live process or memory dump) with per-generation heap statistics.

## Installation

```bash
dotnet tool install --global dotnet-dstrings
```

## CLI Usage

```bash
dotnet-dstrings [-p <pid> | <dumpfile>] [-c <count>] [-s <sizeKB>] [-l <length>]
```

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `-p <pid>` | Process ID to attach to | |
| `<dumpfile>` | Path to a memory dump file | |
| `-c <count>` | Minimum occurrence count to display | 128 |
| `-s <sizeKB>` | Minimum cumulated size in KB to display | 100 |
| `-l <length>` | Max string length to display | 64 |
| `--mcp` | Start as stdio MCP server | |

Provide either `-p <pid>` or a dump file path, but not both.

### Example

```
dotnet-dstrings myapp.dmp -c 64 -s 50
```

## MCP Server Usage

```bash
dotnet-dstrings --mcp
```

When started with `--mcp`, the tool runs as a stdio-based MCP server exposing a `GetDuplicatedStrings` tool with the same parameters as the CLI.

## License

MIT
