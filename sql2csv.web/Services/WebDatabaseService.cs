using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Models;
using Sql2Csv.Web.Models;
using Microsoft.Data.Sqlite;

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
    Task<TableAnalysisViewModel> AnalyzeTableAsync(string filePath, string tableName, CancellationToken cancellationToken = default);
    Task<TableDataResult> GetTableDataAsync(string filePath, string tableName, DataTablesRequest request, CancellationToken cancellationToken = default);
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

                schemaReport = await _schemaService.GenerateSchemaReportAsync(connectionString, format: "text", cancellationToken: reportCts.Token);
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

    public async Task<TableAnalysisViewModel> AnalyzeTableAsync(string filePath, string tableName, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting table analysis for table: {TableName} in file: {FilePath}", tableName, filePath);

            // Validate file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Database file not found: {filePath}");
            }

            var connectionString = $"Data Source={filePath};Mode=ReadOnly;Cache=Shared;";
            var databaseName = Path.GetFileNameWithoutExtension(filePath);

            // Get table schema first
            var columns = await _schemaService.GetTableColumnsAsync(connectionString, tableName, cancellationToken);
            var columnsList = columns.ToList();

            if (!columnsList.Any())
            {
                return new TableAnalysisViewModel
                {
                    DatabaseName = databaseName,
                    TableName = tableName,
                    FilePath = filePath,
                    ErrorMessage = "Table not found or has no columns",
                    IsSuccess = false,
                    AnalysisDuration = stopwatch.Elapsed
                };
            }

            // Analyze table statistics
            var tableStats = await AnalyzeTableStatisticsAsync(connectionString, tableName, columnsList, cancellationToken);

            // Analyze each column
            var columnAnalyses = new List<ColumnAnalysisViewModel>();
            foreach (var column in columnsList)
            {
                var columnAnalysis = await AnalyzeColumnAsync(connectionString, tableName, column, tableStats.TotalRows, cancellationToken);
                columnAnalyses.Add(columnAnalysis);
            }

            stopwatch.Stop();
            _logger.LogInformation("Table analysis completed for {TableName} in {Duration}ms", tableName, stopwatch.ElapsedMilliseconds);

            return new TableAnalysisViewModel
            {
                DatabaseName = databaseName,
                TableName = tableName,
                FilePath = filePath,
                Statistics = tableStats,
                ColumnAnalyses = columnAnalyses,
                AnalysisDuration = stopwatch.Elapsed,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing table: {TableName} in file: {FilePath}", tableName, filePath);

            return new TableAnalysisViewModel
            {
                DatabaseName = Path.GetFileNameWithoutExtension(filePath),
                TableName = tableName,
                FilePath = filePath,
                ErrorMessage = ex.Message,
                IsSuccess = false,
                AnalysisDuration = stopwatch.Elapsed
            };
        }
    }

    public async Task<TableDataResult> GetTableDataAsync(string filePath, string tableName, DataTablesRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file exists
            if (!File.Exists(filePath))
            {
                return new TableDataResult { Error = "Database file not found" };
            }

            var connectionString = $"Data Source={filePath};Mode=ReadOnly;Cache=Shared;";

            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Get total count
            var countSql = $"SELECT COUNT(*) FROM [{tableName}]";
            var totalRecords = await ExecuteScalarAsync<long>(connection, countSql, cancellationToken);

            // Build the main query with search and pagination
            var columns = await GetTableColumnsAsync(connection, tableName, cancellationToken);
            var columnNames = columns.Select(c => $"[{c}]").ToList();

            var selectClause = string.Join(", ", columnNames);
            var whereClause = BuildWhereClause(request.SearchValue, columns);
            var orderClause = BuildOrderClause(request.Order, columns);

            var baseSql = $"SELECT {selectClause} FROM [{tableName}]";
            if (!string.IsNullOrEmpty(whereClause))
            {
                baseSql += $" WHERE {whereClause}";
            }

            // Get filtered count
            long filteredRecords = totalRecords;
            if (!string.IsNullOrEmpty(whereClause))
            {
                var filteredCountSql = $"SELECT COUNT(*) FROM [{tableName}] WHERE {whereClause}";
                filteredRecords = await ExecuteScalarAsync<long>(connection, filteredCountSql, cancellationToken);
            }

            // Add ordering and pagination
            var finalSql = baseSql;
            if (!string.IsNullOrEmpty(orderClause))
            {
                finalSql += $" ORDER BY {orderClause}";
            }
            finalSql += $" LIMIT {request.Length} OFFSET {request.Start}";

            // Execute main query
            var data = new List<Dictionary<string, object?>>();
            using var command = new SqliteCommand(finalSql, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columns[i]] = value;
                }
                data.Add(row);
            }

            return new TableDataResult
            {
                Draw = request.Draw,
                RecordsTotal = totalRecords,
                RecordsFiltered = filteredRecords,
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting table data for {TableName}", tableName);
            return new TableDataResult { Error = ex.Message };
        }
    }

    private async Task<TableStatisticsViewModel> AnalyzeTableStatisticsAsync(string connectionString, string tableName, List<ColumnInfo> columns, CancellationToken cancellationToken)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Get row count
        var rowCountSql = $"SELECT COUNT(*) FROM [{tableName}]";
        var totalRows = await ExecuteScalarAsync<long>(connection, rowCountSql, cancellationToken);

        // Calculate column statistics
        var numericColumns = columns.Count(c => IsNumericType(c.DataType));
        var textColumns = columns.Count(c => IsTextType(c.DataType));
        var dateTimeColumns = columns.Count(c => IsDateTimeType(c.DataType));
        var nullableColumns = columns.Count(c => c.IsNullable);
        var primaryKeyColumns = columns.Count(c => c.IsPrimaryKey);

        // Estimate table size (rough calculation)
        var estimatedSizeBytes = totalRows * columns.Count * 20; // Very rough estimate

        // Calculate data quality score (basic implementation)
        var dataQualityScore = CalculateDataQualityScore(columns, nullableColumns, primaryKeyColumns);

        return new TableStatisticsViewModel
        {
            TotalRows = totalRows,
            TotalColumns = columns.Count,
            NumericColumns = numericColumns,
            TextColumns = textColumns,
            DateTimeColumns = dateTimeColumns,
            NullableColumns = nullableColumns,
            PrimaryKeyColumns = primaryKeyColumns,
            DataQualityScore = dataQualityScore,
            EstimatedSizeBytes = estimatedSizeBytes
        };
    }

    private async Task<ColumnAnalysisViewModel> AnalyzeColumnAsync(string connectionString, string tableName, ColumnInfo column, long totalRows, CancellationToken cancellationToken)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Basic statistics
        var nullCountSql = $"SELECT COUNT(*) FROM [{tableName}] WHERE [{column.Name}] IS NULL";
        var nullCount = await ExecuteScalarAsync<long>(connection, nullCountSql, cancellationToken);

        var uniqueCountSql = $"SELECT COUNT(DISTINCT [{column.Name}]) FROM [{tableName}] WHERE [{column.Name}] IS NOT NULL";
        var uniqueCount = await ExecuteScalarAsync<long>(connection, uniqueCountSql, cancellationToken);

        // Get top values
        var topValuesSql = $@"
            SELECT [{column.Name}], COUNT(*) as count
            FROM [{tableName}] 
            WHERE [{column.Name}] IS NOT NULL
            GROUP BY [{column.Name}]
            ORDER BY count DESC
            LIMIT 10";

        var topValues = new List<ValueFrequency>();
        using var topValuesCommand = new SqliteCommand(topValuesSql, connection);
        using var topValuesReader = await topValuesCommand.ExecuteReaderAsync(cancellationToken);

        while (await topValuesReader.ReadAsync(cancellationToken))
        {
            var value = topValuesReader.GetValue(0)?.ToString() ?? "";
            var count = topValuesReader.GetInt64(1);
            var percentage = totalRows > 0 ? Math.Round((double)count / totalRows * 100, 2) : 0;

            topValues.Add(new ValueFrequency
            {
                Value = value,
                Count = count,
                Percentage = percentage
            });
        }

        // Initialize with basic data
        var dataQualityScore = CalculateColumnQualityScore(totalRows, nullCount, uniqueCount);

        var analysis = new ColumnAnalysisViewModel
        {
            ColumnName = column.Name,
            DataType = column.DataType,
            IsNullable = column.IsNullable,
            IsPrimaryKey = column.IsPrimaryKey,
            DefaultValue = column.DefaultValue,
            TotalCount = totalRows,
            NullCount = nullCount,
            UniqueCount = uniqueCount,
            TopValues = topValues,
            DataQualityScore = dataQualityScore
        };

        // Add type-specific analysis
        if (IsNumericType(column.DataType))
        {
            analysis = await AddNumericAnalysisAsync(connection, tableName, column.Name, analysis, cancellationToken);
        }
        else if (IsTextType(column.DataType))
        {
            analysis = await AddTextAnalysisAsync(connection, tableName, column.Name, analysis, cancellationToken);
        }
        else if (IsDateTimeType(column.DataType))
        {
            analysis = await AddDateTimeAnalysisAsync(connection, tableName, column.Name, analysis, cancellationToken);
        }

        return analysis;
    }

    private async Task<ColumnAnalysisViewModel> AddNumericAnalysisAsync(SqliteConnection connection, string tableName, string columnName, ColumnAnalysisViewModel analysis, CancellationToken cancellationToken)
    {
        var numericStatsSql = $@"
            SELECT 
                MIN(CAST([{columnName}] AS REAL)) as min_val,
                MAX(CAST([{columnName}] AS REAL)) as max_val,
                AVG(CAST([{columnName}] AS REAL)) as mean_val
            FROM [{tableName}] 
            WHERE [{columnName}] IS NOT NULL AND [{columnName}] != ''";

        using var command = new SqliteCommand(numericStatsSql, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        double? minValue = null, maxValue = null, meanValue = null;
        if (await reader.ReadAsync(cancellationToken))
        {
            minValue = reader.IsDBNull(0) ? null : reader.GetDouble(0);
            maxValue = reader.IsDBNull(1) ? null : reader.GetDouble(1);
            meanValue = reader.IsDBNull(2) ? null : reader.GetDouble(2);
        }

        // Calculate median (approximate for large datasets)
        double? medianValue = null;
        var medianSql = $@"
            SELECT CAST([{columnName}] AS REAL) as val
            FROM [{tableName}] 
            WHERE [{columnName}] IS NOT NULL AND [{columnName}] != ''
            ORDER BY CAST([{columnName}] AS REAL)
            LIMIT 1 OFFSET (
                SELECT COUNT(*) FROM [{tableName}] 
                WHERE [{columnName}] IS NOT NULL AND [{columnName}] != ''
            ) / 2";

        try
        {
            medianValue = await ExecuteScalarAsync<double?>(connection, medianSql, cancellationToken);
        }
        catch
        {
            // Median calculation failed, skip it
        }

        return analysis with
        {
            MinValue = minValue,
            MaxValue = maxValue,
            MeanValue = meanValue,
            MedianValue = medianValue
        };
    }

    private async Task<ColumnAnalysisViewModel> AddTextAnalysisAsync(SqliteConnection connection, string tableName, string columnName, ColumnAnalysisViewModel analysis, CancellationToken cancellationToken)
    {
        var textStatsSql = $@"
            SELECT 
                MIN(LENGTH([{columnName}])) as min_len,
                MAX(LENGTH([{columnName}])) as max_len,
                AVG(LENGTH([{columnName}])) as avg_len
            FROM [{tableName}] 
            WHERE [{columnName}] IS NOT NULL";

        using var command = new SqliteCommand(textStatsSql, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        int? minLength = null, maxLength = null;
        double? averageLength = null;

        if (await reader.ReadAsync(cancellationToken))
        {
            minLength = reader.IsDBNull(0) ? null : reader.GetInt32(0);
            maxLength = reader.IsDBNull(1) ? null : reader.GetInt32(1);
            averageLength = reader.IsDBNull(2) ? null : reader.GetDouble(2);
        }

        return analysis with
        {
            MinLength = minLength,
            MaxLength = maxLength,
            AverageLength = averageLength
        };
    }

    private async Task<ColumnAnalysisViewModel> AddDateTimeAnalysisAsync(SqliteConnection connection, string tableName, string columnName, ColumnAnalysisViewModel analysis, CancellationToken cancellationToken)
    {
        var dateStatsSql = $@"
            SELECT 
                MIN([{columnName}]) as min_date,
                MAX([{columnName}]) as max_date
            FROM [{tableName}] 
            WHERE [{columnName}] IS NOT NULL";

        using var command = new SqliteCommand(dateStatsSql, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        DateTime? minDate = null, maxDate = null;

        if (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0))
            {
                var minVal = reader.GetValue(0);
                if (DateTime.TryParse(minVal?.ToString(), out var parsedMin))
                    minDate = parsedMin;
            }

            if (!reader.IsDBNull(1))
            {
                var maxVal = reader.GetValue(1);
                if (DateTime.TryParse(maxVal?.ToString(), out var parsedMax))
                    maxDate = parsedMax;
            }
        }

        return analysis with
        {
            MinDate = minDate,
            MaxDate = maxDate
        };
    }

    private async Task<List<string>> GetTableColumnsAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken)
    {
        var columns = new List<string>();
        var sql = $"PRAGMA table_info([{tableName}])";

        using var command = new SqliteCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(1)); // Column name is at index 1
        }

        return columns;
    }

    private static string BuildWhereClause(string? searchValue, List<string> columns)
    {
        if (string.IsNullOrEmpty(searchValue) || !columns.Any())
            return "";

        var conditions = columns.Select(col => $"[{col}] LIKE '%{searchValue.Replace("'", "''")}%'");
        return $"({string.Join(" OR ", conditions)})";
    }

    private static string BuildOrderClause(List<DataTablesOrder> orders, List<string> columns)
    {
        if (!orders.Any() || !columns.Any())
            return "[rowid]"; // Default ordering

        var orderClauses = orders
            .Where(o => o.Column >= 0 && o.Column < columns.Count)
            .Select(o => $"[{columns[o.Column]}] {(o.Dir.ToLower() == "desc" ? "DESC" : "ASC")}");

        return orderClauses.Any() ? string.Join(", ", orderClauses) : "[rowid]";
    }

    private static async Task<T> ExecuteScalarAsync<T>(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        using var command = new SqliteCommand(sql, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result == null || result == DBNull.Value)
            return default(T)!;

        return (T)Convert.ChangeType(result, typeof(T));
    }

    private static bool IsNumericType(string dataType) => dataType.ToUpperInvariant() switch
    {
        "INTEGER" or "REAL" or "NUMERIC" or "DECIMAL" or "FLOAT" or "DOUBLE" => true,
        _ => false
    };

    private static bool IsTextType(string dataType) => dataType.ToUpperInvariant() switch
    {
        "TEXT" or "VARCHAR" or "CHAR" or "STRING" => true,
        _ => false
    };

    private static bool IsDateTimeType(string dataType) => dataType.ToUpperInvariant() switch
    {
        "DATETIME" or "DATE" or "TIME" or "TIMESTAMP" => true,
        _ => false
    };

    private static double CalculateDataQualityScore(List<ColumnInfo> columns, int nullableColumns, int primaryKeyColumns)
    {
        if (!columns.Any()) return 0;

        var baseScore = 0.5; // Start with 50%
        var hasPrimaryKey = primaryKeyColumns > 0 ? 0.2 : 0; // +20% for having primary key
        var nullableRatio = (double)nullableColumns / columns.Count;
        var nullablePenalty = nullableRatio * 0.3; // Up to -30% for many nullable columns

        return Math.Max(0, Math.Min(1, baseScore + hasPrimaryKey - nullablePenalty));
    }

    private static double CalculateColumnQualityScore(long totalRows, long nullCount, long uniqueCount)
    {
        if (totalRows == 0) return 0;

        var completeness = 1.0 - ((double)nullCount / totalRows);
        var uniqueness = Math.Min(1.0, (double)uniqueCount / totalRows);

        // Weight completeness more heavily than uniqueness
        return (completeness * 0.7) + (uniqueness * 0.3);
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
