# Implementation Plan: DataSpark Platform Consolidation

**Branch**: `001-dataspark-consolidation` | **Date**: 2026-03-30 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `.documentation/specs/001-dataspark-consolidation/spec.md`

## Summary

Rebrand and consolidate the sql2csv repository (including sql2csv.web, DataSpark.Web, Sql2Csv.Core, sql2csv.console, Sql2Csv.Tests, Sql2Csv.Benchmarks) into a unified DataSpark platform. Absorb sql2csv.web's SQLite database features into DataSpark.Web, rename all projects/namespaces to `DataSpark.*`, rename the GitHub repository and solution file, add sample datasets, API key authentication, SearchPanes, and pivot localStorage persistence. The legacy DataAnalysisDemo repository will be archived separately.

## Technical Context

**Language/Version**: C# 12 / .NET 10.0 (current LTS)
**Primary Dependencies**: ASP.NET Core MVC, CsvHelper 33.x, Microsoft.Data.Sqlite 10.x, Microsoft.Data.Analysis 0.23.x, ScottPlot 5.x, WebSpark.Bootswatch, Serilog, System.CommandLine 2.x, Chart.js, Plotly.js, PivotTable.js, DataTables 2.x
**Storage**: File system (CSV files, SQLite databases, JSON chart configs); no external RDBMS
**Testing**: MSTest 4.x + Moq 4.x + FluentAssertions 8.x + coverlet; BenchmarkDotNet 0.15.x
**Target Platform**: Windows/Linux server, cross-platform .NET 10 runtime
**Project Type**: Web application (MVC) + CLI tool + shared Core library
**Performance Goals**: EDA report < 5s for files under 10 MB; chart preview update < 2s; data grid page load < 2s for 100K+ rows; CLI batch operations for 50+ databases
**Constraints**: Max upload 50 MB default, 1 GB total storage, single-user primary deployment, API key auth on all API endpoints
**Scale/Scope**: 6 projects → 5 projects (sql2csv.web removed), 51 functional requirements, 14 chart types, 8+ sample datasets, 20+ themes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Clean Architecture — Core-First** | ✅ PASS | All business logic stays in DataSpark.Core. Web controllers remain thin delegation. Renaming preserves this structure. New DatabaseController in Web will delegate to existing Core services (IDatabaseAnalysisService, ISchemaService, IExportService, ICodeGenerationService). |
| **II. Testing Standards** | ✅ PASS | Tests rename to DataSpark.Tests. MSTest/FluentAssertions/Moq retained. Test naming convention unchanged. Coverage target 85% (exceeds constitution minimum of 80%). |
| **III. Async/Await Discipline** | ✅ PASS | All existing Core services already async with ConfigureAwait(false) and CancellationToken. No changes to async patterns needed — only namespace renaming. |
| **IV. Security — CSRF & Input Validation** | ✅ PASS | FR-045–FR-049 align with constitution requirements. FR-050 adds API key auth (new, does not conflict). All POST actions require CSRF tokens per constitution. |
| **V. Code Quality — Nullable & Compiler Strictness** | ⚠️ ACTION NEEDED | Constitution requires `TreatWarningsAsErrors` in ALL .csproj files. Currently missing from sql2csv.web and sql2csv.console. Must add to all renamed projects during consolidation. |
| **VI. Database Access — SQL Safety** | ✅ PASS | Parameterized queries throughout. Constitution notes 2 concatenation violations in UnifiedAnalysisService — fix during consolidation. |
| **VII. Structured Logging** | ✅ PASS | Serilog configured in web. All Core services use ILogger<T> with structured templates. DataSpark.Console should also use Serilog (currently uses Microsoft.Extensions.Logging — acceptable per constitution since console is not web). |

**Gate Result**: ✅ PASS with 2 corrective actions:
1. Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to all renamed .csproj files
2. Fix SQL concatenation in UnifiedAnalysisService to use bracket-escaping

## Project Structure

### Documentation (this feature)

