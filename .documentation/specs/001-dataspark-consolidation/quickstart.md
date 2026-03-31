# Quickstart: DataSpark Platform

**Branch**: `001-dataspark-consolidation` | **Date**: 2026-03-30

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- (Optional) Node.js 18+ for Tailwind CSS rebuild in DataSpark.Web

## Clone and Build

```bash
git clone https://github.com/markhazleton/DataSpark.git
cd DataSpark
dotnet build
```

## Project Structure

```
DataSpark.sln
├── DataSpark.Core/          # Shared business logic (services, models, interfaces)
├── DataSpark.Web/           # ASP.NET Core MVC web application
├── DataSpark.Console/       # CLI tool (System.CommandLine)
├── DataSpark.Tests/         # MSTest unit + integration tests
└── DataSpark.Benchmarks/    # BenchmarkDotNet performance tests
```

## Run the Web Application

```bash
cd DataSpark.Web
dotnet run --urls=http://localhost:5001
```

Open [http://localhost:5001](http://localhost:5001) in your browser.

### First Steps in the Web UI

1. **Explore sample data**: 8 pre-loaded CSV datasets are available on the home page
2. **Upload your own**: Drag-and-drop a CSV or SQLite `.db` file onto the upload area
3. **Analyze**: Click any dataset to see the full EDA report
4. **Chart**: Navigate to Charts to build interactive visualizations
5. **Pivot**: Navigate to Pivot Tables for drag-and-drop cross-tabulation
6. **Database tools**: Upload a `.db` file to browse schema, export tables, or generate C# DTOs

### Configuration

Key settings in `appsettings.json`:

```json
{
  "DataSpark": {
    "RootPath": "./data",
    "MaxUploadSizeMB": 50,
    "RetentionDays": 30,
    "StorageLimitMB": 1024
  },
  "ApiKey": {
    "HeaderName": "X-Api-Key",
    "Key": "<your-api-key>"
  },
  "OpenAI": {
    "ApiKey": "<your-openai-key>",
    "AssistantId": "<your-assistant-id>"
  }
}
```

## Run the CLI

```bash
cd DataSpark.Console
dotnet run -- discover --path ../DataSpark.Web/data
dotnet run -- schema --path ../DataSpark.Web/data/chinook.db --format markdown
dotnet run -- export --path ../DataSpark.Web/data/chinook.db --output ./output
dotnet run -- generate --path ../DataSpark.Web/data/chinook.db --namespace MyApp.Models
```

## Run Tests

```bash
# All tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Specific project
dotnet test DataSpark.Tests
```

## Run Benchmarks

```bash
cd DataSpark.Benchmarks
dotnet run -c Release
```

## Key Technologies

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 10 / C# 12 |
| Web framework | ASP.NET Core MVC |
| Database | Microsoft.Data.Sqlite |
| CSV | CsvHelper |
| Charts (server) | ScottPlot 5.x |
| Charts (client) | Chart.js, Plotly.js |
| Pivot tables | PivotTable.js 2.23.0 |
| Data analysis | Microsoft.Data.Analysis |
| Theming | WebSpark.Bootswatch |
| CLI | System.CommandLine 2.x |
| AI | OpenAI Assistants v2 |
| Logging | Serilog |
| Testing | MSTest + Moq + FluentAssertions |

## API Quick Test

```bash
# List files
curl -H "X-Api-Key: YOUR_KEY" http://localhost:5001/api/Files/list

# Upload a file
curl -H "X-Api-Key: YOUR_KEY" -F "file=@data.csv" http://localhost:5001/api/Files/upload

# Get chart types
curl -H "X-Api-Key: YOUR_KEY" http://localhost:5001/api/Chart/charttypes
```
