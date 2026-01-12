# Sql2Csv

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Build](https://img.shields.io/github/actions/workflow/status/markhazleton/sql2csv/ci.yml?branch=main)](https://github.com/markhazleton/sql2csv/actions)
[![Coverage](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/markhazleton/sql2csv/main/coverage-badge.json)](#testing)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A .NET 10 toolkit for working with SQLite databases: discover files, export tables to CSV, inspect schema, and generate C# DTOs. The solution includes a CLI, a web UI, a core library, tests, and benchmarks.

## Projects

- `sql2csv.console` - CLI for discovery, export, schema reports, and code generation.
- `Sql2Csv.Core` - Core services and models shared by the CLI and web app.
- `sql2csv.web` - ASP.NET Core MVC web UI for uploading SQLite files, analyzing tables, exporting CSVs, and generating DTOs.
- `DataSpark.Web` - Experimental web app for CSV exploration, visualization, and ML experiments.
- `Sql2Csv.Tests` - MSTest unit and integration tests.
- `Sql2Csv.Benchmarks` - BenchmarkDotNet performance benchmarks.

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
- Node.js (only if you want to build `sql2csv.web` frontend assets)

Build everything:

```bash
dotnet build sql2csv.sln
```

Run the CLI:

```bash
dotnet run --project sql2csv.console -- --help
```

Run the web UI:

```bash
dotnet run --project sql2csv.web
```

Build web assets (optional, for UI changes):

```bash
cd sql2csv.web
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
dotnet run --project sql2csv.console discover --path "C:\Data\DBs"
dotnet run --project sql2csv.console export --path "C:\Data\DBs" --output "C:\Exports"
dotnet run --project sql2csv.console export --path "C:\Data\DBs" --tables Users,Orders
dotnet run --project sql2csv.console schema --path "C:\Data\DBs" --format json
dotnet run --project sql2csv.console generate --path "C:\Data\DBs" --output "C:\Gen" --namespace "MyApp.Models"
```

## Configuration

Both the CLI and web app use `appsettings.json`. See:

- `sql2csv.console/appsettings.json`
- `sql2csv.web/appsettings.json`

## Testing

```bash
dotnet test sql2csv.sln
```

## Repository Layout

```text
sql2csv/
  sql2csv.console/    # CLI app
  Sql2Csv.Core/       # Core library
  sql2csv.web/        # Web UI
  DataSpark.Web/      # Experimental web app
  Sql2Csv.Tests/      # Tests
  Sql2Csv.Benchmarks/ # Benchmarks
  sql2csv.sln
```

## Contributing

Please see `CONTRIBUTING.md` for development workflow and coding standards.

## License

MIT. See `LICENSE`.