```text
.documentation/specs/001-dataspark-consolidation/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (API contracts)
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
DataSpark.sln                          # Renamed from sql2csv.sln
├── DataSpark.Core/                    # Renamed from Sql2Csv.Core/
│   ├── Configuration/                 # Sql2CsvOptions → DataSparkOptions
│   ├── Interfaces/                    # All service contracts
│   ├── Models/                        # Domain models (CSV, DB, Chart, Analysis)
│   └── Services/                      # Business logic (Discovery, Export, Schema, Charts, AI, Analysis)
│
├── DataSpark.Web/                     # Promoted from DataSpark.Web/ (absorbs sql2csv.web)
│   ├── Controllers/                   # MVC + API controllers
│   │   ├── HomeController.cs          # Landing, file upload/management
│   │   ├── DatabaseController.cs      # NEW: SQLite upload, schema, export, DTO gen (from sql2csv.web)
│   │   ├── ChartController.cs         # Chart CRUD + preview
│   │   ├── PivotTableController.cs    # Interactive pivots
│   │   ├── UnivariateController.cs    # Univariate analysis
│   │   ├── VisualizationController.cs # General visualization
│   │   ├── CsvAIProcessingController.cs # OpenAI analysis
│   │   ├── SanityCheckController.cs   # Data quality
│   │   └── api/                       # RESTful API endpoints (with API key middleware)
│   ├── Middleware/                     # NEW: ApiKeyAuthMiddleware
│   ├── Services/                      # Web-specific adapters
│   ├── Views/                         # Razor views (+ new Database/ views)
│   └── wwwroot/
│       ├── files/                     # User uploads
│       └── sample-data/               # NEW: Read-only sample CSVs (8+ files)
│
├── DataSpark.Console/                 # Renamed from sql2csv.console/
│   └── Presentation/Commands/         # CLI commands (discover, export, schema, generate)
│
├── DataSpark.Tests/                   # Renamed from Sql2Csv.Tests/
│   ├── Configuration/
│   ├── Controllers/
│   ├── Integration/
│   ├── Models/
│   └── Services/
│
└── DataSpark.Benchmarks/              # Renamed from Sql2Csv.Benchmarks/
    └── (discovery, export, schema benchmarks)
```

**Structure Decision**: This is a multi-project .NET solution following Clean Architecture (Core library + presentation layers). The consolidation reduces from 6 projects to 5 by removing sql2csv.web and absorbing its features into DataSpark.Web. This aligns with Constitution Principle I (Core-First) and avoids the complexity violation that two overlapping web projects would create.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| 5 projects (Core + Web + Console + Tests + Benchmarks) | Each serves a distinct concern: shared logic, web UI, CLI, testing, performance | Fewer projects would violate Clean Architecture (constitution principle I) |
| API key middleware (new component) | FR-050 requires auth on all API endpoints | Attribute-based auth would require decorating every API controller individually |

## Post-Design Constitution Re-Evaluation

*Re-check after Phase 1 design (data-model.md, contracts/, quickstart.md).*

| Principle | Status | Post-Design Notes |
|-----------|--------|-------------------|
| **I. Clean Architecture — Core-First** | ✅ PASS | Data model entities (DataFile, DataColumn, ChartConfiguration, PivotConfiguration, etc.) all belong in DataSpark.Core/Models. API contracts show controllers as thin delegation layers — Chart/render delegates to Core chart service, Database/export delegates to Core export service. No business logic in contracts. |
| **II. Testing Standards** | ✅ PASS | All entities in data-model.md need corresponding tests. ExportResult, AnalysisResult, and ColumnStatistics computation tests. API contract validation testing (invalid inputs → correct error codes). Quickstart documents test commands. |
| **III. Async/Await Discipline** | ✅ PASS | All API endpoints in contracts return async responses. Database/export and Files/analysis are I/O-bound operations that must be async with CancellationToken. No design conflicts. |
| **IV. Security — CSRF & Input Validation** | ✅ PASS | API contracts specify API key auth on all endpoints (FR-050). File upload validates type + size + content. Error responses never expose internal details (stack traces, file paths). DELETE endpoint blocks deletion of read-only samples. Formula injection sanitization documented. |
| **V. Code Quality — Nullable & Compiler Strictness** | ✅ PASS (with action) | Data model uses nullable annotations correctly (Mode?, AIResponse?, etc.). Corrective action to add TreatWarningsAsErrors still required during implementation. |
| **VI. Database Access — SQL Safety** | ✅ PASS (with action) | Database API contract uses parameterized queries. CLI contracts operate on validated file paths. Corrective action to fix SQL concatenation in DatabaseAnalysisService still required. |
| **VII. Structured Logging** | ✅ PASS | No new logging patterns introduced. All services continue to use ILogger<T> with structured templates. |

**Post-Design Gate Result**: ✅ PASS — No new violations introduced by Phase 1 design. Two pre-existing corrective actions carry forward to implementation.

## Generated Artifacts

| File | Phase | Description |
|------|-------|-------------|
| [research.md](research.md) | 0 | 8 research topics resolved (namespace rename, SQL fixes, API auth, samples, SearchPanes, pivot persistence, compiler strictness, feature absorption) |
| [data-model.md](data-model.md) | 1 | 10 entities with fields, types, constraints, validation rules, and state transitions |
| [contracts/web-api.md](contracts/web-api.md) | 1 | REST API contract: 10 endpoints across Files, Chart, and Database modules with request/response schemas |
| [contracts/cli.md](contracts/cli.md) | 1 | CLI contract: 4 commands (discover, export, schema, generate) with options, output formats, exit codes |
| [quickstart.md](quickstart.md) | 1 | Developer getting-started guide: build, run, test, API usage |
