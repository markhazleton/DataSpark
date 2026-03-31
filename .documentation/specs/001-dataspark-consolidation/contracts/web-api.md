# REST API Contract: DataSpark Platform

**Branch**: `001-dataspark-consolidation` | **Date**: 2026-03-30

## Common

### Authentication
All API endpoints require the `X-Api-Key` header.

```
X-Api-Key: <configured-api-key>
```

Missing or invalid key → `401 Unauthorized`.

### Response Envelope

All responses follow a consistent envelope:

```json
{
  "status": "success" | "error",
  "data": { ... },
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Human-readable message"
  },
  "meta": {
    "timestamp": "2026-03-30T12:00:00Z",
    "requestId": "guid"
  }
}
```

- `data` is present only on success.
- `error` is present only on failure.
- Internal details (stack traces, file paths) are NEVER exposed.

### Common Error Codes

| HTTP Status | Code | Description |
|-------------|------|-------------|
| 400 | VALIDATION_ERROR | Invalid input |
| 401 | UNAUTHORIZED | Missing or invalid API key |
| 404 | NOT_FOUND | Resource not found |
| 413 | FILE_TOO_LARGE | Upload exceeds size limit |
| 415 | UNSUPPORTED_TYPE | Invalid file type |
| 500 | INTERNAL_ERROR | Server error (no details) |

---

## File Management API

### POST /api/Files/upload

Upload a CSV or SQLite file.

**Request**: `multipart/form-data`

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| file | binary | Yes | Max 50 MB, `.csv` or `.db` extension |

**Response** (200):
```json
{
  "status": "success",
  "data": {
    "fileName": "sales-data.csv",
    "fileType": "CSV",
    "fileSize": 1048576,
    "rowCount": 25000,
    "columnCount": 12,
    "uploadDate": "2026-03-30T12:00:00Z"
  }
}
```

**Errors**: 413 (too large), 415 (unsupported type), 400 (validation failure)

---

### GET /api/Files/list

List all available files (uploads + samples).

**Query Parameters**: None

**Response** (200):
```json
{
  "status": "success",
  "data": {
    "files": [
      {
        "fileName": "sales-data.csv",
        "fileType": "CSV",
        "fileSize": 1048576,
        "rowCount": 25000,
        "columnCount": 12,
        "uploadDate": "2026-03-30T12:00:00Z",
        "isReadOnly": false,
        "description": null
      },
      {
        "fileName": "census-data.csv",
        "fileType": "CSV",
        "fileSize": 512000,
        "rowCount": 10000,
        "columnCount": 8,
        "uploadDate": "2026-01-01T00:00:00Z",
        "isReadOnly": true,
        "description": "US Census sample data"
      }
    ]
  }
}
```

---

### GET /api/Files/data

Retrieve paginated data from a file.

**Query Parameters**:

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| fileName | string | Required | File name |
| skip | int | 0 | Rows to skip |
| take | int | 25 | Rows to return (max 1000) |
| sortColumn | string? | null | Column to sort by |
| sortDirection | string? | "asc" | "asc" or "desc" |
| search | string? | null | Global search term |

**Response** (200):
```json
{
  "status": "success",
  "data": {
    "fileName": "sales-data.csv",
    "totalRows": 25000,
    "filteredRows": 25000,
    "skip": 0,
    "take": 25,
    "columns": ["Id", "Date", "Region", "Revenue"],
    "rows": [
      ["1", "2026-01-01", "West", "45000"],
      ["2", "2026-01-02", "East", "32000"]
    ]
  }
}
```

---

### GET /api/Files/analysis

Get EDA analysis for a file.

**Query Parameters**:

| Param | Type | Required |
|-------|------|----------|
| fileName | string | Yes |

**Response** (200):
```json
{
  "status": "success",
  "data": {
    "fileName": "sales-data.csv",
    "rowCount": 25000,
    "columnCount": 12,
    "columns": [
      {
        "name": "Revenue",
        "inferredDataType": "Numeric",
        "nullCount": 15,
        "uniqueCount": 18742,
        "statistics": {
          "mean": 42500.75,
          "median": 38000.00,
          "mode": 35000.00,
          "stdDev": 12340.50,
          "min": 1200.00,
          "max": 125000.00,
          "q1": 28000.00,
          "q3": 55000.00,
          "iqr": 27000.00,
          "skewness": 0.85,
          "kurtosis": 2.15
        }
      },
      {
        "name": "Region",
        "inferredDataType": "Categorical",
        "nullCount": 0,
        "uniqueCount": 4,
        "statistics": null
      }
    ]
  }
}
```

