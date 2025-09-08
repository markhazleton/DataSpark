using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Models;
using Sql2Csv.Web.Models;

namespace Sql2Csv.Web.Services;

/// <summary>
/// Unified service for handling database file operations in the web application
/// </summary>
public interface IUnifiedWebDataService
{
    /// <summary>
    /// Saves an uploaded database file and returns basic information
    /// </summary>
    Task<(bool Success, string? ErrorMessage, string? FilePath, DataSourceType FileType, object AdditionalInfo)> SaveUploadedFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Analyzes a database file and returns unified analysis results
    /// </summary>
    Task<UnifiedAnalysisViewModel> AnalyzeDataSourceAsync(string filePath, DataSourceType fileType, string? tableName = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets paginated data from a data source
    /// </summary>
    Task<TableDataResult> GetDataAsync(string filePath, DataSourceType fileType, string? tableName, DataTablesRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets available tables for a database file
    /// </summary>
    Task<List<DataSourceInfo>> GetAvailableDataSourcesAsync(string filePath, DataSourceType fileType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Exports database tables to CSV
    /// </summary>
    Task<List<ExportResultViewModel>> ExportToCsvAsync(string filePath, DataSourceType fileType, List<string> dataSourceNames, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Determines file type from uploaded file
    /// </summary>
    Task<(DataSourceType Type, string? ErrorMessage)> DetermineFileTypeAsync(IFormFile file);
    
    /// <summary>
    /// Cleanup temporary files
    /// </summary>
    void CleanupTempFiles();
}

/// <summary>
/// Implementation of the unified web data service
/// </summary>
public class UnifiedWebDataService : IUnifiedWebDataService
{
    private readonly IWebDatabaseService _databaseService;
    private readonly IUnifiedAnalysisService _unifiedAnalysisService;
    private readonly ILogger<UnifiedWebDataService> _logger;
    private readonly string _tempDirectory;
    private readonly HashSet<string> _tempFiles = [];

    public UnifiedWebDataService(
        IWebDatabaseService databaseService,
        IUnifiedAnalysisService unifiedAnalysisService,
        ILogger<UnifiedWebDataService> logger)
    {
        _databaseService = databaseService;
        _unifiedAnalysisService = unifiedAnalysisService;
        _logger = logger;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "Sql2Csv.Web.Unified");

        // Ensure temp directory exists
        Directory.CreateDirectory(_tempDirectory);
    }

    public Task<(DataSourceType Type, string? ErrorMessage)> DetermineFileTypeAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return Task.FromResult<(DataSourceType Type, string? ErrorMessage)>((DataSourceType.Database, "No file provided"));
        }

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        // Check for database files
        var dbExtensions = new[] { ".db", ".sqlite", ".sqlite3" };
        if (dbExtensions.Contains(fileExtension))
        {
            return Task.FromResult<(DataSourceType Type, string? ErrorMessage)>((DataSourceType.Database, null));
        }

        return Task.FromResult<(DataSourceType Type, string? ErrorMessage)>((DataSourceType.Database, $"Unsupported file type: {fileExtension}. Supported types: .db, .sqlite, .sqlite3"));
    }

    public async Task<(bool Success, string? ErrorMessage, string? FilePath, DataSourceType FileType, object AdditionalInfo)> SaveUploadedFileAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null)
            {
                return (false, "No file provided", null, DataSourceType.Database, new { });
            }

            _logger.LogInformation("Starting file upload for: {FileName}", file.FileName);
            
            var (fileType, typeError) = await DetermineFileTypeAsync(file).ConfigureAwait(false);
            _logger.LogInformation("Detected file type: {FileType}, Error: {Error}", fileType, typeError);
            
            if (typeError != null)
            {
                return (false, typeError, null, fileType, new { });
            }

            if (fileType == DataSourceType.Database)
            {
                _logger.LogInformation("Processing as database file");
                var (success, errorMessage, filePath, tableCount) = await _databaseService.SaveUploadedFileAsync(file, cancellationToken).ConfigureAwait(false);
                return (success, errorMessage, filePath, fileType, new { TableCount = tableCount });
            }

