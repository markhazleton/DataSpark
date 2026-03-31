using DataSpark.Core.Interfaces;
using DataSpark.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataSpark.Core.Services;

/// <summary>
/// Orchestrates recursive database discovery with summary metadata aggregation.
/// </summary>
public sealed class DatabaseDiscoverySummaryService : IDatabaseDiscoverySummaryService
{
    private readonly IDatabaseDiscoveryService _discoveryService;
    private readonly ISchemaService _schemaService;
    private readonly ILogger<DatabaseDiscoverySummaryService> _logger;

    public DatabaseDiscoverySummaryService(
        IDatabaseDiscoveryService discoveryService,
        ISchemaService schemaService,
        ILogger<DatabaseDiscoverySummaryService> logger)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _schemaService = schemaService ?? throw new ArgumentNullException(nameof(schemaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<DiscoveryScanResult> ScanAsync(string directoryPath, bool recursive = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        _logger.LogInformation("Starting discovery scan in {DirectoryPath}, recursive={Recursive}", directoryPath, recursive);

        var scanPaths = recursive
            ? new[] { directoryPath }.Concat(Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories))
            : [directoryPath];

        var allDatabases = new List<DatabaseConfiguration>();
        foreach (var scanPath in scanPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var found = await _discoveryService.DiscoverDatabasesAsync(scanPath, cancellationToken).ConfigureAwait(false);
            allDatabases.AddRange(found);
        }

        var unique = allDatabases
            .GroupBy(d => d.ConnectionString, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var summaries = new List<DatabaseDiscoverySummary>();
        foreach (var db in unique)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dbPath = ExtractDatabasePath(db.ConnectionString);
            long sizeBytes = 0;
            if (!string.IsNullOrWhiteSpace(dbPath) && File.Exists(dbPath))
            {
                sizeBytes = new FileInfo(dbPath).Length;
            }

            var tables = await _schemaService.GetTableNamesAsync(db.ConnectionString, cancellationToken).ConfigureAwait(false);
            summaries.Add(new DatabaseDiscoverySummary
            {
                Path = dbPath,
                SizeBytes = sizeBytes,
                TableCount = tables.Count()
            });
        }

        _logger.LogInformation("Discovery scan complete: found {Count} database(s)", summaries.Count);

        return new DiscoveryScanResult { Databases = summaries };
    }

    private static string ExtractDatabasePath(string connectionString)
    {
        const string prefix = "Data Source=";
        if (!connectionString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        return connectionString.Substring(prefix.Length).Trim();
    }
}
