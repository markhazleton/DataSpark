# Feature Specification: DataSpark Platform Consolidation

**Feature Branch**: `001-dataspark-consolidation`  
**Created**: 2026-03-30  
**Status**: Draft  
**Input**: User description: "Consolidate sql2csv, DataAnalysisDemo, and similar repos into a single rebranded DataSpark platform as part of the WebSpark suite. Full consolidation of all features from all repos into a single updated repo. When done, this will fully replace sql2csv, DataAnalysisDemo, and other similar code/repos. Make it awesome, complete, and a one-stop shop for data analysis features."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Upload and Explore Any Data File (Priority: P1)

A user visits the DataSpark web application, uploads a CSV file or SQLite database via drag-and-drop, and immediately sees a comprehensive exploratory data analysis (EDA) report. The report includes row/column counts, data type inference, descriptive statistics (mean, median, quartiles, skewness, kurtosis for numeric columns; frequency distributions for categorical columns), missing-value detection, and a sample data preview with sortable, searchable pagination.

**Why this priority**: This is the fundamental value proposition of DataSpark — turning raw data into instant insight. Every other feature (charting, pivots, AI) depends on successful data ingestion and analysis.

**Independent Test**: Upload a CSV file and a SQLite database. Verify the EDA report renders correctly with all statistics, preview grid, and column analysis for each.

**Acceptance Scenarios**:

1. **Given** a user on the DataSpark home page, **When** they drag a CSV file onto the upload area, **Then** the file is accepted, validated, and a complete EDA report is displayed within 5 seconds for files under 10 MB.
2. **Given** a user on the DataSpark home page, **When** they drag a SQLite `.db` file onto the upload area, **Then** the system validates it as a real SQLite database, lists all tables, and displays schema information (column names, types, row counts) for each table.
3. **Given** a CSV file with mixed data types, **When** the EDA report is generated, **Then** numeric columns show mean/median/min/max/quartiles/std-dev and categorical columns show top-N frequency distributions and unique counts.
4. **Given** a CSV file with missing values, **When** the EDA report is generated, **Then** each column shows null/empty count and percentage, and a data quality score is computed.
5. **Given** a file larger than the configured maximum (default 50 MB), **When** the user attempts to upload, **Then** a clear error message is shown without crashing the application.

---

### User Story 2 — Interactive Chart Creation and Export (Priority: P1)

A user selects a data source (uploaded CSV or database table), configures an interactive chart by choosing chart type, axes, series, aggregation, and filters through a visual UI, sees a real-time preview, and exports the chart as an image or data file.

**Why this priority**: Data visualization is the primary output users seek after exploring their data. Charts are the core deliverable of any data analysis tool.

**Independent Test**: Upload a CSV, navigate to Charts, configure a bar chart with category on X-axis and numeric sum on Y-axis, verify the live preview, then export as PNG.

**Acceptance Scenarios**:

1. **Given** a user with an uploaded CSV file, **When** they navigate to the Chart module and select chart type "Column", set X-axis to a categorical column and Y-axis to a numeric column with "Sum" aggregation, **Then** a live chart preview renders within 2 seconds.
2. **Given** a configured chart, **When** the user changes the chart type from Column to Pie, **Then** the preview updates in place without page reload.
3. **Given** a completed chart, **When** the user clicks "Export as PNG", **Then** a PNG image file downloads. The same works for JPEG, SVG, CSV, and JSON export formats.
4. **Given** a chart configuration, **When** the user clicks "Save", **Then** the configuration is persisted and appears in the chart list for future reuse.
5. **Given** a chart with data filters applied (e.g., "Country = USA"), **When** the chart renders, **Then** only filtered data is included in the visualization.

**Supported Chart Types**: Column, Bar, Line, Area, Pie, Doughnut, Scatter, Bubble, Radar, Combination (dual-axis), Stacked Column, Stacked Bar, Spline, Step Line.

---

### User Story 3 — Interactive Pivot Table Analysis (Priority: P2)

A user selects a CSV data source and builds a pivot table using a drag-and-drop interface, choosing row fields, column fields, value fields, and aggregation functions. The pivot table supports multiple renderers (table, heatmap, bar chart, line chart) and can be exported in multiple formats.

