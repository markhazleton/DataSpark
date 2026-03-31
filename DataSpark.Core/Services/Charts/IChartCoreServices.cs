using DataSpark.Core.Models.Analysis; // For ColumnInfo
using DataSpark.Core.Models.Charts;

namespace DataSpark.Core.Services.Charts;

/// <summary>Interface for chart data processing (moved from web layer).</summary>
public interface IChartDataService
{
    Task<ProcessedChartData> ProcessDataAsync(string dataSource, ChartConfiguration config);
    Task<List<ColumnInfo>> GetColumnsAsync(string dataSource);
    Task<List<string>> GetColumnValuesAsync(string dataSource, string column, int maxValues = 1000);
    Task<bool> ValidateDataSourceAsync(string dataSource);
    Task<List<string>> GetAvailableDataSourcesAsync();
    Task<Dictionary<string, List<string>>> GetMultipleColumnValuesAsync(string dataSource, List<string> columns, int maxValues = 100);
    Task<DataSummary> GetDataSummaryAsync(string dataSource);
}

/// <summary>Interface for validation logic.</summary>
public interface IChartValidationService
{
    Task<ValidationResult> ValidateConfigurationAsync(ChartConfiguration config, string? dataSource = null);
    Task<List<string>> ValidateSeriesAsync(ChartSeries series, List<ColumnInfo> columns);
    Task<List<string>> ValidateAxisAsync(ChartAxis axis, List<ColumnInfo> columns);
    Task<List<string>> ValidateFiltersAsync(List<ChartFilter> filters, List<ColumnInfo> columns);
    Task<bool> IsChartTypeCompatibleAsync(string chartType, List<ColumnInfo> columns);
}
