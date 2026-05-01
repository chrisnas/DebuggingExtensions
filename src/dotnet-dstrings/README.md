# dotnet-dstrings

A .NET global CLI tool and MCP server to analyze duplicated strings in a .NET application (live process or memory dump) with per-generation heap statistics.

## Installation

```bash
dotnet tool install --global dotnet-dstrings
```

## CLI Usage

```bash
dotnet-dstrings [-p <pid> | <dumpfile>] [-c <count>] [-s <sizeKB>] [-l <length>] [--gen] [--dup]
```

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `-p <pid>` | Process ID to attach to | |
| `<dumpfile>` | Path to a memory dump file | |
| `-c <count>` | Minimum occurrence count to display | 128 |
| `-s <sizeKB>` | Minimum cumulated size in KB to display | 100 |
| `-l <length>` | Max string length to display | 64 |
| `--gen` | Show only generation statistics | |
| `--dup` | Show only duplicated strings | |
| `--mcp` | Start as stdio MCP server | |

Provide either `-p <pid>` or a dump file path, but not both.
By default (no `--gen`/`--dup` flag), both generation statistics and duplicated strings are displayed.

### Examples

```
dotnet-dstrings myapp.dmp -c 64 -s 50
dotnet-dstrings myapp.dmp --gen
dotnet-dstrings -p 1234 --dup -c 32
```

## MCP Server Usage

```bash
dotnet-dstrings --mcp
```

When started with `--mcp`, the tool runs as a stdio-based MCP server exposing two tools and three prompts:

### Tools

- `GetGenerationStats` -- per-generation heap statistics (string size, duplication ratios, object counts)
- `GetDuplicatedStrings` -- list of duplicated strings sorted by total size

### Prompts

- `analyze_string_memory` -- full analysis workflow: generation stats overview then duplicated strings
- `check_generation_stats` -- focused interpretation of per-generation heap statistics
- `find_duplicate_strings` -- find duplicates and suggest remediation strategies

## License

MIT
