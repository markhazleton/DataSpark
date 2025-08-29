using Microsoft.Extensions.Logging;
using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Models;

namespace Sql2Csv.Core.Services;

/// <summary>
/// Unified service for analyzing both database tables and CSV files.
/// </summary>
public class UnifiedAnalysisService : IUnifiedAnalysisService
{
    private readonly ISchemaService _schemaService;
    private readonly ICsvAnalysisService _csvAnalysisService;
    private readonly ILogger<UnifiedAnalysisService> _logger;

    public UnifiedAnalysisService(
        ISchemaService schemaService,
        ICsvAnalysisService csvAnalysisService,
        ILogger<UnifiedAnalysisService> logger)
    {
        _schemaService = schemaService;
        _csvAnalysisService = csvAnalysisService;
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
                : dataSource.Name
        };

        try
        {
            if (dataSource.Type == DataSourceType.Database)
            {
                await AnalyzeDatabaseTableAsync(dataSource, result, cancellationToken);
            }
            else if (dataSource.Type == DataSourceType.Csv)
            {
                await AnalyzeCsvFileAsync(dataSource, result, cancellationToken);
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
                await GetDatabaseDataAsync(dataSource, result, skip, take, cancellationToken);
            }
            else if (dataSource.Type == DataSourceType.Csv)
            {
                await GetCsvDataAsync(dataSource, result, skip, take, cancellationToken);
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
        var columns = await _schemaService.GetTableColumnsAsync(dataSource.ConnectionString, dataSource.TableName, cancellationToken);
        
        result.ColumnCount = columns.Count();
        result.ColumnAnalyses = columns.Select((col, index) => new ColumnAnalysis
        {
            ColumnName = col.Name,
            ColumnIndex = index,
            DataType = col.DataType,
            IsNullable = col.IsNullable,
            IsPrimaryKey = col.IsPrimaryKey
        }).ToList();

        // TODO: Add row count and statistics calculation for database tables
        // This would require executing SQL queries to get counts and sample data
        result.RowCount = 0; // Placeholder - implement actual row counting
    }

    private async Task AnalyzeCsvFileAsync(DataSourceConfiguration dataSource, UnifiedAnalysisResult result, CancellationToken cancellationToken)
    {
        var csvAnalysis = await _csvAnalysisService.AnalyzeCsvAsync(dataSource.FilePath, cancellationToken);
        
        result.RowCount = csvAnalysis.RowCount;
        result.ColumnCount = csvAnalysis.ColumnCount;
        result.Errors.AddRange(csvAnalysis.Errors);

        result.ColumnAnalyses = csvAnalysis.ColumnAnalyses.Select(col => new ColumnAnalysis
        {
            ColumnName = col.ColumnName,
            ColumnIndex = col.ColumnIndex,
            DataType = col.DataType,
            IsNullable = col.NullCount > 0,
            IsPrimaryKey = false,
            NonNullCount = col.NonNullCount,
            NullCount = col.NullCount,
            UniqueCount = col.UniqueCount,
            MinValue = col.MinValue,
            MaxValue = col.MaxValue,
            Mean = col.Mean,
            StandardDeviation = col.StandardDeviation,
            SampleValues = col.SampleValues
        }).ToList();
    }

    private async Task GetDatabaseDataAsync(DataSourceConfiguration dataSource, UnifiedDataResult result, int skip, int take, CancellationToken cancellationToken)
    {
        // TODO: Implement database data retrieval with pagination
        // This would require executing SQL SELECT queries with LIMIT/OFFSET
        
        if (string.IsNullOrEmpty(dataSource.ConnectionString) || string.IsNullOrEmpty(dataSource.TableName))
        {
            throw new ArgumentException("Database data source must have ConnectionString and TableName");
        }

        // Get column names
        var columns = await _schemaService.GetTableColumnsAsync(dataSource.ConnectionString, dataSource.TableName, cancellationToken);
        result.Columns = columns.Select(c => c.Name).ToList();

        // Placeholder implementation - actual database querying would go here
        result.Rows = new List<Dictionary<string, object?>>();
        result.TotalRows = 0;
        result.ReturnedRows = 0;
    }

    private async Task GetCsvDataAsync(DataSourceConfiguration dataSource, UnifiedDataResult result, int skip, int take, CancellationToken cancellationToken)
    {
        var csvData = await _csvAnalysisService.GetCsvDataAsync(dataSource.FilePath, skip, take, cancellationToken);
        
        result.Columns = csvData.Columns;
        result.TotalRows = csvData.TotalRows;
        result.ReturnedRows = csvData.ReturnedRows;

        // Convert CSV rows to dictionary format
        result.Rows = csvData.Rows.Select(row => 
        {
            var dict = new Dictionary<string, object?>();
            for (int i = 0; i < Math.Min(row.Count, result.Columns.Count); i++)
            {
                dict[result.Columns[i]] = row[i];
            }
            return dict;
        }).ToList();
    }
}
