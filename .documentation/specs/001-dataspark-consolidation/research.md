# Research: DataSpark Platform Consolidation

**Branch**: `001-dataspark-consolidation` | **Date**: 2026-03-30

## Research Summary

All NEEDS CLARIFICATION items from Technical Context have been resolved through codebase analysis and prior clarification sessions. This document records the research findings and decisions.

---

## R1: Namespace Rename Strategy

**Decision**: Global find-and-replace of `Sql2Csv` → `DataSpark` across all files, followed by project/folder renames.

**Rationale**: The namespace `Sql2Csv` appears in 100+ C# files across all projects. A systematic rename is the cleanest approach since:
- All .csproj files use either explicit `RootNamespace` declarations or default to folder-based namespaces
- All `using` statements follow the `Sql2Csv.Core.*`, `Sql2Csv.Web.*`, `Sql2Csv.Tests.*` pattern
- The solution file references project paths that will change with folder renames

**Alternatives considered**:
- Namespace aliases (rejected: leaves legacy names in codebase, confusing)
- Gradual rename with forwarding types (rejected: unnecessary complexity for a single-repo rename)

**Rename mapping**:
| Old | New |
|-----|-----|
| `Sql2Csv.Core` | `DataSpark.Core` |
| `Sql2Csv.Core.Services` | `DataSpark.Core.Services` |
| `Sql2Csv.Core.Interfaces` | `DataSpark.Core.Interfaces` |
| `Sql2Csv.Core.Models` | `DataSpark.Core.Models` |
| `Sql2Csv.Core.Configuration` | `DataSpark.Core.Configuration` |
| `Sql2Csv.Web` (sql2csv.web) | REMOVED (absorbed into DataSpark.Web) |
| `Sql2Csv.Web.Models` | DataSpark.Web absorbs relevant models |
| `Sql2Csv.Web.Services` | DataSpark.Web absorbs relevant services |
| `Sql2Csv.Presentation.Commands` | `DataSpark.Console.Commands` |
| `Sql2Csv.Tests` | `DataSpark.Tests` |
| `Sql2Csv.Benchmarks` | `DataSpark.Benchmarks` |
| `DataSpark.Web` (already named) | `DataSpark.Web` (no change needed) |
| `Sql2CsvOptions` | `DataSparkOptions` |

**Folder renames**:
| Old folder | New folder |
|------------|-----------|
| `Sql2Csv.Core/` | `DataSpark.Core/` |
| `sql2csv.console/` | `DataSpark.Console/` |
| `sql2csv.web/` | REMOVED |
| `Sql2Csv.Tests/` | `DataSpark.Tests/` |
| `Sql2Csv.Benchmarks/` | `DataSpark.Benchmarks/` |
| `DataSpark.Web/` | `DataSpark.Web/` (unchanged) |
| `sql2csv.sln` | `DataSpark.sln` |
| `Sql2Csv.snk` | `DataSpark.snk` |

**File rename steps** (ordered):
1. Rename solution file: `sql2csv.sln` → `DataSpark.sln`
2. Rename strong-name key: `Sql2Csv.snk` → `DataSpark.snk`
3. Rename project folders (Core, Console, Tests, Benchmarks)
4. Rename .csproj files inside each folder
5. Update solution file project references
6. Global text replace `Sql2Csv` → `DataSpark` in all .cs, .csproj, .cshtml, .json files
7. Replace `sql2csv` (lowercase) → `DataSpark` in remaining references (copilot-instructions, README, etc.)
8. Update `RootNamespace` and `AssemblyName` in all .csproj files
9. Remove sql2csv.web project folder and solution references

---

## R2: SQL Injection Remediation

**Decision**: Fix all SQL string concatenation issues identified in the constitution compliance check.

**Findings**: The codebase has SQL injection risks in two services:

### Critical (user-controlled input):
- `DatabaseAnalysisService.cs` line ~729: Search value used with `LIKE` and only basic quote escaping (`Replace("'", "''")`). This MUST use parameterized queries.

### Medium (identifier interpolation — constitution requires bracket-escaping):
All table/column name interpolation already uses bracket-escaping `[{tableName}]` which is the constitution-prescribed pattern. These are **compliant** but should be audited:
- `UnifiedAnalysisService.cs`: 4 instances of `$"SELECT ... FROM [{tableName}]"` — COMPLIANT
- `DatabaseAnalysisService.cs`: 6 instances — COMPLIANT except line 729
- `ExportService.cs`: 1 instance — COMPLIANT

**Fix for line 729**: Replace string interpolation in the LIKE clause with a parameterized query:
```csharp
// BEFORE (vulnerable):
var conditions = columns.Select(col => $"[{col}] LIKE '%{searchValue.Replace("'", "''")}%'");

// AFTER (safe):
var conditions = columns.Select(col => $"[{col}] LIKE @searchPattern");
// Add parameter: command.Parameters.AddWithValue("@searchPattern", $"%{searchValue}%");
```

---

## R3: API Key Authentication Middleware

**Decision**: Implement a simple middleware-based API key check for all `/api/*` routes.

**Rationale**: FR-050 requires API key auth. ASP.NET Core middleware is the cleanest pattern for this — it inspects a configurable header (e.g., `X-Api-Key`) and returns 401 if missing/invalid. This keeps API controllers clean and avoids per-controller attributes.

