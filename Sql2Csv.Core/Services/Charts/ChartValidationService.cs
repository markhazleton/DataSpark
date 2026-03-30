using Microsoft.Extensions.Logging;
using Sql2Csv.Core.Models.Analysis;
using Sql2Csv.Core.Models.Charts;

namespace Sql2Csv.Core.Services.Charts;

/// <summary>Reusable validation logic for chart configurations.</summary>
public class ChartValidationService : IChartValidationService
{
    private readonly IChartDataService _dataService;
    private readonly ILogger<ChartValidationService> _logger;

    public ChartValidationService(IChartDataService dataService, ILogger<ChartValidationService> logger)
    { _dataService = dataService; _logger = logger; }

    public async Task<ValidationResult> ValidateConfigurationAsync(ChartConfiguration config, string? dataSource = null)
    {
        var result = new ValidationResult();
        try
        {
            var basic = config.Validate();
            result.Errors.AddRange(basic.Errors);
            result.Warnings.AddRange(basic.Warnings);
            if (!string.IsNullOrWhiteSpace(dataSource))
            {
                if (!await _dataService.ValidateDataSourceAsync(dataSource).ConfigureAwait(false))
                { result.Errors.Add($"CSV '{dataSource}' not found or empty."); return result; }
                var columns = await _dataService.GetColumnsAsync(dataSource).ConfigureAwait(false);
                foreach (var s in config.Series) result.Errors.AddRange(await ValidateSeriesAsync(s, columns).ConfigureAwait(false));
                if (config.XAxis != null) result.Errors.AddRange(await ValidateAxisAsync(config.XAxis, columns).ConfigureAwait(false));
                if (config.YAxis != null) result.Errors.AddRange(await ValidateAxisAsync(config.YAxis, columns).ConfigureAwait(false));
                if (config.Y2Axis != null) result.Errors.AddRange(await ValidateAxisAsync(config.Y2Axis, columns).ConfigureAwait(false));
                result.Errors.AddRange(await ValidateFiltersAsync(config.Filters, columns).ConfigureAwait(false));
                if (!await IsChartTypeCompatibleAsync(config.ChartType, columns).ConfigureAwait(false))
                    result.Warnings.Add($"Chart type '{config.ChartType}' may not be optimal for the data.");
            }
            ValidateChartSpecificRules(config, result);
            AddPerformanceWarnings(config, result);
        }
        catch (Exception ex)
        { _logger.LogError(ex, "Validation failure for chart {Name}", config.Name); result.Errors.Add("Unexpected validation error."); }
        return result;
    }

    public Task<List<string>> ValidateSeriesAsync(ChartSeries series, List<ColumnInfo> columns)
    {
        var errors = new List<string>();
        errors.AddRange(series.Validate());
        if (string.IsNullOrWhiteSpace(series.DataColumn)) errors.Add($"Series '{series.Name}' requires a data column.");
        else
        {
            var col = columns.FirstOrDefault(c => c.Column.Equals(series.DataColumn, StringComparison.OrdinalIgnoreCase));
            if (col == null) errors.Add($"Series '{series.Name}': column '{series.DataColumn}' not found.");
            else if (!col.IsNumeric && !IsCountAggregation(series.AggregationFunction))
                errors.Add($"Series '{series.Name}': non-numeric column requires Count aggregation.");
        }
        if (!string.IsNullOrWhiteSpace(series.Color) && !IsValidColor(series.Color)) errors.Add($"Series '{series.Name}': invalid color '{series.Color}'.");
        if (series.MarkerSize is < 1 or > 50) errors.Add($"Series '{series.Name}': marker size out of range.");
        if (series.LineWidth is < 1 or > 20) errors.Add($"Series '{series.Name}': line width out of range.");
        return Task.FromResult(errors);
    }

