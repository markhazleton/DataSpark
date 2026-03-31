namespace DataSpark.Core.Models;

/// <summary>
/// Metadata for a SQLite database schema.
/// </summary>
public sealed record SchemaInfo
{
    /// <summary>
    /// Full path to the database file.
    /// </summary>
    public required string DatabasePath { get; init; }

    /// <summary>
    /// Database file size in bytes.
    /// </summary>
    public long DatabaseSizeBytes { get; init; }

    /// <summary>
    /// Tables discovered in the database.
    /// </summary>
    public IReadOnlyList<TableInfo> Tables { get; init; } = Array.Empty<TableInfo>();
}