**Pattern**:
```
Configuration: appsettings.json → DataSpark:ApiKey
Middleware: ApiKeyAuthMiddleware checks X-Api-Key header
Pipeline: app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/api"), ...)
```

**Alternatives considered**:
- `[Authorize]` attribute with custom policy (rejected: requires auth scheme setup, heavier)
- JWT tokens (rejected: over-engineering for single-user/local deployment per FR-050)

---

## R4: Sample Datasets

**Decision**: Bundle 8 diverse CSV files in `wwwroot/sample-data/` as read-only static files.

**Source datasets** (from DataAnalysisDemo + public domain):
1. `adult.csv` — US Census income data (32,561 rows) — classification, demographics
2. `legislators.csv` — US Congress members (541 rows) — political analysis
3. `mps.csv` — Canadian Parliament members (338 rows) — demographics
4. `TitanicManifest.csv` — Titanic passenger data (891 rows) — survival analysis
5. `heroes_information.csv` — Superhero characteristics (734 rows) — categorical analysis
6. `SouthlakeCodeEnforcement.csv` — Municipal code violations (1,200+ rows) — geographic
7. `Beverages.csv` — Beverage industry data (255 rows) — market analysis
8. `DesktopOS.csv` — OS market share (50 rows) — time series

**Implementation**: Files served from `wwwroot/sample-data/` and listed via a `SampleDataService` that reads the directory. The file management UI distinguishes sample files (read-only badge, no delete button) from user uploads.

---

## R5: SearchPanes Integration

**Decision**: Add DataTables SearchPanes as a client-side enhancement to DataSpark.Web data grids.

**Rationale**: DataAnalysisDemo used DataTables 2.3.3 with SearchPanes for multi-column filtering. DataSpark.Web currently has DataTables for basic grids but not SearchPanes.

**Implementation approach**:
- Add DataTables SearchPanes CDN references to views that display data grids
- Configure SearchPanes in DataTables initialization JavaScript
- No backend changes needed — SearchPanes works on client-side data or can leverage existing server-side endpoints

**Note**: sql2csv.web uses a custom DataTables-like implementation with TailwindCSS. Since DataSpark.Web uses Bootstrap 5, it can use the standard DataTables library directly.

---

## R6: Pivot Table localStorage Persistence

**Decision**: Add client-side localStorage save/restore for pivot table configurations.

**Rationale**: DataAnalysisDemo's pivot implementation included localStorage-based state persistence. DataSpark.Web has server-side config save via PivotTableController but lacks client-side persistence.

**Implementation**: Add JavaScript to the pivot table initialization that:
1. On pivot configuration change → save state to `localStorage` with a key based on the data source
2. On page load → check `localStorage` for saved config and restore if present
3. Add "Clear Saved State" button in the pivot UI

**Current PivotTable.js version**: 2.23.0 (CDN). The pivot renderers already include C3.js, Plotly.js, and export renderers.

---

## R7: TreatWarningsAsErrors Gap

**Decision**: Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to Sql2Csv.Tests (the only project missing it) during the rename.

**Current status**:
| Project | Has TreatWarningsAsErrors |
|---------|--------------------------|
| Sql2Csv.Core | ✅ Yes |
| Sql2Csv.Benchmarks | ✅ Yes |
| sql2csv.console | ✅ Yes |
| sql2csv.web | ✅ Yes |
| DataSpark.Web | ✅ Yes |
| **Sql2Csv.Tests** | **❌ No** |

The Tests project is the only gap. This will be added when renaming to `DataSpark.Tests.csproj`.

---

## R8: sql2csv.web Feature Absorption

**Decision**: Port sql2csv.web's unique features into DataSpark.Web via a new `DatabaseController`.

**Features to port** (from sql2csv.web/Controllers/HomeController.cs):
| Feature | sql2csv.web action | DataSpark.Web target |
|---------|-------------------|---------------------|
| SQLite upload | `Upload()` | DatabaseController.Upload() |
| Schema analysis | `Analyze()` | DatabaseController.Analyze() |
| Table analysis | `AnalyzeTable()` | DatabaseController.AnalyzeTable() |
| CSV export | `ExportTables()` | DatabaseController.ExportTables() |
| DTO generation | `GenerateCode()` + `CodeResults()` | DatabaseController.GenerateCode() |
| File management | `ManageFiles()` + `DeleteFile()` | DatabaseController.ManageFiles() |
| DataTables endpoint | `GetTableData()` | DatabaseController.GetTableData() |

**Views to create** (adapted from sql2csv.web to Bootstrap 5):
- `Views/Database/Index.cshtml` — Upload + file management
- `Views/Database/Analyze.cshtml` — Schema analysis results
- `Views/Database/AnalyzeTable.cshtml` — Table detail with stats
- `Views/Database/ExportResults.cshtml` — Export confirmation
- `Views/Database/CodeResults.cshtml` — Generated C# code display

**Core services already available** (no new business logic needed):
- `IDatabaseAnalysisService` — validate, analyze
- `ISchemaService` — table names, column metadata, row counts
- `IExportService` — CSV export
- `ICodeGenerationService` — C# DTO generation
- `IPersistedFileService` — file management

**Features NOT ported** (DataSpark.Web already has superior versions):
- PerformanceController (metrics dashboard) — replaced by Serilog structured logging
- UnifiedDataController — DataSpark.Web has more complete unified analysis
- Custom TailwindCSS table components — DataSpark.Web uses Bootstrap 5 DataTables
