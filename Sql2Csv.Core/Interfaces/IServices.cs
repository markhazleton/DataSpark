using Sql2Csv.Core.Models;

namespace Sql2Csv.Core.Interfaces;

/// <summary>
/// Service for discovering database files.
/// </summary>
public interface IDatabaseDiscoveryService
{
    /// <summary>
    /// Discovers SQLite database files in the specified directory.
    /// </summary>
    /// <param name="directoryPath">The directory path to search.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of discovered database configurations.</returns>
    Task<IEnumerable<DatabaseConfiguration>> DiscoverDatabasesAsync(string directoryPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for exporting database data to CSV.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports all tables from a database to CSV files.
    /// </summary>
    /// <param name="databaseConfig">The database configuration.</param>
    /// <param name="outputDirectory">The output directory.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of export results.</returns>
    Task<IEnumerable<ExportResult>> ExportDatabaseToCsvAsync(DatabaseConfiguration databaseConfig, string outputDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a specific table from a database to a CSV file.
    /// </summary>
    /// <param name="databaseConfig">The database configuration.</param>
    /// <param name="tableName">The table name.</param>
    /// <param name="outputFilePath">The output file path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The export result.</returns>
    Task<ExportResult> ExportTableToCsvAsync(DatabaseConfiguration databaseConfig, string tableName, string outputFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports all tables from a database to CSV files with optional runtime overrides.
    /// </summary>
    /// <param name="databaseConfig">The database configuration.</param>
    /// <param name="outputDirectory">The output directory.</param>
    /// <param name="delimiter">Optional delimiter override; if null uses configured option.</param>
    /// <param name="includeHeaders">Optional include headers override; if null uses configured option.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IEnumerable<ExportResult>> ExportDatabaseToCsvAsync(DatabaseConfiguration databaseConfig, string outputDirectory, string? delimiter, bool? includeHeaders, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a specific table with optional runtime overrides.
    /// </summary>
    /// <param name="databaseConfig">The database configuration.</param>
    /// <param name="tableName">The table name.</param>
    /// <param name="outputFilePath">The output file path.</param>
    /// <param name="delimiter">Optional delimiter override.</param>
    /// <param name="includeHeaders">Optional include headers override.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<ExportResult> ExportTableToCsvAsync(DatabaseConfiguration databaseConfig, string tableName, string outputFilePath, string? delimiter, bool? includeHeaders, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for database schema operations.
/// </summary>
public interface ISchemaService
{
    /// <summary>
    /// Gets all table information from a database.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of table information.</returns>
    Task<IEnumerable<TableInfo>> GetTablesAsync(string connectionString, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all table names from a database.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of table names.</returns>
    Task<IEnumerable<string>> GetTableNamesAsync(string connectionString, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets column information for a specific table.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="tableName">The table name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of column information.</returns>
    Task<IEnumerable<ColumnInfo>> GetTableColumnsAsync(string connectionString, string tableName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a schema report for a database.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="format">Output format: text (default), json, or markdown.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A schema report in the requested format.</returns>
    Task<string> GenerateSchemaReportAsync(string connectionString, string format = "text", CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for generating code from database schema.
/// </summary>
public interface ICodeGenerationService
{
    /// <summary>
    /// Generates DTO classes for all tables in a database.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="outputDirectory">The output directory.</param>
    /// <param name="namespaceName">The namespace for generated classes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task GenerateDtoClassesAsync(string connectionString, string outputDirectory, string namespaceName, CancellationToken cancellationToken = default);
}
