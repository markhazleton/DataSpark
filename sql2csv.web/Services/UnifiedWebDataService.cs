using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Models;
using Sql2Csv.Web.Models;

namespace Sql2Csv.Web.Services;

/// <summary>
/// Unified service for handling both database and CSV file operations in the web application
/// </summary>
public interface IUnifiedWebDataService
{
    /// <summary>
    /// Saves an uploaded file (database or CSV) and returns basic information
    /// </summary>
    Task<(bool Success, string? ErrorMessage, string? FilePath, DataSourceType FileType, object AdditionalInfo)> SaveUploadedFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Analyzes a data source (database or CSV file) and returns unified analysis results
    /// </summary>
    Task<UnifiedAnalysisViewModel> AnalyzeDataSourceAsync(string filePath, DataSourceType fileType, string? tableName = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets paginated data from a data source
    /// </summary>
    Task<TableDataResult> GetDataAsync(string filePath, DataSourceType fileType, string? tableName, DataTablesRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets available tables/data sources for a file
    /// </summary>
    Task<List<DataSourceInfo>> GetAvailableDataSourcesAsync(string filePath, DataSourceType fileType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Exports data to CSV (for database tables, this converts to CSV; for CSV files, this might apply transformations)
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
    private readonly ICsvAnalysisService _csvAnalysisService;
    private readonly ILogger<UnifiedWebDataService> _logger;
    private readonly string _tempDirectory;
    private readonly HashSet<string> _tempFiles = [];

    public UnifiedWebDataService(
        IWebDatabaseService databaseService,
        IUnifiedAnalysisService unifiedAnalysisService,
        ICsvAnalysisService csvAnalysisService,
        ILogger<UnifiedWebDataService> logger)
    {
        _databaseService = databaseService;
        _unifiedAnalysisService = unifiedAnalysisService;
        _csvAnalysisService = csvAnalysisService;
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

        // Check for CSV files
        var csvExtensions = new[] { ".csv", ".tsv", ".txt", ".tab" };
        if (csvExtensions.Contains(fileExtension))
        {
            return Task.FromResult<(DataSourceType Type, string? ErrorMessage)>((DataSourceType.Csv, null));
        }

        return Task.FromResult<(DataSourceType Type, string? ErrorMessage)>((DataSourceType.Database, $"Unsupported file type: {fileExtension}. Supported types: .db, .sqlite, .sqlite3, .csv, .tsv, .txt, .tab"));
    }

    public async Task<(bool Success, string? ErrorMessage, string? FilePath, DataSourceType FileType, object AdditionalInfo)> SaveUploadedFileAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            var (fileType, typeError) = await DetermineFileTypeAsync(file);
            if (typeError != null)
            {
                return (false, typeError, null, fileType, new { });
            }

            else if (fileType == DataSourceType.Database)
            {
                var (success, errorMessage, filePath, tableCount) = await _databaseService.SaveUploadedFileAsync(file, cancellationToken);
                return (success, errorMessage, filePath, fileType, new { TableCount = tableCount });
            }
            else if (fileType == DataSourceType.Csv)
            {
                return await SaveCsvFileAsync(file, cancellationToken);
            }

            return (false, "Unsupported file type", null, fileType, new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving uploaded file: {FileName}", file?.FileName);
            return (false, $"Error saving file: {ex.Message}", null, DataSourceType.Database, new { });
        }
    }

    private async Task<(bool Success, string? ErrorMessage, string? FilePath, DataSourceType FileType, object AdditionalInfo)> SaveCsvFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            // Validate file size (max 100MB for CSV files)
            if (file.Length > 100 * 1024 * 1024)
            {
                return (false, "File size too large. Maximum size is 100MB for CSV files.", null, DataSourceType.Csv, new { });
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(_tempDirectory, fileName);

            // Save file to disk
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await file.CopyToAsync(fileStream, cancellationToken);
            }

            // Track temp file for cleanup
            _tempFiles.Add(filePath);

            // Quick validation - try to read first few lines
            try
            {
                using var reader = new StreamReader(filePath);
                var firstLine = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(firstLine))
                {
                    File.Delete(filePath);
                    _tempFiles.Remove(filePath);
                    return (false, "CSV file appears to be empty.", null, DataSourceType.Csv, new { });
                }

                // Basic analysis to get column count
                var quickAnalysis = await _csvAnalysisService.AnalyzeCsvAsync(filePath, cancellationToken);
                
                _logger.LogInformation("Successfully uploaded and validated CSV file: {FileName} with {ColumnCount} columns, {RowCount} rows", 
                    file.FileName, quickAnalysis.ColumnCount, quickAnalysis.RowCount);

                return (true, null, filePath, DataSourceType.Csv, new 
                { 
                    ColumnCount = quickAnalysis.ColumnCount, 
                    RowCount = quickAnalysis.RowCount,
                    HasHeaders = quickAnalysis.HasHeaders,
                    Delimiter = quickAnalysis.Delimiter
                });
            }
            catch (Exception ex)
            {
                // Cleanup invalid file
                File.Delete(filePath);
                _tempFiles.Remove(filePath);
                return (false, $"Invalid CSV file: {ex.Message}", null, DataSourceType.Csv, new { });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading CSV file: {FileName}", file?.FileName);
            return (false, $"Error uploading CSV file: {ex.Message}", null, DataSourceType.Csv, new { });
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
            else if (fileType == DataSourceType.Csv)
            {
                return await AnalyzeCsvFileAsync(filePath, cancellationToken);
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

    private async Task<UnifiedAnalysisViewModel> AnalyzeCsvFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var csvAnalysis = await _csvAnalysisService.AnalyzeCsvAsync(filePath, cancellationToken);
        
        return new UnifiedAnalysisViewModel
        {
            FilePath = filePath,
            FileName = csvAnalysis.FileName,
            FileType = DataSourceType.Csv,
            DataSources = new List<DataSourceInfoViewModel>
            {
                new DataSourceInfoViewModel
                {
                    Name = Path.GetFileNameWithoutExtension(csvAnalysis.FileName),
                    DisplayName = csvAnalysis.FileName,
                    Type = "CSV File",
                    RowCount = csvAnalysis.RowCount,
                    ColumnCount = csvAnalysis.ColumnCount,
                    Description = $"CSV file with {csvAnalysis.ColumnCount} columns, {csvAnalysis.RowCount} rows",
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        ["Delimiter"] = csvAnalysis.Delimiter,
                        ["HasHeaders"] = csvAnalysis.HasHeaders,
                        ["Encoding"] = csvAnalysis.Encoding
                    }
                }
            },
            Summary = new DataSourceSummaryViewModel
            {
                TotalDataSources = 1,
                TotalColumns = csvAnalysis.ColumnCount,
                TotalRows = csvAnalysis.RowCount,
                FileSize = csvAnalysis.FileSize,
                AnalysisDate = DateTime.UtcNow
            },
            ColumnAnalyses = csvAnalysis.ColumnAnalyses.Select(col => new UnifiedColumnAnalysisViewModel
            {
                ColumnName = col.ColumnName,
                DataType = col.DataType,
                NonNullCount = col.NonNullCount,
                NullCount = col.NullCount,
                UniqueCount = col.UniqueCount,
                MinValue = col.MinValue,
                MaxValue = col.MaxValue,
                Mean = col.Mean,
                StandardDeviation = col.StandardDeviation,
                SampleValues = col.SampleValues
            }).ToList()
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
            else if (fileType == DataSourceType.Csv)
            {
                return await GetCsvDataAsync(filePath, request, cancellationToken);
            }

            throw new NotSupportedException($"File type {fileType} is not supported");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data: {FilePath} ({FileType})", filePath, fileType);
            throw;
        }
    }

    private async Task<TableDataResult> GetCsvDataAsync(string filePath, DataTablesRequest request, CancellationToken cancellationToken)
    {
        var csvData = await _csvAnalysisService.GetCsvDataAsync(filePath, request.Start, request.Length, cancellationToken);
        
        return new TableDataResult
        {
            Data = csvData.Rows.Select(row => row.Cast<object?>().ToArray()).ToArray(),
            Draw = request.Draw,
            RecordsTotal = (int)csvData.TotalRows,
            RecordsFiltered = (int)csvData.TotalRows, // TODO: Implement filtering for CSV
            Columns = csvData.Columns
        };
    }

    public async Task<List<DataSourceInfo>> GetAvailableDataSourcesAsync(string filePath, DataSourceType fileType, CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileType == DataSourceType.Database)
            {
                var dbAnalysis = await _databaseService.AnalyzeDatabaseAsync(filePath, cancellationToken);
                return dbAnalysis.Tables.Select(t => new DataSourceInfo
                {
                    Name = t.Name,
                    DisplayName = t.Name,
                    Type = DataSourceType.Database,
                    RowCount = 0, // Not available in current implementation
                    ColumnCount = t.ColumnCount
                }).ToList();
            }
            else if (fileType == DataSourceType.Csv)
            {
                var csvAnalysis = await _csvAnalysisService.AnalyzeCsvAsync(filePath, cancellationToken);
                return new List<DataSourceInfo>
                {
                    new DataSourceInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(csvAnalysis.FileName),
                        DisplayName = csvAnalysis.FileName,
                        Type = DataSourceType.Csv,
                        RowCount = csvAnalysis.RowCount,
                        ColumnCount = csvAnalysis.ColumnCount
                    }
                };
            }

            return new List<DataSourceInfo>();
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
            else if (fileType == DataSourceType.Csv)
            {
                // For CSV files, we might just copy the file or apply transformations
                // For now, just indicate that the file is already in CSV format
                return new List<ExportResultViewModel>
                {
                    new ExportResultViewModel
                    {
                        TableName = Path.GetFileNameWithoutExtension(filePath),
                        FilePath = filePath,
                        FileName = Path.GetFileName(filePath),
                        FileContent = "", // CSV files don't need content since they're already files
                        IsSuccess = true,
                        ErrorMessage = "File is already in CSV format",
                        RowCount = 0 // Could get actual count if needed
                    }
                };
            }

            return new List<ExportResultViewModel>();
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