    public Task<List<string>> ValidateAxisAsync(ChartAxis axis, List<ColumnInfo> columns)
    {
        var errors = new List<string>();
        errors.AddRange(axis.Validate());
        if (!string.IsNullOrWhiteSpace(axis.DataColumn))
        {
            var col = columns.FirstOrDefault(c => c.Column.Equals(axis.DataColumn, StringComparison.OrdinalIgnoreCase));
            if (col == null) errors.Add($"{axis.AxisType}-Axis: column '{axis.DataColumn}' not found");
            else
            {
                if (axis.ScaleType == "DateTime" && !IsDateTimeColumn(col)) errors.Add($"{axis.AxisType}-Axis: DateTime scale requires date column");
                if (axis.IsLogarithmic && !col.IsNumeric) errors.Add($"{axis.AxisType}-Axis: Log scale requires numeric column");
            }
        }
        return Task.FromResult(errors);
    }

    public Task<List<string>> ValidateFiltersAsync(List<ChartFilter> filters, List<ColumnInfo> columns)
    {
        var errors = new List<string>();
        foreach (var f in filters)
        {
            errors.AddRange(f.Validate().Select(e => $"Filter '{f.Column}': {e}"));
            if (!columns.Any(c => c.Column.Equals(f.Column, StringComparison.OrdinalIgnoreCase)))
                errors.Add($"Filter: column '{f.Column}' not found");
        }
        return Task.FromResult(errors);
    }

    public Task<bool> IsChartTypeCompatibleAsync(string chartType, List<ColumnInfo> columns)
    {
        var info = ChartTypes.GetInfo(chartType); if (info == null) return Task.FromResult(false);
        var numeric = columns.Count(c => c.IsNumeric); var dateCols = columns.Any(IsDateTimeColumn);
        var compatible = chartType switch
        {
            "Pie" or "Doughnut" => numeric >= 1 && columns.Count >= 2,
            "Scatter" or "Bubble" => numeric >= 2,
            "Line" or "Area" or "Spline" or "SplineArea" => dateCols || columns.Any(c => c.IsCategory),
            _ => numeric >= 1
        };
        return Task.FromResult(compatible);
    }

    private void ValidateChartSpecificRules(ChartConfiguration config, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(config.ChartType)) { result.Errors.Add("Chart type required"); return; }
        if (ChartTypes.GetInfo(config.ChartType) == null) { result.Errors.Add($"Unknown chart type: {config.ChartType}"); return; }
        if ((config.ChartType == "Pie" || config.ChartType == "Doughnut") && config.Series.Count > 1)
            result.Warnings.Add("Pie/Doughnut best with single series");
        if (config.ChartType.Contains("Stacked") && config.Series.Count == 1)
            result.Warnings.Add("Stacked charts more meaningful with multiple series");
        if (config.ChartStyle == "3D") result.Warnings.Add("3D may reduce readability");
        if (config.Width > 1500 || config.Height > 1000) result.Warnings.Add("Large dimensions may impact performance");
        if (config.IsAnimated && config.AnimationDuration > 5000) result.Warnings.Add("Long animation duration");
    }

    private void AddPerformanceWarnings(ChartConfiguration config, ValidationResult result)
    {
        if (config.Series.Count > 10) result.Warnings.Add($"{config.Series.Count} series may reduce readability");
        if (config.Filters.Count > 5) result.Warnings.Add("Many filters may impact performance");
        if (config.Filters.Count(f => f.IsRegex) > 2) result.Warnings.Add("Multiple regex filters may slow processing");
    }

    private bool IsCountAggregation(string agg) => agg.Equals(AggregationFunctions.Count, StringComparison.OrdinalIgnoreCase);
    private bool IsValidColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color)) return true;
        if (color.StartsWith("#")) { var h = color[1..]; return h.Length == 6 && h.All(c => "0123456789ABCDEFabcdef".Contains(c)); }
        var named = new[] { "red","green","blue","yellow","orange","purple","pink","brown","black","white","gray","grey","silver","gold","cyan","magenta" };
        return named.Contains(color.ToLowerInvariant());
    }
    private bool IsDateTimeColumn(ColumnInfo c) => c.Type.Contains("DateTime") || c.Type.Contains("Date") || c.Column.Contains("date", StringComparison.OrdinalIgnoreCase) || c.Column.Contains("time", StringComparison.OrdinalIgnoreCase);
}