**Why this priority**: Pivot tables are the second most-requested analysis tool after charts. They allow non-technical users to slice data dynamically without writing queries.

**Independent Test**: Upload a CSV with at least 3 columns, build a pivot with one row field, one column field, and one value with "Count" aggregation. Verify the pivot renders, switch to heatmap renderer, then export as CSV.

**Acceptance Scenarios**:

1. **Given** a user on the Pivot Table page with a CSV loaded, **When** they drag "Department" to Rows, "Year" to Columns, and "Revenue" to Values with "Sum" aggregation, **Then** a pivot table renders showing revenue by department and year.
2. **Given** a rendered pivot table, **When** the user switches the renderer from "Table" to "Heatmap", **Then** the display changes to a color-coded heatmap without data loss.
3. **Given** a pivot table configuration, **When** the user clicks "Save Configuration", **Then** the setup is persisted and can be reloaded later, including in a new browser session.
4. **Given** a pivot table, **When** the user exports to Excel, **Then** a properly formatted spreadsheet file downloads containing the pivot data.
5. **Given** a pivot table in full-screen mode, **When** the user interacts with the pivot, **Then** the drag-and-drop interface works correctly and the entire viewport is used for the analysis.

---

### User Story 4 — SQLite Database Tools (Discovery, Schema, Export, Code Generation) (Priority: P2)

A user uploads a SQLite database file, explores its schema (tables, columns, types, row counts), exports any table to CSV, and generates C# DTO classes from the database schema. Alternatively, a user points the CLI tool at a directory to discover all SQLite databases and batch-export them.

**Why this priority**: Database tooling differentiates DataSpark from pure CSV analyzers. These features serve developers and data engineers who need to quickly inspect, export, and codify database structures.

**Independent Test**: Upload a SQLite database with 3 tables. Verify schema display (all tables, columns, types, row counts). Export one table to CSV and verify contents match. Generate C# DTOs and verify class names, property names, and types are correct.

**Acceptance Scenarios**:

1. **Given** a user uploading a `.db` file, **When** the upload completes, **Then** the system displays a schema view listing all non-system tables with column names, data types, nullable flags, primary key indicators, and row counts.
2. **Given** a displayed schema, **When** the user clicks "Export to CSV" on a specific table, **Then** a CSV file downloads containing all rows with correct headers matching column names.
3. **Given** a displayed schema, **When** the user clicks "Export All Tables", **Then** a ZIP file or multiple CSV files download, one per table.
4. **Given** a displayed schema, **When** the user clicks "Generate C# DTOs", **Then** the system generates valid C# record classes with PascalCase property names, correct .NET type mappings (INTEGER → long, TEXT → string, REAL → double, BLOB → byte[]), and nullable annotations where appropriate.
5. **Given** a CLI user running `dataspark discover --path ./databases/`, **When** the directory contains 5 `.db` files, **Then** the tool lists all 5 with file paths, sizes, and table counts.

---

### User Story 5 — AI-Powered Data Insights (Priority: P2)

A user selects one or more uploaded CSV files and requests AI-powered analysis using natural language prompts. The system sends data to OpenAI's Assistants API and returns structured insights, trends, anomalies, and recommendations.

**Why this priority**: AI analysis is a differentiating feature that transforms DataSpark from a utility tool into an intelligent analysis platform. It depends on the file management infrastructure from P1.

**Independent Test**: Upload a CSV, navigate to AI Processing, enter a custom prompt ("What trends do you see in this dataset?"), and verify a coherent AI response is returned with data-specific insights.

**Acceptance Scenarios**:

1. **Given** a user with an uploaded CSV, **When** they navigate to AI Processing and click "Analyze", **Then** the system sends the file to OpenAI and displays structured insights within 30 seconds.
2. **Given** a user entering a custom prompt ("Compare sales trends across regions"), **When** the analysis runs, **Then** the AI response addresses the specific question with references to actual columns and values in the data.
3. **Given** multiple registered files, **When** the user selects 2 files and clicks "Compare", **Then** the AI provides a cross-dataset comparison response.
4. **Given** an invalid or missing OpenAI API key, **When** the user attempts AI analysis, **Then** a clear, non-technical error message explains the issue and suggests configuration steps.
5. **Given** a user clicking "Diagnostics", **When** the diagnostics run, **Then** the system validates the OpenAI configuration and reports the status of API key, assistant ID, and connectivity.

