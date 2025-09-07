using Microsoft.Extensions.Options;
using Sql2Csv.Core.Configuration;
using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Models;
using Sql2Csv.Web.Models;

namespace Sql2Csv.Web.Services;

/// <summary>
/// Web-specific file storage options
/// </summary>
public class WebFileStorageOptions : IFileStorageOptions
{
    private readonly string _persistedDirectory;

    public WebFileStorageOptions(IConfiguration configuration)
    {
        _persistedDirectory = configuration["FileUpload:PersistedDirectory"]
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Sql2Csv.Web", "PersistedDatabases");

        var maxAgeDays = configuration.GetValue("FileUpload:MaxAgeDays", 30);
        MaxFileAge = TimeSpan.FromDays(maxAgeDays);

        var maxSizeMB = configuration.GetValue("FileUpload:MaxStorageSizeMB", 1024);
        MaxStorageSizeBytes = maxSizeMB * 1024L * 1024L;
    }

    public string PersistedDirectory => _persistedDirectory;
    public TimeSpan MaxFileAge { get; }
    public long MaxStorageSizeBytes { get; }
}

/// <summary>
/// Web service that orchestrates core services and handles web-specific concerns
/// </summary>
public interface IWebDatabaseService
{
    Task<(bool Success, string? ErrorMessage, string? FilePath, int TableCount)> SaveUploadedFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<DatabaseAnalysisViewModel> AnalyzeDatabaseAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<ExportResultViewModel>> ExportTablesToCsvAsync(string filePath, List<string> tableNames, CancellationToken cancellationToken = default);
    Task<List<GeneratedCodeViewModel>> GenerateCodeAsync(string filePath, List<string> tableNames, string namespaceName, CancellationToken cancellationToken = default);
    Task<TableAnalysisViewModel> AnalyzeTableAsync(string filePath, string tableName, CancellationToken cancellationToken = default);
    Task<TableDataResult> GetTableDataAsync(string filePath, string tableName, DataTablesRequest request, CancellationToken cancellationToken = default);
    void CleanupTempFiles();
}

/// <summary>
/// Implementation of web database service using core services
/// </summary>
public class WebDatabaseService : IWebDatabaseService
{
    private readonly IDatabaseAnalysisService _databaseAnalysisService;
    private readonly IPersistedFileService _persistedFileService;
    private readonly ILogger<WebDatabaseService> _logger;
    private readonly string _tempDirectory;
    private readonly HashSet<string> _tempFiles = [];

    public WebDatabaseService(
        IDatabaseAnalysisService databaseAnalysisService,
        IPersistedFileService persistedFileService,
        ILogger<WebDatabaseService> logger)
    {
        _databaseAnalysisService = databaseAnalysisService ?? throw new ArgumentNullException(nameof(databaseAnalysisService));
        _persistedFileService = persistedFileService ?? throw new ArgumentNullException(nameof(persistedFileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _tempDirectory = Path.Combine(Path.GetTempPath(), "Sql2Csv.Web");
        Directory.CreateDirectory(_tempDirectory);
    }

    public async Task<(bool Success, string? ErrorMessage, string? FilePath, int TableCount)> SaveUploadedFileAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                return (false, "No file selected", null, 0);
            }

            // Validate file extension
            var allowedExtensions = new[] { ".db", ".sqlite", ".sqlite3" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return (false, "Invalid file type. Only SQLite database files (.db, .sqlite, .sqlite3) are allowed.", null, 0);
            }

            // Validate file size (max 50MB)
            if (file.Length > 50 * 1024 * 1024)
            {
                return (false, "File size too large. Maximum size is 50MB.", null, 0);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(_tempDirectory, fileName);

            // Save file to disk
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await file.CopyToAsync(fileStream, cancellationToken);
            }

            // Track temp file for cleanup
            _tempFiles.Add(filePath);

            // Validate using core service
            var validationResult = await _databaseAnalysisService.ValidateDatabaseFileAsync(filePath, cancellationToken);

            if (!validationResult.Success)
            {
                // Cleanup invalid file
                File.Delete(filePath);
                _tempFiles.Remove(filePath);
                return (false, validationResult.ErrorMessage, null, 0);
            }

            _logger.LogInformation("Successfully uploaded and validated database file: {FileName} with {TableCount} tables", file.FileName, validationResult.TableCount);
            return (true, null, filePath, validationResult.TableCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
            return (false, $"Error uploading file: {ex.Message}", null, 0);
        }
    }

    public async Task<DatabaseAnalysisViewModel> AnalyzeDatabaseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var coreResult = await _databaseAnalysisService.AnalyzeDatabaseAsync(filePath, cancellationToken);
        return DatabaseAnalysisViewModel.FromCore(coreResult);
    }

    public async Task<List<ExportResultViewModel>> ExportTablesToCsvAsync(string filePath, List<string> tableNames, CancellationToken cancellationToken = default)
    {
        var coreResults = await _databaseAnalysisService.ExportTablesToCsvAsync(filePath, tableNames, cancellationToken);
        return coreResults.Select(ExportResultViewModel.FromCore).ToList();
    }

    public async Task<List<GeneratedCodeViewModel>> GenerateCodeAsync(string filePath, List<string> tableNames, string namespaceName, CancellationToken cancellationToken = default)
    {
        var coreResults = await _databaseAnalysisService.GenerateCodeAsync(filePath, tableNames, namespaceName, cancellationToken);
        return coreResults.Select(GeneratedCodeViewModel.FromCore).ToList();
    }

    public async Task<TableAnalysisViewModel> AnalyzeTableAsync(string filePath, string tableName, CancellationToken cancellationToken = default)
    {
        var coreResult = await _databaseAnalysisService.AnalyzeTableAsync(filePath, tableName, cancellationToken);
        return TableAnalysisViewModel.FromCore(coreResult);
    }

    public async Task<TableDataResult> GetTableDataAsync(string filePath, string tableName, DataTablesRequest request, CancellationToken cancellationToken = default)
    {
        return await _databaseAnalysisService.GetTableDataAsync(filePath, tableName, request, cancellationToken);
    }

    public void CleanupTempFiles()
    {
        foreach (var filePath in _tempFiles.ToList())
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                _tempFiles.Remove(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup temp file: {FilePath}", filePath);
            }
        }
    }
}
