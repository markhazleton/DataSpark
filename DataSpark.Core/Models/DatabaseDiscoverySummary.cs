namespace DataSpark.Core.Models;

/// <summary>
/// Summary information for a discovered database.
/// </summary>
public sealed record DatabaseDiscoverySummary
{
    /// <summary>
    /// Gets the file path of the database.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Gets the number of tables in the database.
    /// </summary>
    public int TableCount { get; init; }
}

/// <summary>
/// Result of a database discovery scan operation.
/// </summary>
public sealed record DiscoveryScanResult
{
    /// <summary>
    /// Gets the discovered database summaries.
    /// </summary>
    public IReadOnlyList<DatabaseDiscoverySummary> Databases { get; init; } = [];
}
