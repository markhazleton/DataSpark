# DataSpark.Core

Core library that discovers SQLite `.db` files, explores schema, exports tables to CSV, analyzes CSV (and other delimited) files, performs unified (DB/CSV) analysis, and generates C# DTO classes. Designed for reuse across console / service / UI hosts.

## Features

### Discovery

* Locate SQLite database files (`*.db`) in a directory (`IDatabaseDiscoveryService`).
* Locate mixed data files (SQLite + CSV + probable delimited: .csv, .tsv, .tab, .txt) with lightweight delimiter heuristic (`IDataFileDiscoveryService`).

### Export

* Export every table of a database to individual CSV files (`ExportService`).
* Optional overrides at runtime: custom delimiter, include / suppress headers.
* Filter export to a subset of tables.
* Per-table timing + row counts surfaced in `ExportResult` and summarized by `ApplicationService`.

### Schema

* Enumerate table names and detailed column metadata (`ISchemaService`).
* Row counts per table (SQLite `COUNT(*)`).
* Multi‑format schema report generation: `text`, `json`, `markdown`.

### CSV & Delimited Analysis

* Auto-detect delimiter (`,` `;` `\t` `|`) and basic header detection.
* Column type inference (INTEGER / REAL / DATETIME / BOOLEAN / TEXT heuristic).
* Per column: null vs non-null counts, sample values (deduplicated, capped), min/max/mean/std-dev for numeric columns.
* Row counting (capped at 100,000 for profiling pass) with safety limits.
* Paginated data retrieval with `skip` / `take` for CSV files.

### Unified Analysis

* Single abstraction over database table vs CSV file (`IUnifiedAnalysisService`).
* Returns unified models for column statistics & data page retrieval.
* Database data paging & statistics beyond schema (advanced counts, sampling) are marked TODO (stubbed currently).

### Code Generation

* Generates DTO classes (one per table) with PascalCase properties from SQLite schema (`ICodeGenerationService`).
* Nullable handling: value types become nullable when column allows nulls; reference types annotated accordingly.

### Configuration (`DataSparkOptions`)

```yaml
DataSpark:
 RootPath: "C:/data"
 Paths:
  Config: "config"
  Data: "data"
  Scripts: "scripts"
 Database:
  DefaultName: "main"
  Timeout: 600          # seconds for commands
 Export:
  IncludeHeaders: true
  Delimiter: ","
  Encoding: "UTF-8"
Bind with `services.Configure<DataSparkOptions>(configuration.GetSection(DataSparkOptions.SectionName));`.

## Project Structure

* **Configuration** – Options objects (`DataSparkOptions`, `PathOptions`, `DatabaseOptions`, `ExportOptions`).
* **Interfaces** – Service contracts for discovery, export, schema, code gen, CSV analysis, unified analysis.
* **Models** – Domain + DTO models (schema, export results, unified analysis/data, CSV profiling).
* **Services** – Implementations (application orchestration + all feature services).

## Service Registration (DI)

```csharp
services.Configure<DataSparkOptions>(config.GetSection(DataSparkOptions.SectionName));

services.AddScoped<IDatabaseDiscoveryService, DatabaseDiscoveryService>();
services.AddScoped<IDataFileDiscoveryService, DataFileDiscoveryService>();
services.AddScoped<ICsvAnalysisService, CsvAnalysisService>();
services.AddScoped<IUnifiedAnalysisService, UnifiedAnalysisService>();
services.AddScoped<IExportService, ExportService>();
services.AddScoped<ISchemaService, SchemaService>();
services.AddScoped<ICodeGenerationService, CodeGenerationService>();
services.AddScoped<ApplicationService>();
```

## Typical Usage Patterns

### 1. Export All Tables

```csharp
await appService.ExportDatabasesAsync(dbDirectory, outputDirectory);
```

With overrides & table filter:

```csharp
await appService.ExportDatabasesAsync(dbDirectory, outputDirectory, new[]{"users","orders"}, ";", includeHeaders: false);
```

### 2. Schema Report

```csharp
await appService.GenerateSchemaReportsAsync(dbDirectory, format: "markdown");
```

### 3. Generate DTO Classes

```csharp
await appService.GenerateCodeAsync(dbDirectory, codeOutDir, "MyApp.Data");
```

### 4. CSV Analysis & Paging

```csharp
var analysis = await csvAnalysisService.AnalyzeCsvAsync(csvPath);
var page = await csvAnalysisService.GetCsvDataAsync(csvPath, skip: 0, take: 100);
```

### 5. Unified Analysis (DB or CSV)

```csharp
var unified = await unifiedAnalysisService.AnalyzeDataSourceAsync(dataSourceConfig);
var data = await unifiedAnalysisService.GetDataAsync(dataSourceConfig, skip: 0, take: 50);
```

## Models Highlight

* `ExportResult` – success flag, row count, duration, error.
* `TableInfo` / `ColumnInfo` – schema & metadata (PK, nullability, defaults, row counts).
* `CsvAnalysisResult` / `CsvColumnAnalysis` – profiling output.
* `UnifiedAnalysisResult` / `UnifiedDataResult` – abstraction over DB table vs CSV.

## Current Limitations / TODOs

* Unified database row statistics & data paging not yet implemented (placeholders set to 0).
* Table filtering for code generation currently ignored (generates all tables).
* CSV delimiter inference is heuristic; complex quoted edge cases may require manual override upstream.

## Dependencies

* Microsoft.Data.Sqlite
* CsvHelper
* Microsoft.Extensions.* (Configuration, DependencyInjection, Logging, Options)

## Logging & Observability

All services use `ILogger<T>`; verbose diagnostics (Debug) include per-table export and generation details. Summaries logged by `ApplicationService` after batch operations.

## Error Handling

* Export returns per-table `ExportResult` with `IsSuccess` & `ErrorMessage`.
* Analysis accumulates non-fatal issues in `Errors` collections.

## License / Contribution

Add your chosen license & contribution guidelines in the repository root.

---
This document reflects the current implemented surface area; update as TODOs are completed.