            return (false, "Unsupported file type", null, fileType, new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving uploaded file: {FileName}", file?.FileName);
            return (false, "An error occurred while saving the file", null, DataSourceType.Database, new { });
        }
    }

    public async Task<UnifiedAnalysisViewModel> AnalyzeDataSourceAsync(string filePath, DataSourceType fileType, string? tableName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileType == DataSourceType.Database)
            {
                // Use existing database analysis but adapt to unified format
                var dbAnalysis = await _databaseService.AnalyzeDatabaseAsync(filePath, cancellationToken);
                return AdaptDatabaseAnalysisToUnified(dbAnalysis, filePath);
            }

            throw new NotSupportedException($"File type {fileType} is not supported");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing data source: {FilePath} ({FileType})", filePath, fileType);
            throw;
        }
    }

    private UnifiedAnalysisViewModel AdaptDatabaseAnalysisToUnified(DatabaseAnalysisViewModel dbAnalysis, string filePath)
    {
        return new UnifiedAnalysisViewModel
        {
            FilePath = filePath,
            FileName = dbAnalysis.DatabaseName,
            FileType = DataSourceType.Database,
            DataSources = dbAnalysis.Tables.Select(t => new DataSourceInfoViewModel
            {
                Name = t.Name,
                DisplayName = t.Name,
                Type = "Table",
                RowCount = 0, // Database analysis doesn't currently provide row counts
                ColumnCount = t.ColumnCount,
                Description = $"Database table with {t.ColumnCount} columns"
            }).ToList(),
            Summary = new DataSourceSummaryViewModel
            {
                TotalDataSources = dbAnalysis.Tables.Count,
                TotalColumns = dbAnalysis.Tables.Sum(t => t.ColumnCount),
                TotalRows = 0, // Not available in current database analysis
                FileSize = new FileInfo(filePath).Length,
                AnalysisDate = DateTime.UtcNow
            }
        };
    }

    public async Task<TableDataResult> GetDataAsync(string filePath, DataSourceType fileType, string? tableName, DataTablesRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileType == DataSourceType.Database)
            {
                if (string.IsNullOrEmpty(tableName))
                {
                    throw new ArgumentException("Table name is required for database files");
                }
                return await _databaseService.GetTableDataAsync(filePath, tableName, request, cancellationToken);
            }

            throw new NotSupportedException($"File type {fileType} is not supported");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data: {FilePath} ({FileType}) - Table: {TableName}", filePath, fileType, tableName);
            throw;
        }
    }

    public async Task<List<DataSourceInfo>> GetAvailableDataSourcesAsync(string filePath, DataSourceType fileType, CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileType == DataSourceType.Database)
            {
                var dbAnalysis = await _databaseService.AnalyzeDatabaseAsync(filePath, cancellationToken).ConfigureAwait(false);
                return dbAnalysis.Tables.Select(t => new DataSourceInfo
                {
                    Name = t.Name,
                    DisplayName = t.Name,
                    Type = DataSourceType.Database,
                    RowCount = 0, // Not available in current implementation
                    ColumnCount = t.ColumnCount
                }).ToList();
            }

            throw new NotSupportedException($"File type {fileType} is not supported");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available data sources: {FilePath} ({FileType})", filePath, fileType);
            throw;
        }
    }

    public async Task<List<ExportResultViewModel>> ExportToCsvAsync(string filePath, DataSourceType fileType, List<string> dataSourceNames, CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileType == DataSourceType.Database)
            {
                return await _databaseService.ExportTablesToCsvAsync(filePath, dataSourceNames, cancellationToken);
            }

            throw new NotSupportedException($"File type {fileType} is not supported");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to CSV: {FilePath} ({FileType})", filePath, fileType);
            throw;
        }
    }

    public void CleanupTempFiles()
    {
        try
        {
            _databaseService.CleanupTempFiles();

            foreach (var tempFile in _tempFiles.ToList())
            {
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                        _logger.LogDebug("Cleaned up temp file: {TempFile}", tempFile);
                    }
                    _tempFiles.Remove(tempFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup temp file: {TempFile}", tempFile);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during temp file cleanup");
        }
    }
}
