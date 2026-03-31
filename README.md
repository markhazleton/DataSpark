# DataSpark

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Build](https://img.shields.io/github/actions/workflow/status/markhazleton/DataSpark/ci.yml?branch=main)](https://github.com/markhazleton/DataSpark/actions)
[![Coverage](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/markhazleton/DataSpark/main/coverage-badge.json)](#testing)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A .NET 10 toolkit for working with SQLite databases: discover files, export tables to CSV, inspect schema, and generate C# DTOs. The solution includes a CLI, a web UI, a core library, tests, and benchmarks.

## Projects

- `DataSpark.Console` - CLI for discovery, export, schema reports, and code generation.
- `DataSpark.Core` - Core services and models shared by the CLI and web app.
- `DataSpark.Web` - ASP.NET Core MVC web UI for uploading SQLite files, analyzing tables, exporting CSVs, and generating DTOs.
- `DataSpark.Tests` - MSTest unit and integration tests.
- `DataSpark.Benchmarks` - BenchmarkDotNet performance benchmarks.

## Features

- Discover SQLite database files in a directory.
- Export all tables or a filtered list to CSV.
- Generate schema reports (text, JSON, Markdown).
- Generate C# DTOs from database schema.
- Web UI for upload, analysis, CSV export, and code generation.
- Persisted file management in the web UI (save/list/delete/describe).

## Quick Start

Prerequisites:

- .NET 10.0 SDK
- Node.js (only if you want to build `DataSpark.Web` frontend assets)

Build everything:

```bash
dotnet build DataSpark.sln
```

Run the CLI:

```bash
dotnet run --project DataSpark.Console -- --help
```

Run the web UI:

```bash
dotnet run --project DataSpark.Web
```

Build web assets (optional, for UI changes):

```bash
cd DataSpark.Web
npm install
npm run build
```

## CLI Usage

Commands:

- `discover` - list SQLite databases in a directory.
- `export` - export tables to CSV.
- `schema` - print schema reports.
- `generate` - generate C# DTOs.

Examples:

```bash
dotnet run --project DataSpark.Console discover --path "C:\Data\DBs"
dotnet run --project DataSpark.Console export --path "C:\Data\DBs" --output "C:\Exports"
dotnet run --project DataSpark.Console export --path "C:\Data\DBs" --tables Users,Orders
dotnet run --project DataSpark.Console schema --path "C:\Data\DBs" --format json
dotnet run --project DataSpark.Console generate --path "C:\Data\DBs" --output "C:\Gen" --namespace "MyApp.Models"
```

## Configuration

Both the CLI and web app use `appsettings.json`. See:

- `DataSpark.Console/appsettings.json`
- `DataSpark.Web/appsettings.json`

## Testing

```bash
dotnet test DataSpark.sln
```

## Repository Layout

```text
DataSpark/
  DataSpark.Console/    # CLI app
  DataSpark.Core/       # Core library
  DataSpark.Web/        # Web UI
  DataSpark.Tests/      # Tests
  DataSpark.Benchmarks/ # Benchmarks
  DataSpark.sln
```

## Contributing

Please see `CONTRIBUTING.md` for development workflow and coding standards.

## License

MIT. See `LICENSE`.