---

### User Story 6 — Statistical Analysis (Univariate and Bivariate) (Priority: P3)

A user selects a column from a dataset and generates a univariate analysis report including descriptive statistics, distribution visualization (box plot or frequency bar chart), and outlier detection. For bivariate analysis, the user selects two columns and gets correlation metrics, scatter plots, or contingency tables depending on data types.

**Why this priority**: Deep statistical analysis serves more advanced users (data scientists, analysts) and builds on the EDA foundation from P1.

**Independent Test**: Upload a CSV with numeric and categorical columns. Run univariate analysis on a numeric column and verify the box plot SVG and statistics. Run on a categorical column and verify the frequency bar chart.

**Acceptance Scenarios**:

1. **Given** a numeric column selected for univariate analysis, **When** the analysis runs, **Then** the report shows: mean, median, mode, standard deviation, variance, min, max, Q1, Q3, IQR, skewness, kurtosis, and a box plot visualization.
2. **Given** a categorical column selected for univariate analysis, **When** the analysis runs, **Then** the report shows: unique count, mode, frequency distribution (top N values with counts and percentages), and a bar chart visualization.
3. **Given** two numeric columns selected for bivariate analysis, **When** the analysis runs, **Then** the report shows: Pearson correlation coefficient, scatter plot, and trend line.
4. **Given** one categorical and one numeric column for bivariate, **When** the analysis runs, **Then** the report shows group statistics (mean/median per category) with a grouped bar or box plot.

---

### User Story 7 — Command-Line Interface for Automation (Priority: P3)

A developer or data engineer uses the DataSpark CLI to discover databases, export tables, generate schema reports, create C# DTO code, and analyze CSV files from scripts and CI/CD pipelines.

**Why this priority**: The CLI serves power users and automation scenarios. It extends DataSpark's reach beyond the web interface into scripting workflows.

**Independent Test**: Run `dataspark discover --path ./data/`, verify output lists all `.db` files. Run `dataspark export --path ./test.db --output ./csv/`, verify CSV files are created for all tables.

**Acceptance Scenarios**:

1. **Given** a directory with SQLite databases, **When** running `dataspark discover --path <dir>`, **Then** the CLI outputs a list of all `.db` files with paths, sizes, and table counts.
2. **Given** a SQLite database, **When** running `dataspark export --path <db> --output <dir> --tables Users,Orders`, **Then** CSV files for the specified tables are created in the output directory.
3. **Given** a SQLite database, **When** running `dataspark schema --path <db> --format markdown`, **Then** a Markdown-formatted schema report is printed to stdout.
4. **Given** a SQLite database, **When** running `dataspark generate --path <db> --namespace MyApp.Models`, **Then** C# DTO source files are generated with the specified namespace.
5. **Given** any command, **When** running with `--help`, **Then** clear usage instructions with all options are displayed.

---

### User Story 8 — Data Grid with Advanced Filtering and Search (Priority: P3)

A user views data in a paginated, sortable, searchable data grid with server-side pagination for large datasets. The grid supports advanced filtering via SearchPanes (multi-column filter panels), column visibility toggling, and data export.

**Why this priority**: Advanced data grid features improve the data exploration experience for users working with large or complex datasets.

**Independent Test**: Upload a CSV with 10,000+ rows and 10+ columns. Verify server-side pagination loads pages quickly, SearchPanes appear with filter counts, and exported data respects active filters.

**Acceptance Scenarios**:

1. **Given** a dataset with 50,000 rows, **When** the user views it in the data grid, **Then** the initial page loads within 2 seconds using server-side pagination (default 25 rows per page).
2. **Given** a data grid with SearchPanes enabled, **When** the user selects a value in a SearchPane column filter, **Then** the grid filters to matching rows and other SearchPanes update their counts.
3. **Given** an active filter, **When** the user clicks "Export CSV", **Then** only the filtered subset is exported.
4. **Given** a data grid, **When** the user sorts by a column, **Then** sorting is performed server-side and results are correct even across page boundaries.