---

### DELETE /api/Files/{fileName}

Delete an uploaded file. Sample (read-only) files cannot be deleted.

**Response** (200):
```json
{ "status": "success", "data": { "deleted": "sales-data.csv" } }
```

**Errors**: 404 (not found), 400 (read-only sample)

---

## Chart API

### GET /api/Chart/charttypes

List supported chart types.

**Response** (200):
```json
{
  "status": "success",
  "data": {
    "chartTypes": [
      "Column", "Bar", "Line", "Area", "Pie", "Doughnut",
      "Scatter", "Bubble", "Radar", "StackedColumn", "StackedBar",
      "Spline", "StepLine", "Combination"
    ]
  }
}
```

---

### GET /api/Chart/palettes

List available color palettes.

**Response** (200):
```json
{
  "status": "success",
  "data": {
    "palettes": ["Default", "Pastel", "Vibrant", "Monochrome", "Earth", "Ocean"]
  }
}
```

---

### POST /api/Chart/render

Render a chart from configuration.

**Request** (`application/json`):
```json
{
  "dataSource": "sales-data.csv",
  "chartType": "Column",
  "xAxis": {
    "columnName": "Region",
    "label": "Sales Region",
    "aggregationType": "None"
  },
  "yAxis": {
    "columnName": "Revenue",
    "label": "Total Revenue",
    "aggregationType": "Sum"
  },
  "series": [],
  "filters": [
    {
      "columnName": "Year",
      "operator": "Equals",
      "value": "2026"
    }
  ],
  "palette": "Default",
  "is3D": false
}
```

**Response** (200):
```json
{
  "status": "success",
  "data": {
    "chartHtml": "<div>...</div>",
    "dataPoints": 4,
    "renderTimeMs": 120
  }
}
```

---

### POST /api/Chart/validate

Validate a chart configuration without rendering.

**Request**: Same as `/api/Chart/render`

**Response** (200):
```json
{
  "status": "success",
  "data": {
    "isValid": true,
    "warnings": ["Column 'Revenue' has 15 null values that will be excluded"]
  }
}
```

---

## Database API

### GET /api/Database/schema

Get schema for an uploaded SQLite database.

**Query Parameters**:

| Param | Type | Required |
|-------|------|----------|
| fileName | string | Yes |

**Response** (200):
```json
{
  "status": "success",
  "data": {
    "databaseName": "chinook.db",
    "sizeBytes": 884736,
    "tables": [
      {
        "name": "artists",
        "rowCount": 275,
        "columns": [
          {
            "name": "ArtistId",
            "dataType": "INTEGER",
            "isNullable": false,
            "isPrimaryKey": true,
            "defaultValue": null
          },
          {
            "name": "Name",
            "dataType": "TEXT",
            "isNullable": true,
            "isPrimaryKey": false,
            "defaultValue": null
          }
        ]
      }
    ]
  }
}
```

---

### GET /api/Database/export

Export a database table to CSV.

**Query Parameters**:

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| fileName | string | Required | Database file |
| tableName | string | Required | Table to export |
| delimiter | string | "," | CSV delimiter |
| includeHeaders | bool | true | Include column headers |

**Response**: `text/csv` file download (Content-Disposition: attachment)

**Errors**: 404 (database not found), 400 (table name missing)

---

### GET /api/Database/export-all

Export all tables from a database as a single ZIP file (one CSV per table).

**Query Parameters**:

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| fileName | string | Required | Database file |
| delimiter | string | "," | CSV delimiter |
| includeHeaders | bool | true | Include column headers |

**Response**: `application/zip` file download
- Content-Disposition: `attachment; filename="<databaseName>-tables.zip"`
- ZIP contains one CSV file per table, named `<tableName>.csv`

**Errors**: 404 (database not found)

---

### GET /api/Database/generate-dto

Generate C# DTO code from database schema.

**Query Parameters**:

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| fileName | string | Required | Database file |
| namespace | string | "DataSpark.Models" | C# namespace |
| tableName | string? | null | Specific table (null = all) |

**Response** (200):
```json
{
  "status": "success",
  "data": {
    "files": [
      {
        "className": "Artist",
        "code": "namespace DataSpark.Models;\n\npublic record Artist\n{\n    public long ArtistId { get; init; }\n    public string? Name { get; init; }\n}\n"
      }
    ]
  }
}
```
