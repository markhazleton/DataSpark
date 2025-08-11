using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sql2Csv.Core.Configuration;
using Sql2Csv.Core.Models;
using Sql2Csv.Core.Interfaces;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Sql2Csv.Core.Services;

/// <summary>
/// Service for exporting database data to CSV files.
/// </summary>
public sealed class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly ISchemaService _schemaService;
    private readonly Sql2CsvOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="schemaService">The schema service.</param>
    /// <param name="options">The application options.</param>
    public ExportService(
        ILogger<ExportService> logger,
        ISchemaService schemaService,
        IOptions<Sql2CsvOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _schemaService = schemaService ?? throw new ArgumentNullException(nameof(schemaService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExportResult>> ExportDatabaseToCsvAsync(
        DatabaseConfiguration databaseConfig,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        return await ExportDatabaseToCsvAsync(databaseConfig, outputDirectory, null, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExportResult>> ExportDatabaseToCsvAsync(
        DatabaseConfiguration databaseConfig,
        string outputDirectory,
        string? delimiter,
        bool? includeHeaders,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(databaseConfig);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        _logger.LogInformation("Starting export for database: {DatabaseName}", databaseConfig.Name);

        try
        {
            Directory.CreateDirectory(outputDirectory);

            var tableNames = await _schemaService.GetTableNamesAsync(databaseConfig.ConnectionString, cancellationToken);
            var results = new List<ExportResult>();

            foreach (var tableName in tableNames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outputFilePath = Path.Combine(outputDirectory, $"{tableName}_extract.csv");
                var result = await ExportTableToCsvAsync(databaseConfig, tableName, outputFilePath, delimiter, includeHeaders, cancellationToken);
                results.Add(result);
            }

            _logger.LogInformation("Completed export for database: {DatabaseName}", databaseConfig.Name);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting database: {DatabaseName}", databaseConfig.Name);
            throw;
        }
    }

    /// <summary>
    /// Export database with optional table filtering.
    /// </summary>
    public async Task<IEnumerable<ExportResult>> ExportDatabaseToCsvAsync(
        DatabaseConfiguration databaseConfig,
        string outputDirectory,
        IEnumerable<string>? tablesFilter,
        string? delimiter,
        bool? includeHeaders,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(databaseConfig);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        _logger.LogInformation("Starting export for database: {DatabaseName} (filtered)", databaseConfig.Name);

        try
        {
            Directory.CreateDirectory(outputDirectory);

            var allTableNames = await _schemaService.GetTableNamesAsync(databaseConfig.ConnectionString, cancellationToken);
            IEnumerable<string> effectiveTables = allTableNames;
            if (tablesFilter != null)
            {
                var filterSet = new HashSet<string>(tablesFilter, StringComparer.OrdinalIgnoreCase);
                effectiveTables = allTableNames.Where(t => filterSet.Contains(t)).ToList();

                var unknown = filterSet.Except(allTableNames, StringComparer.OrdinalIgnoreCase).ToList();
                if (unknown.Any())
                {
                    _logger.LogWarning("The following requested tables were not found in {DatabaseName}: {Missing}", databaseConfig.Name, string.Join(", ", unknown));
                }
                if (!effectiveTables.Any())
                {
                    _logger.LogWarning("No matching tables after filtering for database {DatabaseName}", databaseConfig.Name);
                    return Enumerable.Empty<ExportResult>();
                }
                _logger.LogInformation("Filtered tables: {Count}/{Total}", effectiveTables.Count(), allTableNames.Count());
            }

            var results = new List<ExportResult>();
            foreach (var tableName in effectiveTables)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var outputFilePath = Path.Combine(outputDirectory, $"{tableName}_extract.csv");
                var result = await ExportTableToCsvAsync(databaseConfig, tableName, outputFilePath, delimiter, includeHeaders, cancellationToken);
                results.Add(result);
            }

            _logger.LogInformation("Completed filtered export for database: {DatabaseName}", databaseConfig.Name);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting database (filtered): {DatabaseName}", databaseConfig.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ExportResult> ExportTableToCsvAsync(
        DatabaseConfiguration databaseConfig,
        string tableName,
        string outputFilePath,
        CancellationToken cancellationToken = default)
    {
        return await ExportTableToCsvAsync(databaseConfig, tableName, outputFilePath, null, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ExportResult> ExportTableToCsvAsync(
        DatabaseConfiguration databaseConfig,
        string tableName,
        string outputFilePath,
        string? delimiter,
        bool? includeHeaders,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(databaseConfig);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFilePath);

        _logger.LogDebug("Exporting table {TableName} to {OutputFile}", tableName, outputFilePath);

        var stopwatch = Stopwatch.StartNew();
        var rowCount = 0;

        try
        {
            var encoding = Encoding.GetEncoding(_options.Export.Encoding);

            // Ensure output directory exists
            var outputDirectory = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            await using var connection = new SqliteConnection(databaseConfig.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            // Use parameterized query to prevent SQL injection
            using var command = new SqliteCommand($"SELECT * FROM [{tableName}]", connection);
            command.CommandTimeout = _options.Database.Timeout;

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await using var fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);
            await using var writer = new StreamWriter(fileStream, encoding);

            // Configure CSV writer with custom delimiter
            var effectiveDelimiter = string.IsNullOrEmpty(delimiter) ? _options.Export.Delimiter : delimiter;
            var effectiveIncludeHeaders = includeHeaders ?? _options.Export.IncludeHeaders;

            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = effectiveDelimiter
            };
            using var csv = new CsvWriter(writer, csvConfig);

            // Write headers if configured
            if (effectiveIncludeHeaders)
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    csv.WriteField(reader.GetName(i));
                }
                await csv.NextRecordAsync();
            }

            // Write data rows
            while (await reader.ReadAsync(cancellationToken))
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i)?.ToString() ?? string.Empty;
                    csv.WriteField(value);
                }
                await csv.NextRecordAsync();
                rowCount++;
            }

            stopwatch.Stop();
            _logger.LogDebug("Successfully exported table {TableName} with {RowCount} rows", tableName, rowCount);

            return new ExportResult
            {
                DatabaseName = databaseConfig.Name,
                TableName = tableName,
                OutputFilePath = outputFilePath,
                RowCount = rowCount,
                Duration = stopwatch.Elapsed,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error exporting table {TableName} to {OutputFile}", tableName, outputFilePath);

            return new ExportResult
            {
                DatabaseName = databaseConfig.Name,
                TableName = tableName,
                OutputFilePath = outputFilePath,
                RowCount = rowCount,
                Duration = stopwatch.Elapsed,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
