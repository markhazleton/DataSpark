using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Models;
using Sql2Csv.Web.Models;

namespace Sql2Csv.Web.Services;

/// <summary>
/// Service for handling database operations in the web application
/// </summary>
public interface IWebDatabaseService
{
    Task<(bool Success, string? ErrorMessage, string? FilePath, int TableCount)> SaveUploadedFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<DatabaseAnalysisViewModel> AnalyzeDatabaseAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<ExportResultViewModel>> ExportTablesToCsvAsync(string filePath, List<string> tableNames, CancellationToken cancellationToken = default);
    Task<List<GeneratedCodeViewModel>> GenerateCodeAsync(string filePath, List<string> tableNames, string namespaceName, CancellationToken cancellationToken = default);
    void CleanupTempFiles();
}

/// <summary>
/// Implementation of the web database service
/// </summary>
public class WebDatabaseService : IWebDatabaseService
{
    private readonly ISchemaService _schemaService;
    private readonly IExportService _exportService;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly IPersistedFileService _persistedFileService;
    private readonly ILogger<WebDatabaseService> _logger;
    private readonly string _tempDirectory;
    private readonly HashSet<string> _tempFiles = [];

    public WebDatabaseService(
        ISchemaService schemaService,
        IExportService exportService,
        ICodeGenerationService codeGenerationService,
        IPersistedFileService persistedFileService,
        ILogger<WebDatabaseService> logger)
    {
        _schemaService = schemaService;
        _exportService = exportService;
        _codeGenerationService = codeGenerationService;
        _persistedFileService = persistedFileService;
        _logger = logger;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "Sql2Csv.Web");

