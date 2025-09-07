using Sql2Csv.Core.Models;

namespace Sql2Csv.Core.Interfaces;

/// <summary>
/// Interface for managing persisted database files
/// </summary>
public interface IPersistedFileService
{
    /// <summary>
    /// Gets all persisted files
    /// </summary>
    Task<List<PersistedDatabaseFile>> GetPersistedFilesAsync();

    /// <summary>
    /// Gets all available files for selection
    /// </summary>
    Task<List<PersistedDatabaseFile>> GetAvailableFilesAsync();

    /// <summary>
    /// Gets a specific persisted file by ID
    /// </summary>
    Task<PersistedDatabaseFile?> GetPersistedFileAsync(string fileId);

    /// <summary>
    /// Saves an uploaded file as a persisted file
    /// </summary>
    Task<PersistedDatabaseFile> SavePersistedFileAsync(IUploadedFileInfo file, string tempFilePath, int tableCount, string? description = null);

    /// <summary>
    /// Deletes a persisted file
    /// </summary>
    Task<bool> DeletePersistedFileAsync(string fileId);

    /// <summary>
    /// Updates the description of a persisted file
    /// </summary>
    Task<bool> UpdateFileDescriptionAsync(string fileId, string? description);

    /// <summary>
    /// Updates the last accessed timestamp
    /// </summary>
    Task UpdateLastAccessedAsync(string fileId);

    /// <summary>
    /// Cleans up old files based on age
    /// </summary>
    Task CleanupOldFilesAsync(TimeSpan maxAge);

    /// <summary>
    /// Gets the total storage size of all persisted files
    /// </summary>
    Task<long> GetTotalStorageSizeAsync();
}

/// <summary>
/// Interface for database analysis operations
/// </summary>
public interface IDatabaseAnalysisService
{
    /// <summary>
    /// Validates an uploaded database file
    /// </summary>
    Task<FileValidationResult> ValidateDatabaseFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes a database and returns comprehensive information
    /// </summary>
    Task<DatabaseAnalysisResult> AnalyzeDatabaseAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports specified tables to CSV format
    /// </summary>
    Task<List<ExportResult>> ExportTablesToCsvAsync(string filePath, List<string> tableNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates code for specified tables
    /// </summary>
    Task<List<GeneratedCodeResult>> GenerateCodeAsync(string filePath, List<string> tableNames, string namespaceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes a specific table in detail
    /// </summary>
    Task<TableAnalysisResult> AnalyzeTableAsync(string filePath, string tableName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated table data for display
    /// </summary>
    Task<TableDataResult> GetTableDataAsync(string filePath, string tableName, DataTablesRequest request, CancellationToken cancellationToken = default);
}
