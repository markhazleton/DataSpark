using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Models;

namespace Sql2Csv.Core.Services;

/// <summary>
/// Unified service for analyzing database tables.
/// </summary>
public class UnifiedAnalysisService : IUnifiedAnalysisService
{
    private readonly ISchemaService _schemaService;
    private readonly ILogger<UnifiedAnalysisService> _logger;

    public UnifiedAnalysisService(
        ISchemaService schemaService,
        ILogger<UnifiedAnalysisService> logger)
    {
        _schemaService = schemaService;
        _logger = logger;
    }

    public async Task<UnifiedAnalysisResult> AnalyzeDataSourceAsync(DataSourceConfiguration dataSource, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting unified analysis for data source: {Name} ({Type})", dataSource.Name, dataSource.Type);

        var result = new UnifiedAnalysisResult
        {
            DataSource = dataSource,
            DisplayName = dataSource.Type == DataSourceType.Database 
                ? $"{dataSource.Name}.{dataSource.TableName}" 
                : dataSource.Name,
            Capabilities = UnifiedAnalysisCapabilities.None
        };

        try
        {
            if (dataSource.Type == DataSourceType.Database)
            {
                await AnalyzeDatabaseTableAsync(dataSource, result, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new NotSupportedException($"Data source type {dataSource.Type} is not supported");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing data source: {Name}", dataSource.Name);
            result.Errors.Add($"Analysis error: {ex.Message}");
        }

        return result;
    }

    public async Task<UnifiedDataResult> GetDataAsync(DataSourceConfiguration dataSource, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting data from data source: {Name} ({Type}), Skip: {Skip}, Take: {Take}", 
            dataSource.Name, dataSource.Type, skip, take);

        var result = new UnifiedDataResult
        {
            DataSource = dataSource,
            StartIndex = skip
        };

        try
        {
            if (dataSource.Type == DataSourceType.Database)
            {
                await GetDatabaseDataAsync(dataSource, result, skip, take, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new NotSupportedException($"Data source type {dataSource.Type} is not supported");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data from data source: {Name}", dataSource.Name);
            result.Errors.Add($"Data retrieval error: {ex.Message}");
        }

        return result;
    }

    private async Task AnalyzeDatabaseTableAsync(DataSourceConfiguration dataSource, UnifiedAnalysisResult result, CancellationToken cancellationToken)
    {
    if (string.IsNullOrEmpty(dataSource.ConnectionString) || string.IsNullOrEmpty(dataSource.TableName))
        {
            throw new ArgumentException("Database data source must have ConnectionString and TableName");
        }

        // Get table columns
        var columns = await _schemaService.GetTableColumnsAsync(dataSource.ConnectionString, dataSource.TableName, cancellationToken).ConfigureAwait(false);
        
        var columnList = columns.ToList();
        result.ColumnCount = columnList.Count;
        result.ColumnAnalyses = columnList.Select((col, index) => new ColumnAnalysis
        {
            ColumnName = col.Name,
            ColumnIndex = index,
            DataType = col.DataType,
            IsNullable = col.IsNullable,
            IsPrimaryKey = col.IsPrimaryKey
        }).ToList();
        result.Capabilities |= UnifiedAnalysisCapabilities.ColumnCount | UnifiedAnalysisCapabilities.ColumnStatistics;

        // Row count & simple numeric stats
        try
        {
            await using var connection = new SqliteConnection(dataSource.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using (var countCmd = new SqliteCommand($"SELECT COUNT(*) FROM [{dataSource.TableName}]", connection))
            {
                var scalar = await countCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                result.RowCount = Convert.ToInt64(scalar);
                result.Capabilities |= UnifiedAnalysisCapabilities.RowCount;
            }

            // Gather simple numeric stats (mean, min, max) for INTEGER/REAL columns (best effort)
            foreach (var numericCol in result.ColumnAnalyses.Where(c => c.DataType.Equals("INTEGER", StringComparison.OrdinalIgnoreCase) || c.DataType.Equals("REAL", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    var sql = $"SELECT MIN([{numericCol.ColumnName}]), MAX([{numericCol.ColumnName}]), AVG([{numericCol.ColumnName}]) FROM [{dataSource.TableName}]";
                    await using var statsCmd = new SqliteCommand(sql, connection);
                    await using var reader = await statsCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        numericCol.MinValue = reader.IsDBNull(0) ? null : reader.GetValue(0)?.ToString();
                        numericCol.MaxValue = reader.IsDBNull(1) ? null : reader.GetValue(1)?.ToString();
                        var meanObj = reader.IsDBNull(2) ? null : reader.GetValue(2);
                        if (meanObj != null && double.TryParse(meanObj.ToString(), out var mean))
                        {
                            numericCol.Mean = mean;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed numeric stats for {Column}", numericCol.ColumnName);
                }
            }
            result.Capabilities |= UnifiedAnalysisCapabilities.NumericStats;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Row count/statistics retrieval failed for table {Table}", dataSource.TableName);
            result.IsPartial = true;
        }
    }

    private async Task GetDatabaseDataAsync(DataSourceConfiguration dataSource, UnifiedDataResult result, int skip, int take, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(dataSource.ConnectionString) || string.IsNullOrEmpty(dataSource.TableName))
        {
            throw new ArgumentException("Database data source must have ConnectionString and TableName");
        }

        var columns = (await _schemaService.GetTableColumnsAsync(dataSource.ConnectionString, dataSource.TableName, cancellationToken)).ToList();
        result.Columns = columns.Select(c => c.Name).ToList();

        try
        {
            await using var connection = new SqliteConnection(dataSource.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Total rows (cache could be added)
            await using (var countCmd = new SqliteCommand($"SELECT COUNT(*) FROM [{dataSource.TableName}]", connection))
            {
                var scalar = await countCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                result.TotalRows = Convert.ToInt64(scalar);
            }

            var sql = $"SELECT * FROM [{dataSource.TableName}] LIMIT @take OFFSET @skip";
            await using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@take", take);
            cmd.Parameters.AddWithValue("@skip", skip);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var rows = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dict[result.Columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(dict);
            }
            result.Rows = rows;
            result.ReturnedRows = rows.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error paging database table {Table}", dataSource.TableName);
            result.Errors.Add($"Paging error: {ex.Message}");
        }
    }

}
