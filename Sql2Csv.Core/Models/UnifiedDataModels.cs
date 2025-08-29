using System.ComponentModel.DataAnnotations;

namespace Sql2Csv.Core.Models;

/// <summary>
/// Represents the type of data source.
/// </summary>
public enum DataSourceType
{
    Database,
    Csv
}

/// <summary>
/// Unified configuration for both database and CSV data sources.
/// </summary>
public class DataSourceConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data source name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data source type.
    /// </summary>
    public DataSourceType Type { get; set; }

    /// <summary>
    /// Gets or sets the table name (for database sources).
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// Gets or sets the connection string (for database sources).
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the CSV delimiter (for CSV sources).
    /// </summary>
    public string? CsvDelimiter { get; set; }

    /// <summary>
    /// Gets or sets whether the CSV has headers (for CSV sources).
    /// </summary>
    public bool? CsvHasHeaders { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the last modified date.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Results from CSV file analysis.
/// </summary>
public class CsvAnalysisResult
{
    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of rows.
    /// </summary>
    public long RowCount { get; set; }

    /// <summary>
    /// Gets or sets the number of columns.
    /// </summary>
    public int ColumnCount { get; set; }

    /// <summary>
    /// Gets or sets the detected delimiter.
    /// </summary>
    public string Delimiter { get; set; } = ",";

    /// <summary>
    /// Gets or sets whether headers were detected.
    /// </summary>
    public bool HasHeaders { get; set; }

    /// <summary>
    /// Gets or sets the column analyses.
    /// </summary>
    public List<CsvColumnAnalysis> ColumnAnalyses { get; set; } = new();

    /// <summary>
    /// Gets or sets any parsing errors encountered.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the encoding used.
    /// </summary>
    public string Encoding { get; set; } = "UTF-8";
}

/// <summary>
/// Analysis results for a CSV column.
/// </summary>
public class CsvColumnAnalysis
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the column index.
    /// </summary>
    public int ColumnIndex { get; set; }

    /// <summary>
    /// Gets or sets the detected data type.
    /// </summary>
    public string DataType { get; set; } = "TEXT";

    /// <summary>
    /// Gets or sets the number of non-null values.
    /// </summary>
    public long NonNullCount { get; set; }

    /// <summary>
    /// Gets or sets the number of null values.
    /// </summary>
    public long NullCount { get; set; }

    /// <summary>
    /// Gets or sets the number of unique values.
    /// </summary>
    public long UniqueCount { get; set; }

    /// <summary>
    /// Gets or sets the minimum value (for numeric/date columns).
    /// </summary>
    public string? MinValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum value (for numeric/date columns).
    /// </summary>
    public string? MaxValue { get; set; }

    /// <summary>
    /// Gets or sets the mean value (for numeric columns).
    /// </summary>
    public double? Mean { get; set; }

    /// <summary>
    /// Gets or sets the standard deviation (for numeric columns).
    /// </summary>
    public double? StandardDeviation { get; set; }

    /// <summary>
    /// Gets or sets sample values from the column.
    /// </summary>
    public List<string> SampleValues { get; set; } = new();
}

/// <summary>
/// Results from CSV data retrieval.
/// </summary>
public class CsvDataResult
{
    /// <summary>
    /// Gets or sets the column names.
    /// </summary>
    public List<string> Columns { get; set; } = new();

    /// <summary>
    /// Gets or sets the data rows.
    /// </summary>
    public List<List<string>> Rows { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of rows available.
    /// </summary>
    public long TotalRows { get; set; }

    /// <summary>
    /// Gets or sets the number of rows returned.
    /// </summary>
    public int ReturnedRows { get; set; }

    /// <summary>
    /// Gets or sets the starting row index.
    /// </summary>
    public int StartIndex { get; set; }
}

/// <summary>
/// Unified analysis results for both database tables and CSV files.
/// </summary>
public class UnifiedAnalysisResult
{
    /// <summary>
    /// Gets or sets the data source configuration.
    /// </summary>
    public DataSourceConfiguration DataSource { get; set; } = new();

    /// <summary>
    /// Gets or sets the display name for the data source.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of rows.
    /// </summary>
    public long RowCount { get; set; }

    /// <summary>
    /// Gets or sets the number of columns.
    /// </summary>
    public int ColumnCount { get; set; }

    /// <summary>
    /// Gets or sets the column analyses.
    /// </summary>
    public List<ColumnAnalysis> ColumnAnalyses { get; set; } = new();

    /// <summary>
    /// Gets or sets any errors encountered during analysis.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the analysis timestamp.
    /// </summary>
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Unified column analysis for both database and CSV columns.
/// </summary>
public class ColumnAnalysis
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the column index.
    /// </summary>
    public int ColumnIndex { get; set; }

    /// <summary>
    /// Gets or sets the data type.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the column allows null values.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Gets or sets whether the column is a primary key.
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Gets or sets the number of non-null values.
    /// </summary>
    public long NonNullCount { get; set; }

    /// <summary>
    /// Gets or sets the number of null values.
    /// </summary>
    public long NullCount { get; set; }

    /// <summary>
    /// Gets or sets the number of unique values.
    /// </summary>
    public long UniqueCount { get; set; }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public string? MinValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public string? MaxValue { get; set; }

    /// <summary>
    /// Gets or sets the mean value (for numeric columns).
    /// </summary>
    public double? Mean { get; set; }

    /// <summary>
    /// Gets or sets the standard deviation (for numeric columns).
    /// </summary>
    public double? StandardDeviation { get; set; }

    /// <summary>
    /// Gets or sets sample values from the column.
    /// </summary>
    public List<string> SampleValues { get; set; } = new();

    /// <summary>
    /// Gets or sets additional statistics specific to the data type.
    /// </summary>
    public Dictionary<string, object> AdditionalStats { get; set; } = new();
}

/// <summary>
/// Unified data results for both database tables and CSV files.
/// </summary>
public class UnifiedDataResult
{
    /// <summary>
    /// Gets or sets the data source configuration.
    /// </summary>
    public DataSourceConfiguration DataSource { get; set; } = new();

    /// <summary>
    /// Gets or sets the column names.
    /// </summary>
    public List<string> Columns { get; set; } = new();

    /// <summary>
    /// Gets or sets the data rows.
    /// </summary>
    public List<Dictionary<string, object?>> Rows { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of rows available.
    /// </summary>
    public long TotalRows { get; set; }

    /// <summary>
    /// Gets or sets the number of rows returned.
    /// </summary>
    public int ReturnedRows { get; set; }

    /// <summary>
    /// Gets or sets the starting row index.
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Gets or sets any errors encountered.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