---

### User Story 9 — Theming and Branding (WebSpark Suite Integration) (Priority: P3)

A user can switch between 20+ visual themes (via Bootswatch) in real time. The DataSpark application is visually branded as part of the WebSpark suite with consistent navigation, footer branding, and design language using WebSpark shared components.

**Why this priority**: Consistent branding within the WebSpark suite establishes identity. Theme switching is already implemented and needs polish, not new development.

**Independent Test**: Load DataSpark, switch theme from "Flatly" to "Darkly", verify all pages render correctly in the dark theme. Confirm WebSpark branding is present in navigation and footer.

**Acceptance Scenarios**:

1. **Given** a user on any DataSpark page, **When** they select a different theme from the theme switcher, **Then** the entire UI updates to the new theme without page reload and the choice persists across sessions.
2. **Given** the DataSpark application, **When** any page loads, **Then** the navigation bar includes the DataSpark logo/brand name and the footer identifies it as part of the WebSpark suite.
3. **Given** a mobile device, **When** the user accesses DataSpark, **Then** all pages are fully responsive with touch-friendly navigation.

---

### User Story 10 — RESTful API for Programmatic Access (Priority: P3)

A developer integrates DataSpark into an external application or script via its RESTful API endpoints for file management, chart rendering, data retrieval, and analysis.

**Why this priority**: The API enables DataSpark to serve as a backend service, expanding its use beyond the web UI. This supports integration into dashboards, reporting tools, and automated pipelines.

**Independent Test**: Use an HTTP client to POST a file to `/api/Files/upload`, then GET `/api/Files/list` and verify the uploaded file appears. GET `/api/Chart/charttypes` and verify the list of supported types.

**Acceptance Scenarios**:

1. **Given** an API client, **When** POSTing a CSV to `/api/Files/upload`, **Then** the response includes file metadata (name, size, column count, row count) with HTTP 200.
2. **Given** an API client, **When** GETting `/api/Files/data?fileName=test.csv&skip=0&take=50`, **Then** the response includes paginated JSON data with correct row count.
3. **Given** an API client, **When** POSTing a chart configuration to `/api/Chart/render`, **Then** the response includes the rendered chart data or HTML.
4. **Given** an API client, **When** making a request with an invalid file name, **Then** the response is HTTP 404 with a structured error message (no stack traces or internal details).

---

### User Story 11 — Sample Datasets and Guided Onboarding (Priority: P3)

A first-time user sees a curated set of sample datasets (8+ CSV files covering diverse domains) bundled with the application, with guided suggestions for analysis. This allows immediate exploration without uploading personal data.

**Why this priority**: Sample data removes the barrier to entry for new users and serves as demonstration material for showcasing DataSpark capabilities.

**Independent Test**: Launch DataSpark with no prior uploads. Verify 8+ sample CSV files are listed with descriptions. Select one and verify the full analysis pipeline works (EDA, charts, pivot, etc.).

**Acceptance Scenarios**:

1. **Given** a fresh DataSpark installation, **When** the user visits the home page, **Then** at least 8 sample datasets are listed with names, row counts, and brief descriptions.
2. **Given** a sample dataset is selected, **When** the user navigates to Charts, **Then** pre-configured example charts are available demonstrating the chart system.
3. **Given** a sample dataset, **When** the user runs Complete Analysis, **Then** a full EDA report generates successfully showcasing all analysis capabilities.

---

### Edge Cases

- What happens when a CSV file has no headers? The system detects header absence and generates column names (Column1, Column2, etc.) with a warning.
- What happens when a CSV file uses a non-standard delimiter (tab, pipe, semicolon)? The system auto-detects the delimiter from the first 10 rows.
- What happens when a SQLite database is corrupted or encrypted? The system validates the SQLite header bytes and reports a clear error ("File is not a valid SQLite database" or "Database is encrypted").
- What happens when the OpenAI API is unreachable or rate-limited? The system shows a user-friendly error with retry suggestion. All non-AI features remain fully functional.
- What happens when two users upload files with the same name? Each user's session is independent; file names are scoped to avoid collision.
- What happens when a chart configuration references a column that no longer exists in the data source? The system displays a validation error and suggests updating the configuration.
- What happens when a CSV contains formula injection payloads (e.g., `=CMD()`)? The system sanitizes cell values on export to prevent formula injection in spreadsheet applications.