        // Ensure temp directory exists
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

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream, cancellationToken);
            } // Ensure stream is disposed before database validation

            // Track temp file for cleanup
            _tempFiles.Add(filePath);

            // Validate it's a valid SQLite file by trying to open it
            var connectionString = $"Data Source={filePath};Mode=ReadOnly;Cache=Shared;";
            int tableCount = 0;
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(10)); // 10 second timeout for validation

                var tables = await _schemaService.GetTableNamesAsync(connectionString, cts.Token);
                tableCount = tables.Count();
                // If we can get tables, the file is valid
                _logger.LogInformation("Validated SQLite file with {TableCount} tables", tableCount);

                // Force garbage collection to ensure database connections are cleaned up
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (Exception ex)
            {
                // Cleanup invalid file
                File.Delete(filePath);
                _tempFiles.Remove(filePath);
                return (false, $"Invalid SQLite database file: {ex.Message}", null, 0);
            }

            _logger.LogInformation("Successfully uploaded and validated database file: {FileName} with {TableCount} tables", file.FileName, tableCount);
            return (true, null, filePath, tableCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
            return (false, $"Error uploading file: {ex.Message}", null, 0);
        }
    }

    public async Task<DatabaseAnalysisViewModel> AnalyzeDatabaseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting database analysis for file: {FilePath}", filePath);

            // Validate file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Database file not found: {filePath}");
            }

            var connectionString = $"Data Source={filePath};Mode=ReadOnly;Cache=Shared;";
            var databaseName = Path.GetFileNameWithoutExtension(filePath);

            _logger.LogInformation("Getting tables information for database: {DatabaseName}", databaseName);

            // Get all tables with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second timeout

            var tablesInfo = await _schemaService.GetTablesAsync(connectionString, cts.Token);

            _logger.LogInformation("Found {TableCount} tables in database", tablesInfo.Count());

            // Convert to view models
            var tableViewModels = new List<TableInfoViewModel>();
            foreach (var table in tablesInfo)
            {
                var columnViewModels = table.Columns.Select(c => new ColumnInfoViewModel
                {
                    Name = c.Name,
                    DataType = c.DataType,
                    IsNullable = c.IsNullable,
                    IsPrimaryKey = c.IsPrimaryKey,
                    DefaultValue = c.DefaultValue
                }).ToList();

                tableViewModels.Add(new TableInfoViewModel
                {
                    Name = table.Name,
                    Schema = table.Schema,
                    RowCount = table.RowCount,
                    Columns = columnViewModels
                });
            }

            _logger.LogInformation("Generating schema report for database");

            // Generate schema report with timeout
            string? schemaReport = null;
            try
            {
                using var reportCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                reportCts.CancelAfter(TimeSpan.FromSeconds(15)); // 15 second timeout for schema report

                schemaReport = await _schemaService.GenerateSchemaReportAsync(connectionString, reportCts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Schema report generation timed out, continuing without report");
                schemaReport = "Schema report generation timed out.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate schema report, continuing without report");
                schemaReport = $"Failed to generate schema report: {ex.Message}";
            }

            stopwatch.Stop();
            _logger.LogInformation("Database analysis completed in {Duration}ms", stopwatch.ElapsedMilliseconds);

            return new DatabaseAnalysisViewModel
            {
                DatabaseName = databaseName,
                FilePath = filePath,
                Tables = tableViewModels,
                SchemaReport = schemaReport,
                AnalysisDuration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing database: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<List<ExportResultViewModel>> ExportTablesToCsvAsync(string filePath, List<string> tableNames, CancellationToken cancellationToken = default)
    {
        try
        {
            var databaseConfig = DatabaseConfiguration.FromSqliteFile(filePath);
            var results = new List<ExportResultViewModel>();

            foreach (var tableName in tableNames)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    // Create temporary output file
                    var outputFileName = $"{tableName}_{Guid.NewGuid()}.csv";
                    var outputPath = Path.Combine(_tempDirectory, outputFileName);
                    _tempFiles.Add(outputPath);

                    var exportResult = await _exportService.ExportTableToCsvAsync(databaseConfig, tableName, outputPath, cancellationToken);

                    // Read the CSV content
                    var csvContent = await File.ReadAllTextAsync(outputPath, cancellationToken);

                    stopwatch.Stop();

                    results.Add(new ExportResultViewModel
                    {
                        TableName = tableName,
                        FileName = $"{tableName}.csv",
                        FileContent = csvContent,
                        RowCount = exportResult.RowCount,
                        Duration = stopwatch.Elapsed,
                        IsSuccess = exportResult.IsSuccess,
                        ErrorMessage = exportResult.ErrorMessage
                    });
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex, "Error exporting table {TableName}", tableName);

                    results.Add(new ExportResultViewModel
                    {
                        TableName = tableName,
                        FileName = $"{tableName}.csv",
                        FileContent = string.Empty,
                        RowCount = 0,
                        Duration = stopwatch.Elapsed,
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CSV export");
            throw;
        }
    }

    public async Task<List<GeneratedCodeViewModel>> GenerateCodeAsync(string filePath, List<string> tableNames, string namespaceName, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = $"Data Source={filePath}";
            var results = new List<GeneratedCodeViewModel>();

            // For now, we'll generate C# classes
            // In the future, this could be extended to support other languages

            foreach (var tableName in tableNames)
            {
                var columns = await _schemaService.GetTableColumnsAsync(connectionString, tableName, cancellationToken);
                var code = GenerateCSharpClass(tableName, columns, namespaceName);

                results.Add(new GeneratedCodeViewModel
                {
                    TableName = tableName,
                    ClassName = $"{tableName}Model",
                    Code = code,
                    Language = CodeLanguage.CSharp
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating code");
            throw;
        }
    }

    private static string GenerateCSharpClass(string tableName, IEnumerable<ColumnInfo> columns, string namespaceName)
    {
        var className = $"{tableName}Model";
        var code = new System.Text.StringBuilder();

        code.AppendLine($"namespace {namespaceName};");
        code.AppendLine();
        code.AppendLine($"/// <summary>");
        code.AppendLine($"/// Model class for {tableName} table");
        code.AppendLine($"/// </summary>");
        code.AppendLine($"public class {className}");
        code.AppendLine("{");

        foreach (var column in columns)
        {
            var csharpType = MapSqliteTypeToCSharp(column.DataType, column.IsNullable);
            var propertyName = ToPascalCase(column.Name);

            code.AppendLine($"    /// <summary>");
            code.AppendLine($"    /// Gets or sets the {column.Name}");
            code.AppendLine($"    /// </summary>");
            code.AppendLine($"    public {csharpType} {propertyName} {{ get; set; }}");
            code.AppendLine();
        }

        code.AppendLine("}");

        return code.ToString();
    }

    private static string MapSqliteTypeToCSharp(string sqliteType, bool isNullable)
    {
        var baseType = sqliteType.ToUpperInvariant() switch
        {
            "INTEGER" => "long",
            "REAL" => "double",
            "TEXT" => "string",
            "BLOB" => "byte[]",
            "BOOLEAN" => "bool",
            "DATETIME" => "DateTime",
            "DATE" => "DateTime",
            "TIME" => "TimeSpan",
            _ => "string"
        };

        // For value types, add nullable if needed
        if (isNullable && baseType != "string" && baseType != "byte[]")
        {
            return $"{baseType}?";
        }

        return baseType;
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var words = input.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(word =>
            char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
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
