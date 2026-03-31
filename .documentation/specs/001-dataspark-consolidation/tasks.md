# Tasks: DataSpark Platform Consolidation

**Input**: Design documents from `.documentation/specs/001-dataspark-consolidation/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Not explicitly requested in feature specification. Test tasks are omitted. Existing 115+ tests will be updated as part of the rename tasks.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Core library**: `DataSpark.Core/` (renamed from `Sql2Csv.Core/`)
- **Web application**: `DataSpark.Web/` (promoted from `DataSpark.Web/`, absorbs `sql2csv.web/`)
- **Console CLI**: `DataSpark.Console/` (renamed from `sql2csv.console/`)
- **Tests**: `DataSpark.Tests/` (renamed from `Sql2Csv.Tests/`)
- **Benchmarks**: `DataSpark.Benchmarks/` (renamed from `Sql2Csv.Benchmarks/`)

---

## Phase 1: Setup (Project Rename & Restructure)

**Purpose**: Rebrand the entire solution from Sql2Csv to DataSpark per research.md R1 rename mapping. This is the single most impactful phase — every subsequent task depends on the renamed project structure.

- [ ] T001 Rename solution file `sql2csv.sln` → `DataSpark.sln` and update all project references inside it
- [ ] T002 Rename strong-name key file `Sql2Csv.snk` → `DataSpark.snk` and update .csproj references
- [ ] T003 Rename folder `Sql2Csv.Core/` → `DataSpark.Core/` and rename `Sql2Csv.Core.csproj` → `DataSpark.Core.csproj`
- [ ] T004 Rename folder `sql2csv.console/` → `DataSpark.Console/` and rename `Sql2Csv.csproj` → `DataSpark.Console.csproj`
- [ ] T005 Rename folder `Sql2Csv.Tests/` → `DataSpark.Tests/` and rename `Sql2Csv.Tests.csproj` → `DataSpark.Tests.csproj`
- [ ] T006 Rename folder `Sql2Csv.Benchmarks/` → `DataSpark.Benchmarks/` and rename `Sql2Csv.Benchmarks.csproj` → `DataSpark.Benchmarks.csproj`
- [ ] T007 Update `RootNamespace` and `AssemblyName` in all 5 .csproj files to use `DataSpark.*` naming *(must complete before T009)*
- [ ] T008 Global text replace `Sql2Csv` → `DataSpark` in all .cs files across all projects (namespaces, usings, class names like `Sql2CsvOptions` → `DataSparkOptions`)
- [ ] T009 Global text replace `Sql2Csv` → `DataSpark` in all .csproj, .cshtml, .json, and .md files *(depends on T007; skip RootNamespace/AssemblyName properties already handled by T007)*
- [ ] T010 Update `DataSpark.Web.csproj` project references to point to renamed `DataSpark.Core.csproj` path
- [ ] T011 Update `DataSpark.Tests.csproj` project references to point to renamed Core and Web .csproj paths
- [ ] T012 Update `DataSpark.Benchmarks.csproj` project references to point to renamed Core .csproj path
- [ ] T013 Update `DataSpark.Console.csproj` project references to point to renamed Core .csproj path
- [ ] T014 Remove `sql2csv.web/` project folder and its reference from `DataSpark.sln` (features absorbed in Phase 4)
- [ ] T015 Run `dotnet build DataSpark.sln` and fix all compilation errors from the rename
- [ ] T016 Run `dotnet test DataSpark.Tests` and fix all test failures from the rename
- [ ] T017 Update `.github/copilot-instructions.md` AND `.github/agents/copilot-instructions.md` to replace all `sql2csv` references with `DataSpark`
- [ ] T018 Update `README.md` to reflect DataSpark branding, project structure, and build instructions

**Checkpoint**: Solution builds and all existing tests pass under the new DataSpark.* naming. The codebase is fully rebranded.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story work can begin. Includes security fixes, compiler strictness, and shared models/middleware.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T019 Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to `DataSpark.Tests/DataSpark.Tests.csproj` (constitution gap per research.md R7)
- [ ] T020 Fix SQL injection in `DataSpark.Core/Services/DatabaseAnalysisService.cs` line ~729 — replace string concatenation in LIKE clause with parameterized query per research.md R2
- [ ] T021 [P] Create `ApiEnvelope<T>` response model in `DataSpark.Core/Models/ApiEnvelope.cs` with `Status`, `Data`, `Error`, `Meta` fields per contracts/web-api.md
- [ ] T022 [P] Create `ApiKeyAuthMiddleware` in `DataSpark.Web/Middleware/ApiKeyAuthMiddleware.cs` per research.md R3 — checks `X-Api-Key` header on `/api/*` routes, returns 401 if invalid
- [ ] T023 Register `ApiKeyAuthMiddleware` in `DataSpark.Web/Program.cs` pipeline with `app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/api"), ...)`
- [ ] T024 Add API key configuration section to `DataSpark.Web/appsettings.json` with `ApiKey:HeaderName` and `ApiKey:Key` settings
- [ ] T025 [P] Create `SampleDataService` in `DataSpark.Core/Services/SampleDataService.cs` and interface `ISampleDataService` in `DataSpark.Core/Interfaces/ISampleDataService.cs` — lists read-only sample CSV files from a configured directory
- [ ] T026 [P] Copy 8 sample CSV datasets to `DataSpark.Web/wwwroot/sample-data/` per research.md R4: adult.csv, legislators.csv, mps.csv, TitanicManifest.csv, heroes_information.csv, SouthlakeCodeEnforcement.csv, Beverages.csv, DesktopOS.csv
- [ ] T027 Register `SampleDataService` in DI container in `DataSpark.Web/Program.cs`
- [ ] T028 [P] Add `[ValidateAntiForgeryToken]` attribute to all `[HttpPost]` *controller actions* missing it in `DataSpark.Web/Controllers/` (constitution Principle IV; view-level `@Html.AntiForgeryToken()` covered by T103)
- [ ] T029 Run `dotnet build DataSpark.sln` with TreatWarningsAsErrors enabled and fix any new warnings
- [ ] T030 Run `dotnet test DataSpark.Tests` and verify all tests pass with security fixes

**Checkpoint**: Foundation ready — API key middleware active, sample data available, SQL injection fixed, compiler strictness enforced. User story implementation can now begin.

---

## Phase 3: User Story 1 — Upload and Explore Any Data File (Priority: P1) 🎯 MVP

**Goal**: Users upload CSV or SQLite files via drag-and-drop and immediately see a comprehensive EDA report with statistics, data types, missing values, and a paginated data preview.

**Independent Test**: Upload a CSV file and a SQLite .db file. Verify EDA report renders with all statistics, preview grid, and column analysis.

**FRs**: FR-001, FR-002, FR-003, FR-004, FR-005, FR-007, FR-008, FR-009, FR-010, FR-045, FR-046

### Implementation for User Story 1

- [ ] T031 [P] [US1] Verify `DataFile` model exists or create in `DataSpark.Core/Models/DataFile.cs` with fields: FileName, FileType (enum CSV/SQLite), FileSize, UploadDate, RowCount, ColumnCount, StoragePath, RetentionExpiry, IsReadOnly, Description per data-model.md
- [ ] T032 [P] [US1] Verify `DataColumn` model exists or create in `DataSpark.Core/Models/DataColumn.cs` with fields: Name, InferredDataType (enum), NullCount, UniqueCount, SampleValues, Statistics per data-model.md
- [ ] T033 [P] [US1] Verify `ColumnStatistics` model exists or create in `DataSpark.Core/Models/ColumnStatistics.cs` with all 12 statistical fields per data-model.md (Mean, Median, Mode, StdDev, Variance, Min, Max, Q1, Q3, IQR, Skewness, Kurtosis)
- [ ] T034 [US1] Verify file upload logic in `DataSpark.Web/Controllers/HomeController.cs` supports drag-and-drop CSV and SQLite upload with file type + size + content validation (FR-001, FR-002, FR-045)
- [ ] T035 [US1] Verify CSV delimiter auto-detection in `DataSpark.Core/Services/CsvProcessingService.cs` supports comma, tab, pipe, and semicolon (FR-003)
- [ ] T036 [US1] Verify column data type inference in `DataSpark.Core/Services/CsvProcessingService.cs` for numeric, categorical, datetime, boolean types (FR-004)
- [ ] T037 [US1] Verify EDA report generation computes all required statistics per FR-008 (numeric: mean/median/mode/stddev/variance/min/max/Q1/Q3/IQR/skewness/kurtosis), FR-009 (categorical: unique count/mode/top-N frequency), and the overall data quality score per FR-007 (% non-null/non-empty across all columns)
- [ ] T038 [US1] Verify file persistence service stores uploads in `DataSpark.Web/wwwroot/files/` with configurable retention (FR-005)
- [ ] T039 [US1] Verify file name sanitization prevents path traversal in upload processing (FR-046)
- [ ] T040 [US1] Ensure data preview grid in the EDA view supports server-side pagination, sorting, and search for datasets with 100K+ rows (FR-010)
- [ ] T041 [US1] Integrate sample datasets from `SampleDataService` into the home page file listing — wire up data retrieval, show 8+ samples with read-only badge alongside user uploads *(UI description enrichment with names/row counts/domain tags handled in T099)*
- [ ] T042 [US1] Verify upload error handling: file too large (> 50 MB) shows clear message, invalid file type shows clear message, corrupted SQLite shows "not a valid SQLite database"

**Checkpoint**: Users can upload CSV/SQLite files and see a full EDA report with statistics, column analysis, and paginated data preview. Sample datasets also appear and are explorable.

---

## Phase 4: User Story 2 — Interactive Chart Creation and Export (Priority: P1) 🎯 MVP

**Goal**: Users configure interactive charts (14+ types) with a visual UI, see real-time previews, and export as PNG/SVG/CSV/JSON.

**Independent Test**: Upload a CSV, navigate to Charts, build a column chart with aggregation, verify live preview renders, export as PNG.

**FRs**: FR-011, FR-012, FR-013, FR-014, FR-015

### Implementation for User Story 2

- [ ] T043 [P] [US2] Verify `ChartConfiguration` model exists or create in `DataSpark.Core/Models/ChartConfiguration.cs` with all fields per data-model.md (Id, Name, DataSource, ChartType enum with 14 types, XAxis, YAxis, Series, Filters, Palette, Is3D, dates)
- [ ] T044 [P] [US2] Verify `ChartAxis`, `ChartSeries`, `ChartFilter` models exist or create in `DataSpark.Core/Models/` per data-model.md
- [ ] T045 [US2] Verify chart configuration UI in `DataSpark.Web/Views/Chart/` allows selecting chart type, X/Y axes, aggregation function, series, and filters with real-time preview (FR-012)
- [ ] T046 [US2] Verify all 14 chart types render correctly: Column, Bar, Line, Area, Pie, Doughnut, Scatter, Bubble, Radar, StackedColumn, StackedBar, Spline, StepLine, Combination (FR-011)
- [ ] T047 [US2] Verify chart data filtering (include/exclude values, range filters) is applied before rendering in `DataSpark.Core/Services/ChartService.cs` (FR-013)
- [ ] T048 [US2] Verify chart export supports PNG, JPEG, SVG, CSV (data), and JSON (configuration) formats (FR-014)
- [ ] T049 [US2] Verify chart configuration save/load/duplicate/delete in `DataSpark.Web/Controllers/ChartController.cs` (FR-015)

**Checkpoint**: Full chart creation pipeline works — configure, preview, save, export. All 14 chart types render with sample data.

---

## Phase 5: User Story 3 — Interactive Pivot Table Analysis (Priority: P2)

**Goal**: Drag-and-drop pivot table builder with multiple renderers (table, heatmap, bar, line) and configuration persistence (server + localStorage).

**Independent Test**: Upload a CSV, build a pivot with row/column/value fields, verify render, switch to heatmap, export as CSV.

**FRs**: FR-016, FR-017, FR-018, FR-019, FR-020

### Implementation for User Story 3

- [ ] T050 [P] [US3] Verify `PivotConfiguration` model exists or create in `DataSpark.Core/Models/PivotConfiguration.cs` per data-model.md
- [ ] T051 [US3] Verify PivotTable.js 2.23.0 drag-and-drop builder in `DataSpark.Web/Views/PivotTable/` supports row/column/value fields with Sum/Count/Average/Min/Max aggregation (FR-016, FR-017)
- [ ] T052 [US3] Verify multiple pivot renderers (Table, Heatmap, BarChart, LineChart) switch without data loss (FR-018)
- [ ] T053 [US3] Verify pivot export in CSV, TSV, JSON, and Excel formats via PivotTable.js export renderers (FR-019)
- [ ] T054 [US3] Add localStorage persistence for pivot configurations per research.md R6 — save on config change, restore on page load, add "Clear Saved State" button (FR-020)
- [ ] T055 [US3] Verify server-side pivot configuration save/load in `DataSpark.Web/Controllers/PivotTableController.cs` (FR-020)

**Checkpoint**: Pivot tables fully functional with drag-and-drop, multiple renderers, export, and dual persistence (localStorage + server).

---

## Phase 6: User Story 4 — SQLite Database Tools (Priority: P2)

**Goal**: Upload SQLite databases, browse schema, export tables to CSV, generate C# DTOs. Absorb sql2csv.web features into DataSpark.Web.

**Independent Test**: Upload a SQLite .db with 3 tables. Verify schema display, export one table to CSV, generate C# DTOs.

**FRs**: FR-021, FR-022, FR-023, FR-024

### Implementation for User Story 4

- [ ] T056 [P] [US4] Verify `SchemaInfo`, `TableInfo`, `ColumnInfo` models exist or create in `DataSpark.Core/Models/` per data-model.md
- [ ] T057 [P] [US4] Verify `ExportResult` model exists or create in `DataSpark.Core/Models/ExportResult.cs` per data-model.md
- [ ] T058 [US4] Create `DatabaseController` in `DataSpark.Web/Controllers/DatabaseController.cs` with actions: Upload, Analyze, AnalyzeTable, ExportTables, GenerateCode, ManageFiles, GetTableData per research.md R8
- [ ] T059 [P] [US4] Create `DataSpark.Web/Views/Database/Index.cshtml` — SQLite upload + file management view (adapted from sql2csv.web to Bootstrap 5)
- [ ] T060 [P] [US4] Create `DataSpark.Web/Views/Database/Analyze.cshtml` — schema analysis results view showing tables, columns, types, row counts (FR-022)
- [ ] T061 [P] [US4] Create `DataSpark.Web/Views/Database/AnalyzeTable.cshtml` — single table detail view with column stats and data preview
- [ ] T062 [P] [US4] Create `DataSpark.Web/Views/Database/ExportResults.cshtml` — export confirmation view
- [ ] T063 [P] [US4] Create `DataSpark.Web/Views/Database/CodeResults.cshtml` — generated C# DTO code display view
- [ ] T064 [US4] Verify single table CSV export via existing Core `IExportService` with configurable delimiter and header inclusion (FR-023)
- [ ] T065 [US4] Implement "Export All Tables" as a single ZIP download (one CSV per table) via `DatabaseController.ExportTables()` — zip filename: `<databaseName>-tables.zip` (FR-023)
- [ ] T066 [US4] Verify C# DTO generation via existing Core `ICodeGenerationService` with PascalCase naming, correct type mappings (INTEGER→long, TEXT→string, REAL→double, BLOB→byte[]), and nullable annotations (FR-024)
- [ ] T067 [US4] Add "Database" link to the main navigation in `DataSpark.Web/Views/Shared/_Layout.cshtml`

**Checkpoint**: Full SQLite database workflow works in the web UI — upload, schema browse, table export, DTO generation. All sql2csv.web features absorbed into DataSpark.Web.

---

## Phase 7: User Story 5 — AI-Powered Data Insights (Priority: P2)

**Goal**: Users send CSV data to OpenAI Assistants API for AI-powered analysis with custom prompts and multi-file comparison.

**Independent Test**: Upload a CSV, navigate to AI Processing, enter a custom prompt, verify a coherent AI response is returned.

**FRs**: FR-027, FR-028, FR-029, FR-030, FR-031

### Implementation for User Story 5

- [ ] T068 [US5] Verify OpenAI integration in `DataSpark.Core/Services/OpenAIService.cs` (or equivalent) uses Assistants API v2 for file-based analysis (FR-027)
- [ ] T069 [US5] Verify file registration, listing, and removal with OpenAI API in the AI processing controller (FR-028)
- [ ] T070 [US5] Verify multi-file comparison analysis — select 2+ files and get cross-dataset AI response (FR-029)
- [ ] T071 [US5] Verify diagnostics endpoint validates OpenAI configuration (API key, assistant ID, connectivity) in `DataSpark.Web/Controllers/CsvAIProcessingController.cs` (FR-030)
- [ ] T072 [US5] Verify graceful degradation when AI services unavailable — all non-AI features remain fully functional, AI page shows clear error message (FR-031)

**Checkpoint**: AI analysis pipeline works end-to-end — upload file, analyze with custom prompt, view response. Graceful failure when OpenAI unavailable.

---

## Phase 8: User Story 6 — Statistical Analysis (Priority: P3)

**Goal**: Univariate analysis (box plots, descriptive stats, frequency charts) and bivariate analysis (correlation, scatter plots, grouped stats).

**Independent Test**: Upload a CSV with numeric and categorical columns. Run univariate on numeric (verify box plot + stats) and on categorical (verify frequency chart).

**FRs**: FR-025, FR-026

### Implementation for User Story 6

- [ ] T073 [P] [US6] Verify `AnalysisResult` model exists or create in `DataSpark.Core/Models/AnalysisResult.cs` per data-model.md with fields for EDA, Univariate, Bivariate, AI result types
- [ ] T074 [US6] Verify univariate analysis in `DataSpark.Core/Services/UnivariateService.cs` (or equivalent) generates server-side SVG box plots for numeric columns and bar charts for categorical columns using ScottPlot (FR-025)
- [ ] T075 [US6] Verify bivariate analysis computes Pearson correlation coefficient for numeric pairs and generates scatter plots with trend lines (FR-026)
- [ ] T076 [US6] Verify bivariate analysis for mixed types (categorical + numeric) shows grouped statistics (mean/median per category) with grouped bar or box plot (FR-026)
- [ ] T077 [US6] Verify univariate/bivariate views in `DataSpark.Web/Views/Univariate/` render SVG visualizations and statistics tables correctly

**Checkpoint**: Univariate and bivariate analysis fully functional with SVG visualizations for all column type combinations.

---

## Phase 9: User Story 7 — Command-Line Interface (Priority: P3)

**Goal**: CLI tool with `discover`, `export`, `schema`, `generate` commands per contracts/cli.md.

**Independent Test**: Run `dataspark discover --path ./data/` and verify output. Run `dataspark export` and verify CSV files created.

**FRs**: FR-032, FR-033, FR-034

### Implementation for User Story 7

- [ ] T078 [US7] Refactor `DataSpark.Console/Program.cs` to use System.CommandLine 2.x with root command and 4 subcommands: discover, export, schema, generate per contracts/cli.md
- [ ] T079 [US7] Implement `discover` command in `DataSpark.Console/Presentation/Commands/DiscoverCommand.cs` with --path, --recursive, --format (text/json/markdown) options and exit codes per contracts/cli.md
- [ ] T080 [US7] Implement `export` command in `DataSpark.Console/Presentation/Commands/ExportCommand.cs` with --path, --output, --tables, --delimiter, --no-headers options per contracts/cli.md (FR-034)
- [ ] T081 [US7] Implement `schema` command in `DataSpark.Console/Presentation/Commands/SchemaCommand.cs` with --path, --format (text/json/markdown), --table options per contracts/cli.md (FR-033)
- [ ] T082 [US7] Implement `generate` command in `DataSpark.Console/Presentation/Commands/GenerateCommand.cs` with --path, --output, --namespace, --table options per contracts/cli.md
- [ ] T083 [US7] Add --help and --version global options to root CLI command (FR-032)

**Checkpoint**: All 4 CLI commands work with correct options, output formats, and exit codes per contracts/cli.md.

---

## Phase 10: User Story 8 — Data Grid with Advanced Filtering (Priority: P3)

**Goal**: Add DataTables SearchPanes for multi-column filtering, server-side pagination for large datasets.

**Independent Test**: Upload a CSV with 10K+ rows. Verify SearchPanes appear, filter counts update, and exported data respects active filters.

**FRs**: FR-010 (enhanced)

### Implementation for User Story 8

- [ ] T084 [US8] Add DataTables SearchPanes CSS and JS CDN references to `DataSpark.Web/Views/Shared/_Layout.cshtml` or relevant data grid views
- [ ] T085 [US8] Configure SearchPanes in DataTables initialization JavaScript in data grid views — enable multi-column filter panels per research.md R5
- [ ] T086 [US8] Verify server-side pagination handles 50,000+ rows with < 2 second page load time
- [ ] T087 [US8] Verify CSV export of filtered data respects active SearchPane and column filters

**Checkpoint**: Data grids have SearchPanes with multi-column filtering, server-side pagination, and filtered export.

---

## Phase 11: User Story 9 — Theming and Branding (Priority: P3)

**Goal**: WebSpark suite branding, 20+ Bootswatch themes with real-time switching, responsive design.

**Independent Test**: Switch theme from Flatly to Darkly, verify all pages render correctly. Confirm DataSpark branding in nav and footer.

**FRs**: FR-038, FR-039, FR-040

### Implementation for User Story 9

- [ ] T088 [US9] Update navigation bar in `DataSpark.Web/Views/Shared/_Layout.cshtml` with DataSpark logo/brand name and WebSpark suite identity (FR-038)
- [ ] T089 [US9] Update footer in `DataSpark.Web/Views/Shared/_Layout.cshtml` with "Part of the WebSpark suite" branding (FR-038)
- [ ] T090 [US9] Verify Bootswatch theme switcher (via WebSpark.Bootswatch) persists user preference across sessions (FR-039)
- [ ] T091 [US9] Verify all pages are fully responsive across desktop, tablet, and mobile viewports (FR-040)

**Checkpoint**: DataSpark branding visible on every page. Theme switching works across all 20+ themes. Fully responsive.

---

## Phase 12: User Story 10 — RESTful API (Priority: P3)

**Goal**: RESTful API endpoints for file management, chart operations, and database tools with consistent JSON envelope responses.

**Independent Test**: POST file to `/api/Files/upload`, GET `/api/Files/list`, verify uploaded file appears. GET `/api/Chart/charttypes`.

**FRs**: FR-035, FR-036, FR-037, FR-050

### Implementation for User Story 10

- [ ] T092 [P] [US10] Create `FilesApiController` in `DataSpark.Web/Controllers/Api/FilesApiController.cs` with endpoints: POST upload, GET list, GET data (paginated), GET analysis, DELETE per contracts/web-api.md
- [ ] T093 [P] [US10] Create `ChartApiController` in `DataSpark.Web/Controllers/Api/ChartApiController.cs` with endpoints: GET charttypes, GET palettes, POST render, POST validate per contracts/web-api.md
- [ ] T094 [P] [US10] Create `DatabaseApiController` in `DataSpark.Web/Controllers/Api/DatabaseApiController.cs` with endpoints: GET schema, GET export (CSV download), GET generate-dto per contracts/web-api.md
- [ ] T095 [US10] Ensure all API controllers return `ApiEnvelope<T>` responses with consistent status/data/error/meta fields (FR-037)
- [ ] T096 [US10] Ensure all API error responses return structured error codes (VALIDATION_ERROR, NOT_FOUND, etc.) without exposing internal details (FR-037)
- [ ] T097 [US10] Verify API key authentication middleware protects all `/api/*` routes — requests without valid key return 401 (FR-050)

**Checkpoint**: All 10 REST API endpoints functional with consistent envelope format and API key auth.

---

## Phase 13: User Story 11 — Sample Datasets and Guided Onboarding (Priority: P3)

**Goal**: 8+ sample datasets visible on home page with descriptions, enabling immediate exploration without uploading personal data.

**Independent Test**: Launch DataSpark with no uploads. Verify 8+ samples listed. Select one and verify full analysis pipeline works.

**FRs**: FR-006

### Implementation for User Story 11

- [ ] T098 [US11] Add sample dataset descriptions (name, domain, row count, description text) to `SampleDataService` in `DataSpark.Core/Services/SampleDataService.cs`
- [ ] T099 [US11] Enrich sample dataset presentation in `DataSpark.Web/Views/Home/Index.cshtml` — add dataset names, domain tags, row counts, and description text (depends on T041 data wiring) (FR-006)
- [ ] T100 [US11] Ensure sample datasets show a read-only badge and no delete button in the file listing UI
- [ ] T101 [US11] Verify sample datasets work through the full pipeline: EDA report, chart creation, pivot table, export

**Checkpoint**: First-time users see 8+ sample datasets and can explore immediately. Sample data cannot be deleted.

---

## Phase 14: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that span multiple user stories — formula injection protection, documentation, benchmarks, archival.

- [ ] T102 [P] Implement CSV formula injection sanitization on export — strip leading `=`, `+`, `-`, `@` from cell values in `DataSpark.Core/Services/ExportService.cs` (FR-047)
- [ ] T103 [P] Ensure all CSRF tokens present: verify `@Html.AntiForgeryToken()` or `asp-antiforgery="true"` in every *view form* across `DataSpark.Web/Views/` (FR-048; complements T028 which covers controller-level `[ValidateAntiForgeryToken]` attributes)
- [ ] T104 [P] Update `DataSpark.Benchmarks/` benchmark tests to reference renamed namespaces and verify benchmarks still compile and run
- [ ] T105 Update `CONTRIBUTING.md` to reflect DataSpark project structure and contribution guidelines
- [ ] T106 Update `README.md` with comprehensive DataSpark documentation: features, quickstart, API usage, screenshots
- [ ] T107 [P] Update `DataSpark.Web/appsettings.json` configuration section from `Sql2Csv` to `DataSpark` with all new settings (ApiKey, sample data path)
- [ ] T108 Update `DataSpark.Console/appsettings.json` configuration section from `Sql2Csv` to `DataSpark`
- [ ] T109 Run full test suite `dotnet test DataSpark.Tests` and verify 85%+ code coverage on DataSpark.Core (FR-042)
- [ ] T110 [P] Update DataAnalysisDemo README with deprecation notice redirecting to DataSpark repository (FR-051)
- [ ] T111 Run `quickstart.md` validation — follow each step in the quickstart guide and verify they all work
- [ ] T112 Final `dotnet build DataSpark.sln` — zero errors, zero warnings (TreatWarningsAsErrors enabled)
- [ ] T113 Create integration tests for the end-to-end workflow in `DataSpark.Tests/Integration/` — upload CSV → EDA report → chart configuration → CSV export roundtrip (FR-043)
- [ ] T114 [P] Verify CI configuration (`ci.yml` or equivalent) sets `MIN_COVERAGE` to 80 or higher for `DataSpark.Tests` coverage gate (constitution Principle II)
- [ ] T115 Update `.documentation/memory/constitution.md` to replace all `Sql2Csv.*` / `sql2csv.*` naming with `DataSpark.*` throughout — reflects post-Phase-1 project rename *(requires constitution governance review per constitution Governance section)*
- [ ] T116 Rename GitHub repository `markhazleton/sql2csv` → `markhazleton/DataSpark` via GitHub repository Settings → General → Repository name (FR-041; manual admin action; depends on T001)
- [ ] T117 Make `markhazleton/DataAnalysisDemo` repository private and archive it (set to read-only) via GitHub repository Settings → Danger Zone (FR-051; manual admin action; depends on T110 README update)
- [ ] T118 Manual performance test: load a 50,000+ row CSV in the Pivot Table UI and verify no browser freeze or memory errors (SC-005)
- [ ] T119 CLI batch test: run `dataspark discover` and `dataspark export` against 50+ test SQLite database files and verify zero failures (SC-006)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately. T001–T018.
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories. T019–T030.
- **User Story 1 (Phase 3)**: Depends on Phase 2. T031–T042. 🎯 MVP
- **User Story 2 (Phase 4)**: Depends on Phase 2. Can run in parallel with US1. T043–T049.
- **User Story 3 (Phase 5)**: Depends on Phase 2. T050–T055.
- **User Story 4 (Phase 6)**: Depends on Phase 2. T056–T067.
- **User Story 5 (Phase 7)**: Depends on Phase 2 + US1 file upload infrastructure. T068–T072.
- **User Story 6 (Phase 8)**: Depends on Phase 2 + US1 EDA foundation. T073–T077.
- **User Story 7 (Phase 9)**: Depends on Phase 2 (Core services). T078–T083.
- **User Story 8 (Phase 10)**: Depends on US1 data grid. T084–T087.
- **User Story 9 (Phase 11)**: Depends on Phase 2. T088–T091.
- **User Story 10 (Phase 12)**: Depends on Phase 2 (API middleware). T092–T097.
- **User Story 11 (Phase 13)**: Depends on Phase 2 (SampleDataService) + US1 (EDA pipeline). T098–T101.
- **Polish (Phase 14)**: Depends on all desired user stories being complete. T102–T112.

### User Story Dependencies

```
Phase 1 (Setup) ──→ Phase 2 (Foundational) ──┬──→ US1 (P1) ──┬──→ US5 (needs file upload)
                                              │               ├──→ US6 (needs EDA)
                                              │               ├──→ US8 (needs data grid)
                                              │               └──→ US11 (needs EDA pipeline)
                                              ├──→ US2 (P1) ──────→ (independent)
                                              ├──→ US3 (P2) ──────→ (independent)
                                              ├──→ US4 (P2) ──────→ (independent)
                                              ├──→ US7 (P3) ──────→ (independent)
                                              ├──→ US9 (P3) ──────→ (independent)
                                              └──→ US10 (P3) ─────→ (independent)
                                              
All completed stories ──→ Phase 14 (Polish)
```

### Within Each User Story

- Models before services
- Services before controllers/views
- Core implementation before integration
- Story complete = checkpoint verified

### Parallel Opportunities

**Phase 1**: T003–T006 (folder renames) can run in parallel. T008–T009 (text replacements) can run in parallel.
**Phase 2**: T021, T022, T025, T026, T028 are all independent [P] tasks.
**After Phase 2**: US1 and US2 can start simultaneously (both P1, no cross-dependency). US3, US4, US7, US9, US10 can all start independently once Phase 2 completes.
**Phase 12 (API)**: T092, T093, T094 (three API controllers) can be built in parallel.

---

## Parallel Example: After Phase 2

```
┌─ Developer/Agent A: US1 (Upload + EDA)         T031–T042
│
├─ Developer/Agent B: US2 (Charts)                T043–T049
│
└─ Developer/Agent C: US4 (Database Tools)        T056–T067

After US1 completes:
├─ US5 (AI), US6 (Stats), US8 (SearchPanes), US11 (Samples) can start
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup — full rebrand (T001–T018)
2. Complete Phase 2: Foundational — security + infrastructure (T019–T030)
3. Complete Phase 3: US1 — Upload + EDA (T031–T042)
4. Complete Phase 4: US2 — Charts (T043–T049)
5. **STOP and VALIDATE**: Both P1 stories functional independently
6. Deploy/demo MVP

### Incremental Delivery

1. Setup + Foundational → Rebranded, secured foundation
2. + US1 → Upload files, see EDA reports (MVP core)
3. + US2 → Create and export charts (MVP complete)
4. + US3, US4, US5 → Pivots, DB tools, AI (P2 batch)
5. + US6–US11 → Stats, CLI, grids, theming, API, samples (P3 batch)
6. + Polish → Formula injection fix, docs, benchmarks, archival

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks in the same phase
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable at its checkpoint
- Commit after each phase or logical group
- Stop at any checkpoint to validate story independently
- Total: **112 tasks** across 14 phases
