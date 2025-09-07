namespace Sql2Csv.Core.Models;

/// <summary>
/// Represents the result of a table export operation.
/// </summary>
public sealed record ExportResult
{
    /// <summary>
    /// Gets the database name.
    /// </summary>
    public required string DatabaseName { get; init; }

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// Gets the output file name.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Gets the exported file content.
    /// </summary>
    public required string FileContent { get; init; }

    /// <summary>
    /// Gets the output file path.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets the number of rows exported.
    /// </summary>
    public int RowCount { get; init; }

    /// <summary>
    /// Gets the export duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets a value indicating whether the export was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error message if the export failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Represents column information from a database table.
/// </summary>
public sealed record ColumnInfo
{
    /// <summary>
    /// Gets the column name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the column data type.
    /// </summary>
    public required string DataType { get; init; }

    /// <summary>
    /// Gets a value indicating whether the column allows null values.
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// Gets a value indicating whether the column is a primary key.
    /// </summary>
    public bool IsPrimaryKey { get; init; }

    /// <summary>
    /// Gets the default value for the column.
    /// </summary>
    public string? DefaultValue { get; init; }
}

/// <summary>
/// Represents table information from a database.
/// </summary>
public sealed record TableInfo
{
    /// <summary>
    /// Gets the table name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the table schema.
    /// </summary>
    public string Schema { get; init; } = "main";

    /// <summary>
    /// Gets the columns in the table.
    /// </summary>
    public IReadOnlyList<ColumnInfo> Columns { get; init; } = Array.Empty<ColumnInfo>();

    /// <summary>
    /// Gets the estimated row count.
    /// </summary>
    public long RowCount { get; init; }

    // Compatibility properties for web project
    /// <summary>
    /// Gets the table name (alias for compatibility).
    /// </summary>
    public string TableName => Name;

    /// <summary>
    /// Gets the column count (calculated property).
    /// </summary>
    public int ColumnCount => Columns.Count;

    /// <summary>
    /// Gets a value indicating whether the table has a primary key.
    /// </summary>
    public bool HasPrimaryKey => Columns.Any(c => c.IsPrimaryKey);
}
