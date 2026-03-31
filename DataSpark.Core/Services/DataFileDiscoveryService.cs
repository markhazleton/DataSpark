using Microsoft.Extensions.Logging;
using DataSpark.Core.Interfaces;
using DataSpark.Core.Models;

namespace DataSpark.Core.Services;

/// <summary>
/// Service for discovering database data files.
/// </summary>
public class DataFileDiscoveryService : IDataFileDiscoveryService
{
    private readonly IDatabaseDiscoveryService _databaseDiscoveryService;
    private readonly ILogger<DataFileDiscoveryService> _logger;

    public DataFileDiscoveryService(
        IDatabaseDiscoveryService databaseDiscoveryService,
        ILogger<DataFileDiscoveryService> logger)
    {
        _databaseDiscoveryService = databaseDiscoveryService;
        _logger = logger;
    }

    public async Task<IEnumerable<DataSourceConfiguration>> DiscoverDataFilesAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Discovering data files in directory: {DirectoryPath}", directoryPath);

        var dataSources = new List<DataSourceConfiguration>();

        try
        {
            // Discover database files
            var databases = await _databaseDiscoveryService.DiscoverDatabasesAsync(directoryPath, cancellationToken).ConfigureAwait(false);
            foreach (var db in databases)
            {
                // Extract file path from connection string
                var filePath = ExtractFilePathFromConnectionString(db.ConnectionString);
                if (!string.IsNullOrEmpty(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    dataSources.Add(new DataSourceConfiguration
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = Path.GetFileNameWithoutExtension(filePath),
                        FilePath = filePath,
                        Type = DataSourceType.Database,
                        ConnectionString = db.ConnectionString,
                        FileSize = fileInfo.Length,
                        CreatedDate = fileInfo.CreationTime,
                        LastModified = fileInfo.LastWriteTime,
                        Metadata = new Dictionary<string, string>
                        {
                            ["DatabaseType"] = "SQLite",
                            ["TableCount"] = "Unknown" // Could be populated if needed
                        }
                    });
                }
            }

            // Discover CSV files
            var csvFiles = Directory.GetFiles(directoryPath, "*.csv", SearchOption.TopDirectoryOnly);
            foreach (var csvFile in csvFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileInfo = new FileInfo(csvFile);
                dataSources.Add(new DataSourceConfiguration
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = Path.GetFileNameWithoutExtension(csvFile),
                    FilePath = csvFile,
                    Type = DataSourceType.Database,
                    FileSize = fileInfo.Length,
                    CreatedDate = fileInfo.CreationTime,
                    LastModified = fileInfo.LastWriteTime,
                    Metadata = new Dictionary<string, string>
                    {
                        ["FileType"] = "CSV",
                        ["Extension"] = fileInfo.Extension
                    }
                });
            }

            // Also check for other common delimited file extensions
            var otherExtensions = new[] { "*.txt", "*.tsv", "*.tab" };
            foreach (var extension in otherExtensions)
            {
                var files = Directory.GetFiles(directoryPath, extension, SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Quick check if it might be a delimited file
                    if (await IsLikelyDelimitedFileAsync(file, cancellationToken).ConfigureAwait(false))
                    {
                        var fileInfo = new FileInfo(file);
                        dataSources.Add(new DataSourceConfiguration
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = Path.GetFileNameWithoutExtension(file),
                            FilePath = file,
                            Type = DataSourceType.Database,
                            FileSize = fileInfo.Length,
                            CreatedDate = fileInfo.CreationTime,
                            LastModified = fileInfo.LastWriteTime,
                            Metadata = new Dictionary<string, string>
                            {
                                ["FileType"] = "Delimited",
                                ["Extension"] = fileInfo.Extension,
                                ["DetectedAsDelimited"] = "true"
                            }
                        });
                    }
                }
            }

            _logger.LogInformation("Discovered {Count} database files", 
                dataSources.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering data files in directory: {DirectoryPath}", directoryPath);
            throw;
        }

        return dataSources.OrderBy(d => d.Type).ThenBy(d => d.Name);
    }

    private async Task<bool> IsLikelyDelimitedFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            // Read first few lines to check if it looks like a delimited file
            using var reader = new StreamReader(filePath);
            var sampleLines = new List<string>();
            
            for (int i = 0; i < 5; i++)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    sampleLines.Add(line);
                }
            }

            if (!sampleLines.Any()) return false;

            // Check for common delimiters
            var delimiters = new[] { ",", ";", "\t", "|" };
            foreach (var delimiter in delimiters)
            {
                var firstLineCount = sampleLines.First().Split(delimiter).Length;
                if (firstLineCount > 1)
                {
                    // Check if other lines have similar column counts
                    var otherLineCounts = sampleLines.Skip(1).Select(line => line.Split(delimiter).Length);
                    if (otherLineCounts.All(count => Math.Abs(count - firstLineCount) <= 1))
                    {
                        return true; // Looks like a consistent delimited file
                    }
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string? ExtractFilePathFromConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        // Handle SQLite connection strings like "Data Source=path/to/file.db"
        var dataSourceIndex = connectionString.IndexOf("Data Source=", StringComparison.OrdinalIgnoreCase);
        if (dataSourceIndex >= 0)
        {
            var start = dataSourceIndex + "Data Source=".Length;
            var remaining = connectionString.Substring(start);
            
            // Find the end of the file path (semicolon or end of string)
            var semicolonIndex = remaining.IndexOf(';');
            var filePath = semicolonIndex >= 0 ? remaining.Substring(0, semicolonIndex) : remaining;
            
            return filePath.Trim();
        }

        return null;
    }
}
