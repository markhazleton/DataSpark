using DataSpark.Core.Models;

namespace DataSpark.Core.Interfaces;

/// <summary>
/// Service for performing recursive database discovery with summary metadata.
/// </summary>
public interface IDatabaseDiscoverySummaryService
{
    /// <summary>
    /// Scans a directory (optionally recursively) for SQLite databases and returns summary metadata.
    /// </summary>
    /// <param name="directoryPath">The root directory to scan.</param>
    /// <param name="recursive">Whether to scan subdirectories.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A discovery scan result containing database summaries.</returns>
    Task<DiscoveryScanResult> ScanAsync(string directoryPath, bool recursive = false, CancellationToken cancellationToken = default);
}
