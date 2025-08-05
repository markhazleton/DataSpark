using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sql2Csv.Core.Configuration;
using Sql2Csv.Core.Interfaces;

namespace Sql2Csv.Application.Services;

/// <summary>
/// Main application service that orchestrates the export operations.
/// </summary>
public sealed class ApplicationService
{
    private readonly ILogger<ApplicationService> _logger;
    private readonly IDatabaseDiscoveryService _discoveryService;
    private readonly IExportService _exportService;
    private readonly ISchemaService _schemaService;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly Sql2CsvOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationService"/> class.
    /// </summary>
    public ApplicationService(
        ILogger<ApplicationService> logger,
        IDatabaseDiscoveryService discoveryService,
        IExportService exportService,
        ISchemaService schemaService,
        ICodeGenerationService codeGenerationService,
        IOptions<Sql2CsvOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _schemaService = schemaService ?? throw new ArgumentNullException(nameof(schemaService));
        _codeGenerationService = codeGenerationService ?? throw new ArgumentNullException(nameof(codeGenerationService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Exports all databases in the specified directory to CSV files.
    /// </summary>
    /// <param name="databasePath">The path containing the databases.</param>
    /// <param name="outputPath">The output path for CSV files.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ExportDatabasesAsync(string databasePath, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting database export operation");
        _logger.LogInformation("Database path: {DatabasePath}", databasePath);
        _logger.LogInformation("Output path: {OutputPath}", outputPath);

        try
        {
            var databases = await _discoveryService.DiscoverDatabasesAsync(databasePath, cancellationToken);

            if (!databases.Any())
            {
                _logger.LogWarning("No databases found in the specified path: {DatabasePath}", databasePath);
                return;
            }

            var totalResults = new List<Core.Models.ExportResult>();

            foreach (var database in databases)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var databaseOutputPath = Path.Combine(outputPath, database.Name);
                _logger.LogInformation("Exporting database: {DatabaseName}", database.Name);

                var results = await _exportService.ExportDatabaseToCsvAsync(database, databaseOutputPath, cancellationToken);
                totalResults.AddRange(results);
            }

            LogExportSummary(totalResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database export operation");
            throw;
        }
    }

    /// <summary>
    /// Generates schema reports for all databases in the specified directory.
    /// </summary>
    /// <param name="databasePath">The path containing the databases.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task GenerateSchemaReportsAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting schema report generation");
        _logger.LogInformation("Database path: {DatabasePath}", databasePath);

        try
        {
            var databases = await _discoveryService.DiscoverDatabasesAsync(databasePath, cancellationToken);

            if (!databases.Any())
            {
                _logger.LogWarning("No databases found in the specified path: {DatabasePath}", databasePath);
                return;
            }

            foreach (var database in databases)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("Generating schema report for database: {DatabaseName}", database.Name);

                var report = await _schemaService.GenerateSchemaReportAsync(database.ConnectionString, cancellationToken);

                Console.WriteLine($"\n=== Schema Report for {database.Name} ===");
                Console.WriteLine(report);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during schema report generation");
            throw;
        }
    }

    /// <summary>
    /// Generates DTO classes for all databases in the specified directory.
    /// </summary>
    /// <param name="databasePath">The path containing the databases.</param>
    /// <param name="outputPath">The output path for generated code.</param>
    /// <param name="namespaceName">The namespace for generated classes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task GenerateCodeAsync(string databasePath, string outputPath, string namespaceName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting code generation");
        _logger.LogInformation("Database path: {DatabasePath}", databasePath);
        _logger.LogInformation("Output path: {OutputPath}", outputPath);
        _logger.LogInformation("Namespace: {Namespace}", namespaceName);

        try
        {
            var databases = await _discoveryService.DiscoverDatabasesAsync(databasePath, cancellationToken);

            if (!databases.Any())
            {
                _logger.LogWarning("No databases found in the specified path: {DatabasePath}", databasePath);
                return;
            }

            foreach (var database in databases)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var databaseOutputPath = Path.Combine(outputPath, database.Name);
                _logger.LogInformation("Generating code for database: {DatabaseName}", database.Name);

                await _codeGenerationService.GenerateDtoClassesAsync(
                    database.ConnectionString,
                    databaseOutputPath,
                    namespaceName,
                    cancellationToken);
            }

            _logger.LogInformation("Code generation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during code generation");
            throw;
        }
    }

    private void LogExportSummary(IEnumerable<Core.Models.ExportResult> results)
    {
        var resultList = results.ToList();
        var successful = resultList.Count(r => r.IsSuccess);
        var failed = resultList.Count(r => !r.IsSuccess);
        var totalRows = resultList.Where(r => r.IsSuccess).Sum(r => r.RowCount);
        var totalDuration = TimeSpan.FromTicks(resultList.Sum(r => r.Duration.Ticks));

        _logger.LogInformation("Export Summary:");
        _logger.LogInformation("  Total tables: {TotalTables}", resultList.Count);
        _logger.LogInformation("  Successful: {Successful}", successful);
        _logger.LogInformation("  Failed: {Failed}", failed);
        _logger.LogInformation("  Total rows exported: {TotalRows:N0}", totalRows);
        _logger.LogInformation("  Total duration: {TotalDuration}", totalDuration);

        if (failed > 0)
        {
            _logger.LogWarning("Failed exports:");
            foreach (var failedResult in resultList.Where(r => !r.IsSuccess))
            {
                _logger.LogWarning("  {DatabaseName}.{TableName}: {ErrorMessage}",
                    failedResult.DatabaseName, failedResult.TableName, failedResult.ErrorMessage);
            }
        }
    }
}
