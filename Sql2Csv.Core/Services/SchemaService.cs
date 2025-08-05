using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sql2Csv.Core.Configuration;
using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Models;
using System.Text;

namespace Sql2Csv.Core.Services;

/// <summary>
/// Service for database schema operations.
/// </summary>
public sealed class SchemaService : ISchemaService
{
    private readonly ILogger<SchemaService> _logger;
    private readonly Sql2CsvOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The application options.</param>
    public SchemaService(ILogger<SchemaService> logger, IOptions<Sql2CsvOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TableInfo>> GetTablesAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _logger.LogDebug("Retrieving table information from database");

        try
        {
            var tables = new List<TableInfo>();

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var tableNames = await GetTableNamesAsync(connectionString, cancellationToken);

            foreach (var tableName in tableNames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var columns = await GetTableColumnsAsync(connectionString, tableName, cancellationToken);
                var rowCount = await GetTableRowCountAsync(connection, tableName, cancellationToken);

                var tableInfo = new TableInfo
                {
                    Name = tableName,
                    Columns = columns.ToList(),
                    RowCount = rowCount
                };

                tables.Add(tableInfo);
            }

            _logger.LogDebug("Retrieved information for {TableCount} tables", tables.Count);
            return tables;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving table information");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetTableNamesAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _logger.LogDebug("Retrieving table names from database");

        try
        {
            var tableNames = new List<string>();

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";

            using var command = new SqliteCommand(query, connection);
            command.CommandTimeout = _options.Database.Timeout;

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var tableName = reader.GetString(0);
                tableNames.Add(tableName);
            }

            _logger.LogDebug("Found {TableCount} tables", tableNames.Count);
            return tableNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving table names");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ColumnInfo>> GetTableColumnsAsync(string connectionString, string tableName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        try
        {
            var columns = new List<ColumnInfo>();

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqliteCommand($"PRAGMA table_info([{tableName}])", connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var column = new ColumnInfo
                {
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    DataType = reader.GetString(reader.GetOrdinal("type")),
                    IsNullable = !reader.GetBoolean(reader.GetOrdinal("notnull")),
                    IsPrimaryKey = reader.GetBoolean(reader.GetOrdinal("pk")),
                    DefaultValue = reader.IsDBNull(reader.GetOrdinal("dflt_value")) ? null : reader.GetString(reader.GetOrdinal("dflt_value"))
                };

                columns.Add(column);
            }

            return columns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving columns for table {TableName}", tableName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> GenerateSchemaReportAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _logger.LogDebug("Generating schema report");

        try
        {
            var report = new StringBuilder();
            var tables = await GetTablesAsync(connectionString, cancellationToken);

            foreach (var table in tables)
            {
                cancellationToken.ThrowIfCancellationRequested();

                report.AppendLine($"Table: {table.Name} ({table.RowCount} rows)");
                report.AppendLine(new string('-', 50));

                foreach (var column in table.Columns)
                {
                    var nullable = column.IsNullable ? "NULL" : "NOT NULL";
                    var primaryKey = column.IsPrimaryKey ? " PRIMARY KEY" : "";
                    var defaultValue = !string.IsNullOrEmpty(column.DefaultValue) ? $" DEFAULT {column.DefaultValue}" : "";

                    report.AppendLine($"  {column.Name} ({column.DataType}) {nullable}{primaryKey}{defaultValue}");
                }

                report.AppendLine();
            }

            var schemaReport = report.ToString();
            _logger.LogDebug("Generated schema report with {TableCount} tables", tables.Count());

            return schemaReport;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating schema report");
            throw;
        }
    }

    private static async Task<long> GetTableRowCountAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken)
    {
        using var command = new SqliteCommand($"SELECT COUNT(*) FROM [{tableName}]", connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }
}