## Clarifications

### Session 2026-03-30

- Q: Should sql2csv.web be removed after absorbing its SQLite features into DataSpark.Web, kept alongside, or merged? → A: Remove sql2csv.web entirely after absorbing its SQLite features into DataSpark.Web (single web app).
- Q: Should the RESTful API require authentication? → A: Yes, simple API key authentication via request header for all API endpoints.
- Q: Should sample datasets be read-only or deletable by the user? → A: Read-only. Sample datasets are always available and cannot be deleted or overwritten.
- Q: Should the GitHub repository be renamed alongside internal branding? → A: Yes, rename everything — GitHub repository, solution file, projects, and namespaces — all to DataSpark.
- Q: Should machine learning features (anomaly detection, forecasting, clustering) be in initial scope? → A: Deferred to a future release. ML.NET dependencies may remain but no ML-specific UI or workflows in initial consolidation.

## Requirements *(mandatory)*

### Functional Requirements

**Data Ingestion & Management**
- **FR-001**: System MUST accept CSV file uploads via drag-and-drop and file picker, with configurable maximum file size (default 50 MB).
- **FR-002**: System MUST accept SQLite database (.db) file uploads with validation of SQLite file header integrity.
- **FR-003**: System MUST auto-detect CSV delimiters (comma, tab, pipe, semicolon) by sampling the first rows of the file.
- **FR-004**: System MUST infer column data types (numeric, categorical, datetime, boolean) from CSV data.
- **FR-005**: System MUST persist uploaded files across browser sessions with configurable retention (default 30 days) and storage limits (default 1 GB).
- **FR-006**: System MUST ship with at least 8 sample CSV datasets covering diverse domains (census, political, transportation, demographic, geographic, market, sports, and time series data). Sample datasets MUST be read-only — always available, not deletable or overwritable by the user.

**Exploratory Data Analysis**
- **FR-007**: System MUST generate a complete EDA report for any uploaded CSV, including: row count, column count, data types, missing value counts, and per-column descriptive statistics.
- **FR-008**: System MUST compute for numeric columns: mean, median, mode, standard deviation, variance, min, max, Q1, Q3, IQR, skewness, and kurtosis.
- **FR-009**: System MUST compute for categorical columns: unique count, mode, and top-N value frequency distribution with counts and percentages.
- **FR-010**: System MUST provide a paginated, sortable, searchable data preview grid with server-side pagination supporting datasets of 100,000+ rows.

**Charting & Visualization**
- **FR-011**: System MUST support at least 14 chart types: Column, Bar, Line, Area, Pie, Doughnut, Scatter, Bubble, Radar, Stacked Column, Stacked Bar, Spline, Step Line, and Combination (dual-axis).
- **FR-012**: System MUST provide a visual chart configuration UI with real-time preview that updates within 2 seconds of configuration changes.
- **FR-013**: System MUST support chart data filtering (include/exclude values, range filters) applied before rendering.
- **FR-014**: System MUST support chart export in at least 4 formats: PNG, SVG, CSV (data), and JSON (configuration).
- **FR-015**: System MUST persist chart configurations for reuse, including duplication and deletion.

**Pivot Tables**
- **FR-016**: System MUST provide a drag-and-drop pivot table builder supporting row fields, column fields, value fields, and filter fields.
- **FR-017**: System MUST support aggregation functions: Sum, Count, Average, Min, Max.
- **FR-018**: System MUST support multiple pivot renderers: Table, Heatmap, Bar Chart, Line Chart.
- **FR-019**: System MUST support pivot export in CSV, TSV, JSON, and Excel formats.
- **FR-020**: System MUST persist pivot configurations across sessions (server-side save and client-side localStorage).

