# dotnet-pstacks

A .NET global CLI tool and MCP server to display merged call stacks (parallel stacks) from a .NET application (live process or memory dump).

## Installation

```bash
dotnet tool install --global dotnet-pstacks
```

## CLI Usage

```bash
dotnet-pstacks [-p <pid> | <dumpfile>] [-t <threadIdLimit>]
```

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `-p <pid>` | Process ID to attach to | |
| `<dumpfile>` | Path to a memory dump file | |
| `-t <threadIdLimit>` | Max thread IDs to display per stack group (-1 for all) | 4 |
| `--mcp` | Start as stdio MCP server | |

Provide either `-p <pid>` or a dump file path, but not both.

### Example

```
dotnet-pstacks myapp.dmp -t 8
```

## MCP Server Usage

```bash
dotnet-pstacks --mcp
```

When started with `--mcp`, the tool runs as a stdio-based MCP server exposing a `GetParallelStacks` tool with the same parameters as the CLI.

## License

MIT
