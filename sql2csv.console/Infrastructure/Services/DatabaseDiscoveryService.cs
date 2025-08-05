using Microsoft.Extensions.Logging;
using Sql2Csv.Core.Models;
using Sql2Csv.Core.Interfaces;

namespace Sql2Csv.Infrastructure.Services;

/// <summary>
/// Service for discovering database files.
/// </summary>
public sealed class DatabaseDiscoveryService : IDatabaseDiscoveryService
{
    private readonly ILogger<DatabaseDiscoveryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseDiscoveryService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public DatabaseDiscoveryService(ILogger<DatabaseDiscoveryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DatabaseConfiguration>> DiscoverDatabasesAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        _logger.LogInformation("Discovering databases in directory: {DirectoryPath}", directoryPath);

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning("Directory does not exist: {DirectoryPath}", directoryPath);
                return Enumerable.Empty<DatabaseConfiguration>();
            }

            var databaseFiles = await Task.Run(() =>
                Directory.GetFiles(directoryPath, "*.db", SearchOption.TopDirectoryOnly),
                cancellationToken);

            var databases = new List<DatabaseConfiguration>();

            foreach (var filePath in databaseFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var config = DatabaseConfiguration.FromSqliteFile(filePath);
                    databases.Add(config);
                    _logger.LogDebug("Discovered database: {DatabaseName} at {FilePath}", config.Name, filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create configuration for database file: {FilePath}", filePath);
                }
            }

            _logger.LogInformation("Discovered {DatabaseCount} databases", databases.Count);
            return databases;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering databases in directory: {DirectoryPath}", directoryPath);
            throw;
        }
    }
}