**Database Tools**
- **FR-021**: System MUST discover all SQLite database files in a specified directory (CLI and web).
- **FR-022**: System MUST display full schema for any SQLite database: table names, column names, data types, nullable flags, primary key indicators, default values, and row counts.
- **FR-023**: System MUST export individual or all database tables to CSV with configurable delimiter and header inclusion.
- **FR-024**: System MUST generate syntactically valid C# record/class definitions from database schema with PascalCase naming, correct type mappings, nullable annotations, and configurable namespace.

**Statistical Analysis**
- **FR-025**: System MUST provide univariate analysis with server-generated SVG visualizations (box plots for numeric, bar charts for categorical) using a scientific plotting library.
- **FR-026**: System MUST provide bivariate analysis with correlation coefficients (Pearson for numeric pairs), grouped statistics, and visualizations.

**AI Integration**
- **FR-027**: System MUST integrate with OpenAI Assistants API v2 for file-based analysis with custom prompts.
- **FR-028**: System MUST support registering, listing, and removing files with the OpenAI API.
- **FR-029**: System MUST support multi-file comparison analysis via AI.
- **FR-030**: System MUST provide diagnostics for validating OpenAI configuration and connectivity.
- **FR-031**: System MUST degrade gracefully when AI services are unavailable — all non-AI features remain fully functional.

**CLI Tool**
- **FR-032**: System MUST provide a CLI tool with commands: `discover`, `export`, `schema`, `generate`, each with `--help`.
- **FR-033**: CLI MUST support configurable output formats for schema reports: text, JSON, Markdown.
- **FR-034**: CLI MUST support table filtering for selective export (e.g., `--tables Users,Orders`).

**API**
- **FR-035**: System MUST expose RESTful API endpoints for file management (upload, list, data retrieval, analysis).
- **FR-036**: System MUST expose RESTful API endpoints for chart operations (render, validate, list types/palettes).
- **FR-037**: API responses MUST follow a consistent envelope format with status, data, and error fields. Internal details (stack traces, file paths) MUST NOT be exposed in error responses.
- **FR-050**: All API endpoints MUST require API key authentication via a configurable request header. Requests without a valid API key MUST receive HTTP 401 Unauthorized.

**Platform & Branding**
- **FR-038**: System MUST be branded as "DataSpark" with visual identity as part of the WebSpark suite, using shared WebSpark navigation components and Bootswatch theme switching.
- **FR-039**: System MUST support 20+ Bootswatch themes with real-time switching and persistent user preference.
- **FR-040**: System MUST be fully responsive across desktop, tablet, and mobile viewports.
- **FR-041**: ALL library projects, assemblies, and root namespaces MUST start with `DataSpark`. Specific project names: DataSpark.Core, DataSpark.Web, DataSpark.Console, DataSpark.Tests, DataSpark.Benchmarks. All C# namespaces throughout the codebase MUST use the `DataSpark` prefix (e.g., `DataSpark.Core.Services`, `DataSpark.Web.Controllers`). The sql2csv.web project MUST be removed after its SQLite database features (upload, schema analysis, DTO generation, CSV export) are absorbed into DataSpark.Web, resulting in a single web application. The GitHub repository MUST be renamed from `sql2csv` to `DataSpark`, and the solution file MUST be renamed to `DataSpark.sln`.
- **FR-051**: Upon completion of the consolidation, the `markhazleton/DataAnalysisDemo` GitHub repository MUST be made private and archived (read-only). Its README MUST be updated with a notice redirecting users to the new DataSpark repository before archival.

**Quality & Testing**
- **FR-042**: System MUST maintain 85%+ unit test coverage across the Core library.
- **FR-043**: System MUST include integration tests for end-to-end workflows (upload → analyze → chart → export).
- **FR-044**: System MUST include performance benchmarks for database discovery, CSV export, and schema analysis operations.

**Security**
- **FR-045**: System MUST validate all file uploads (type, size, content integrity) before processing.
- **FR-046**: System MUST prevent path traversal attacks by sanitizing all file names and paths.
- **FR-047**: System MUST sanitize CSV cell values on export to prevent formula injection in spreadsheet applications.
- **FR-048**: System MUST use CSRF tokens on all form submissions.
- **FR-049**: System MUST use parameterized queries for all database operations — no string concatenation for SQL.

