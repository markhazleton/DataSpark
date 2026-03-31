# Data Model: DataSpark Platform

**Branch**: `001-dataspark-consolidation` | **Date**: 2026-03-30

## Entity Relationship Overview

```
DataFile (1) ──── (*) DataColumn
    │
    ├── CSV path: analysis via CsvProcessingService
    └── SQLite path: analysis via SchemaService + DatabaseAnalysisService

ChartConfiguration (*) ──── (1) DataFile (via dataSource reference)
    └── Contains: ChartAxis(*), ChartSeries(*), ChartFilter(*)

PivotConfiguration (*) ──── (1) DataFile (via dataSource reference)

AnalysisResult (*) ──── (1) DataFile
    └── Types: EDA, Univariate, Bivariate, AI

ExportResult (*) ──── (1) DataFile or DatabaseTable
```

## Entities

### DataFile

Represents an uploaded or sample data file (CSV or SQLite).

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| FileName | string | Required, unique per storage scope | Sanitized, no path traversal chars |
| FileType | enum: CSV, SQLite | Required | Determined by extension + content validation |
| FileSize | long | >= 0 | Bytes |
| UploadDate | DateTime | Required | UTC |
| RowCount | int | >= 0 | Computed on analysis |
| ColumnCount | int | >= 0 | Computed on analysis |
| StoragePath | string | Required | Absolute path, internal only |
| RetentionExpiry | DateTime? | Nullable | Null for sample datasets (never expire) |
| IsReadOnly | bool | Required | True for sample datasets |
| Description | string? | Optional | User-editable for uploads, pre-set for samples |

### DataColumn

Represents a column within a CSV file or database table.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Name | string | Required | Original column header |
| InferredDataType | enum: Numeric, Categorical, DateTime, Boolean, Unknown | Required | Auto-detected |
| NullCount | int | >= 0 | Count of null/empty values |
| UniqueCount | int | >= 0 | Distinct non-null values |
| SampleValues | string[] | Max 10 | First distinct values for preview |
| Statistics | ColumnStatistics? | Nullable | Populated for numeric columns |

### ColumnStatistics

Computed statistics for a numeric column.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Mean | double | | |
| Median | double | | |
| Mode | double? | Nullable | May not exist |
| StdDev | double | >= 0 | |
| Variance | double | >= 0 | |
| Min | double | | |
| Max | double | | |
| Q1 | double | | 25th percentile |
| Q3 | double | | 75th percentile |
| IQR | double | >= 0 | Q3 - Q1 |
| Skewness | double | | |
| Kurtosis | double | | |

### ChartConfiguration

A saved chart setup.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | Auto-increment | |
| Name | string | Required, max 200 | User-provided |
| DataSource | string | Required | File name reference |
| ChartType | enum | Required | Column, Bar, Line, Area, Pie, Doughnut, Scatter, Bubble, Radar, StackedColumn, StackedBar, Spline, StepLine, Combination |
| XAxis | ChartAxis | Required | |
| YAxis | ChartAxis | Required | |
| Series | ChartSeries[] | 0..* | Multiple series for combination charts |
| Filters | ChartFilter[] | 0..* | Applied before rendering |
| Palette | string | Optional | Color palette name |
| Is3D | bool | Default: false | |
| CreatedDate | DateTime | Required | UTC |
| ModifiedDate | DateTime | Required | UTC |

### ChartAxis

| Field | Type | Constraints |
|-------|------|-------------|
| ColumnName | string | Required |
| Label | string? | Optional display label |
| AggregationType | enum: None, Sum, Count, Average, Min, Max | Default: None |

### ChartSeries

| Field | Type | Constraints |
|-------|------|-------------|
| Name | string | Required |
| ColumnName | string | Required |
| ChartType | enum | Optional (for combination charts) |
| Color | string? | Optional hex color |

### ChartFilter

| Field | Type | Constraints |
|-------|------|-------------|
| ColumnName | string | Required |
| Operator | enum: Equals, NotEquals, Contains, GreaterThan, LessThan, In, Between | Required |
| Value | string | Required |
| SecondValue | string? | For Between operator |

### PivotConfiguration

A saved pivot table setup.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | int | Auto-increment | |
| Name | string | Required, max 200 | |
| DataSource | string | Required | File name reference |
| RowFields | string[] | 0..* | Column names for rows |
| ColumnFields | string[] | 0..* | Column names for columns |
| ValueFields | string[] | 1..* | Column names for values |
| AggregationFunction | enum: Sum, Count, Average, Min, Max | Required | |
| RendererType | enum: Table, Heatmap, BarChart, LineChart | Default: Table | |
| CreatedDate | DateTime | Required | UTC |

### SchemaInfo

Metadata for a SQLite database (not persisted — computed on demand).

| Field | Type | Constraints |
|-------|------|-------------|
| DatabasePath | string | Required |
| DatabaseSizeBytes | long | >= 0 |
| Tables | TableInfo[] | 0..* |

### TableInfo

| Field | Type | Constraints |
|-------|------|-------------|
| Name | string | Required |
| RowCount | long | >= 0 |
| Columns | ColumnInfo[] | 1..* |

### ColumnInfo (Database)

| Field | Type | Constraints |
|-------|------|-------------|
| Name | string | Required |
| DataType | string | Required (SQLite affinity: INTEGER, TEXT, REAL, BLOB, NUMERIC) |
| IsNullable | bool | |
| IsPrimaryKey | bool | |
| DefaultValue | string? | |

### ExportResult

Outcome of an export operation.

| Field | Type | Constraints |
|-------|------|-------------|
| IsSuccess | bool | |
| SourceName | string | Table or file name |
| Format | string | CSV, JSON, etc. |
| RowCount | int | >= 0 |
| FileSizeBytes | long | >= 0 |
| Duration | TimeSpan | |
| ErrorMessage | string? | Null on success |

### AnalysisResult

Output of any analysis operation (not persisted — computed on demand).

| Field | Type | Constraints |
|-------|------|-------------|
| SourceFile | string | Required |
| AnalysisType | enum: EDA, Univariate, Bivariate, AI | Required |
| Columns | DataColumn[] | For EDA |
| Statistics | ColumnStatistics? | For univariate |
| CorrelationCoefficient | double? | For bivariate (numeric pair) |
| Visualizations | string[] | SVG strings for plots |
| AIResponse | string? | For AI analysis |
| Timestamp | DateTime | UTC |

## State Transitions

### DataFile Lifecycle
```
Upload → Validated → Analyzed → [Charted | Pivoted | AI-Analyzed | Exported] → Retained/Expired
```
Sample files skip Upload and Validated — they start in Analyzed state and never expire.

### ChartConfiguration Lifecycle
```
Created → Configured → Previewed → Saved → [Duplicated | Deleted]
```

## Validation Rules

1. **File names**: Alphanumeric, hyphens, underscores, dots only. Max 255 chars. No path separators.
2. **CSV files**: Must have at least 1 column. Delimiter auto-detected from {comma, tab, pipe, semicolon}.
3. **SQLite files**: Must pass SQLite header validation (first 16 bytes = "SQLite format 3\000").
4. **Chart configs**: Must reference an existing data source. X/Y axis columns must exist in the data.
5. **API keys**: Non-empty string, validated via middleware before any API request processing.
6. **Export cell values**: Sanitized to prevent formula injection (strip leading `=`, `+`, `-`, `@`).