### Key Entities

- **DataFile**: An uploaded data file (CSV or SQLite). Key attributes: file name, file type, file size, upload date, row count, column count, storage path, retention expiry.
- **DataColumn**: A column within a data file. Key attributes: name, inferred data type, null count, unique count, statistics summary.
- **ChartConfiguration**: A saved chart setup. Key attributes: name, data source reference, chart type, X/Y axis mappings, series definitions, filter criteria, style options.
- **PivotConfiguration**: A saved pivot table setup. Key attributes: name, data source reference, row fields, column fields, value fields, aggregation function, renderer type.
- **SchemaInfo**: Metadata for a SQLite database. Key attributes: database path, table list, per-table column definitions (name, type, nullable, primary key, default), row counts.
- **AnalysisResult**: Output of any analysis operation. Key attributes: source file, analysis type (EDA, univariate, bivariate, AI), computed statistics, generated visualizations, timestamp.
- **ExportResult**: Outcome of an export operation. Key attributes: source, format, row count, file size, duration, success/error status.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can upload a CSV or SQLite file and see a complete analysis report within 5 seconds for files under 10 MB.
- **SC-002**: The interactive chart preview updates within 2 seconds of any configuration change.
- **SC-003**: Data grids with 100,000+ rows load paginated results within 2 seconds per page.
- **SC-004**: All 14+ chart types render correctly with sample data across all supported themes.
- **SC-005**: Pivot tables support datasets with 50,000+ rows without browser freezing or memory errors.
- **SC-006**: The CLI tool processes batch discovery and export operations for 50+ database files without failure.
- **SC-007**: Unit test coverage of the Core library is 85% or higher.
- **SC-008**: All 8+ sample datasets produce valid EDA reports, charts, and pivot tables without errors.
- **SC-009**: A new user with no prior experience can upload a file and generate their first chart within 3 minutes.
- **SC-010**: All features work correctly across the 3 most popular Bootswatch themes (Flatly, Darkly, Cosmo).
- **SC-011**: The application passes OWASP Top 10 security checks: no SQL injection, no path traversal, no XSS in rendered outputs, no formula injection in exports.
- **SC-012**: AI analysis (when configured) returns a meaningful response within 30 seconds.
- **SC-013**: The RESTful API returns structured JSON responses for all endpoints with appropriate HTTP status codes (200, 400, 404, 500).
- **SC-014**: The DataSpark brand and WebSpark suite identity are visible on every page (navigation, footer).

## Assumptions

- The target runtime is .NET 10.0 (current LTS) with C# 12.
- The application will use ASP.NET Core MVC for the web interface, not Blazor or SPA frameworks.
- Bootstrap 5 with Bootswatch theming (via WebSpark.Bootswatch) is the UI foundation.
- CsvHelper is the CSV parsing library; Microsoft.Data.Analysis provides DataFrame operations.
- ScottPlot is the server-side scientific plotting library; Chart.js and Plotly.js handle client-side interactivity.
- PivotTable.js is the pivot table library for the web interface.
- OpenAI integration uses the Assistants v2 API and requires user-provided API keys.
- The legacy DataAnalysisDemo (VB.NET / WebForms / .NET Framework 4.8) code will NOT be ported directly — features are reimplemented in modern C# / ASP.NET Core. Upon consolidation completion, the DataAnalysisDemo repository will be made private and archived.
- ALL library projects, assemblies, and C# namespaces will start with `DataSpark`. The solution file will be `DataSpark.sln`. The GitHub repository will be renamed from `sql2csv` to `DataSpark`.
- SQLite is the only supported database format at launch; additional database connectors may be added later.
- The application targets single-user local deployment primarily, with multi-user scenarios as a future consideration.
- Machine learning features (anomaly detection, forecasting, clustering via Microsoft.ML) are explicitly deferred to a future release. ML.NET package references may remain in the project but no ML-specific UI, workflows, or user stories are in scope for the initial consolidation.
- Performance targets assume a standard developer workstation (8 GB RAM, SSD).
