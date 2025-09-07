using Sql2Csv.Core.Models.Analysis;
using Sql2Csv.Core.Models.Charts;
using Microsoft.Extensions.Logging;

namespace Sql2Csv.Core.Services.Charts;

/// <summary>Reusable chart data processing implementation (extracted from web layer).</summary>
public class ChartDataService : IChartDataService
{
    private readonly CsvFileService _csvFileService;
    private readonly ILogger<ChartDataService> _logger;

    public ChartDataService(CsvFileService csvFileService, ILogger<ChartDataService> logger)
    { _csvFileService = csvFileService; _logger = logger; }

    public async Task<ProcessedChartData> ProcessDataAsync(string dataSource, ChartConfiguration config)
    {
        try
        {
            _logger.LogInformation("Processing chart data for source {DataSource} ({ChartType})", dataSource, config.ChartType);
            var result = new ProcessedChartData { ProcessedDate = DateTime.UtcNow, ProcessingNotes = $"Processed for chart type: {config.ChartType}" };
            var csvResult = await _csvFileService.ReadCsvForVisualizationAsync(dataSource);
            if (csvResult == null || csvResult.Records.Count == 0)
            { result.Warnings.Add($"No data in '{dataSource}'."); return result; }
            result.TotalRows = csvResult.Records.Count;
            var columns = await GetColumnsAsync(dataSource);
            foreach (var column in columns) result.ColumnTypes[column.Column] = DetermineDataType(column);
            var dataDict = ConvertDynamicToDict(csvResult.Records);
            var filteredData = ApplyFilters(dataDict, config.Filters);
            foreach (var series in config.Series.Where(s => s.IsVisible).OrderBy(s => s.DisplayOrder))
            {
                try
                {
                    var seriesData = ProcessSeries(filteredData, series, config, columns);
                    result.DataPoints.AddRange(seriesData);
                    if (!result.SeriesNames.Contains(series.Name)) result.SeriesNames.Add(series.Name);
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Series processing failed: {Series}", series.Name); result.Warnings.Add($"Series '{series.Name}' failed: {ex.Message}"); }
            }
            if (config.XAxis?.DataColumn is { } xCol && !string.IsNullOrWhiteSpace(xCol))
                result.Categories = ExtractCategories(filteredData, xCol, config.XAxis.SelectedValues);
            return result;
        }
        catch (Exception ex)
        { _logger.LogError(ex, "Chart data processing error for {DataSource}", dataSource); throw; }
    }

    public async Task<List<ColumnInfo>> GetColumnsAsync(string dataSource)
    {
        try
        {
            var frameResult = await _csvFileService.ReadCsvAsDataFrameAsync(dataSource);
            if (!frameResult.Success || !frameResult.Data.Any())
            {
                var headers = await _csvFileService.GetCsvHeadersAsync(dataSource);
                return headers.Success && headers.Data != null ? headers.Data.Select(h => new ColumnInfo { Column = h, Type = "String", IsNumeric = false }).ToList() : new();
            }
            var df = frameResult.Data[0];
            var list = new List<ColumnInfo>();
            foreach (var col in df.Columns)
            {
                var info = new ColumnInfo { Column = col.Name };
                var t = col.DataType;
                if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)) { info.Type = "Integer"; info.IsNumeric = true; }
                else if (t == typeof(float) || t == typeof(double) || t == typeof(decimal)) { info.Type = "Decimal"; info.IsNumeric = true; }
                else if (t == typeof(DateTime)) { info.Type = "DateTime"; }
                else if (t == typeof(bool)) { info.Type = "Boolean"; }
                else { info.Type = "String"; info.IsNumeric = false; }
                list.Add(info);
            }
            return list;
        }
        catch (Exception ex) { _logger.LogError(ex, "Error getting columns for {DS}", dataSource); return new(); }
    }

    public async Task<List<string>> GetColumnValuesAsync(string dataSource, string column, int maxValues = 1000)
    {
        try
        {
            var csvResult = await _csvFileService.ReadCsvForVisualizationAsync(dataSource);
            if (csvResult == null || csvResult.Records.Count == 0) return new();
            var dict = ConvertDynamicToDict(csvResult.Records);
            return dict.Select(r => r.TryGetValue(column, out var v) ? v?.ToString() ?? "" : "")
                       .Where(v => !string.IsNullOrWhiteSpace(v))
                       .Distinct().OrderBy(v => v).Take(maxValues).ToList();
        }
        catch (Exception ex) { _logger.LogError(ex, "Column values error {Col} {DS}", column, dataSource); return new(); }
    }

    public async Task<bool> ValidateDataSourceAsync(string dataSource)
    { try { return (await GetColumnsAsync(dataSource)).Count > 0; } catch { return false; } }

    public async Task<List<string>> GetAvailableDataSourcesAsync()
        => await Task.FromResult(_csvFileService.GetCsvFileNames());

    public async Task<Dictionary<string, List<string>>> GetMultipleColumnValuesAsync(string dataSource, List<string> columns, int maxValues = 100)
    {
        var dict = new Dictionary<string, List<string>>();
        foreach (var c in columns) dict[c] = await GetColumnValuesAsync(dataSource, c, maxValues);
        return dict;
    }

    public async Task<DataSummary> GetDataSummaryAsync(string dataSource)
    {
        try
        {
            var csv = await _csvFileService.ReadCsvForVisualizationAsync(dataSource);
            var cols = await GetColumnsAsync(dataSource);
            return new DataSummary { TotalDataPoints = csv?.Records?.Count ?? 0, CategoryCount = cols.Count, SeriesCount = cols.Count(c => c.IsNumeric) };
        }
        catch (Exception ex) { _logger.LogError(ex, "Summary error {DS}", dataSource); return new(); }
    }

    private List<Dictionary<string, object?>> ConvertDynamicToDict(List<dynamic> records)
    {
        var list = new List<Dictionary<string, object?>>();
        foreach (var r in records)
        {
            var d = new Dictionary<string, object?>();
            if (r is IDictionary<string, object> e) foreach (var kv in e) d[kv.Key] = kv.Value;
            else foreach (var p in r.GetType().GetProperties()) d[p.Name] = p.GetValue(r);
            list.Add(d);
        }
        return list;
    }

    private List<Dictionary<string, object?>> ApplyFilters(List<Dictionary<string, object?>> data, List<ChartFilter> filters)
    {
        if (filters == null || !filters.Any(f => f.IsEnabled)) return data;
        var output = new List<Dictionary<string, object?>>();
        foreach (var row in data)
        {
            var ok = true;
            foreach (var f in filters.Where(f => f.IsEnabled))
            {
                var value = row.TryGetValue(f.Column, out var v) ? v?.ToString() : null;
                if (!PassesFilter(f, value)) { ok = false; break; }
            }
            if (ok) output.Add(row);
        }
        return output;
    }

    private bool PassesFilter(ChartFilter filter, string? value)
    {
        if (!filter.IsEnabled) return true; if (value == null) return filter.FilterType == "Exclude";
        var compare = filter.IsCaseSensitive ? value : value.ToLowerInvariant();
        return filter.FilterType switch
        {
            "Include" => filter.IncludedValues.Any(v => (filter.IsCaseSensitive ? v : v.ToLowerInvariant()) == compare),
            "Exclude" => !filter.ExcludedValues.Any(v => (filter.IsCaseSensitive ? v : v.ToLowerInvariant()) == compare),
            "Range" => PassesRangeFilter(filter, value),
            "Pattern" => PassesPatternFilter(filter, value),
            _ => true
        };
    }

    private bool PassesRangeFilter(ChartFilter f, string value)
    {
        if (double.TryParse(value, out var num))
        {
            var minOk = string.IsNullOrWhiteSpace(f.MinValue) || double.TryParse(f.MinValue, out var min) && num >= min;
            var maxOk = string.IsNullOrWhiteSpace(f.MaxValue) || double.TryParse(f.MaxValue, out var max) && num <= max;
            if (minOk && maxOk) return true;
        }
        if (DateTime.TryParse(value, out var dt))
        {
            var minOk = string.IsNullOrWhiteSpace(f.MinValue) || DateTime.TryParse(f.MinValue, out var minDt) && dt >= minDt;
            var maxOk = string.IsNullOrWhiteSpace(f.MaxValue) || DateTime.TryParse(f.MaxValue, out var maxDt) && dt <= maxDt;
            if (minOk && maxOk) return true;
        }
        var cmp = f.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var minStr = string.IsNullOrWhiteSpace(f.MinValue) || string.Compare(value, f.MinValue, cmp) >= 0;
        var maxStr = string.IsNullOrWhiteSpace(f.MaxValue) || string.Compare(value, f.MaxValue, cmp) <= 0;
        return minStr && maxStr;
    }

    private bool PassesPatternFilter(ChartFilter f, string value)
    {
        if (string.IsNullOrWhiteSpace(f.Pattern)) return true;
        if (f.IsRegex)
        {
            try { var opts = f.IsCaseSensitive ? System.Text.RegularExpressions.RegexOptions.None : System.Text.RegularExpressions.RegexOptions.IgnoreCase; return System.Text.RegularExpressions.Regex.IsMatch(value, f.Pattern, opts); } catch { return false; }
        }
        var compareValue = f.IsCaseSensitive ? value : value.ToLowerInvariant();
        var pattern = f.IsCaseSensitive ? f.Pattern : f.Pattern.ToLowerInvariant();
        return compareValue.Contains(pattern);
    }

    private List<ChartDataPoint> ProcessSeries(List<Dictionary<string, object?>> data, ChartSeries series, ChartConfiguration config, List<ColumnInfo> columns)
    {
        var points = new List<ChartDataPoint>(); if (string.IsNullOrWhiteSpace(series.DataColumn)) return points;
        var col = columns.FirstOrDefault(c => c.Column == series.DataColumn); if (col == null) return points;
        if (config.XAxis?.DataColumn is { } xCol && !string.IsNullOrWhiteSpace(xCol))
        {
            var groups = data.GroupBy(r => r.TryGetValue(xCol, out var v) ? v?.ToString() ?? "" : "");
            foreach (var g in groups)
            {
                var values = ExtractNumericValues(g, series.DataColumn);
                if (values.Any())
                {
                    var agg = ApplyAggregation(values, series.AggregationFunction);
                    points.Add(new ChartDataPoint { Label = g.Key, Category = g.Key, Value = agg, SeriesName = series.Name, Color = series.Color, Tooltip = $"{series.Name}: {agg:N2}" });
                }
            }
        }
        else
        {
            var values = ExtractNumericValues(data, series.DataColumn);
            if (values.Any())
            {
                var agg = ApplyAggregation(values, series.AggregationFunction);
                points.Add(new ChartDataPoint { Label = series.Name, Category = "Total", Value = agg, SeriesName = series.Name, Color = series.Color, Tooltip = $"{series.Name}: {agg:N2}" });
            }
        }
        return points;
    }

    private List<double> ExtractNumericValues(IEnumerable<Dictionary<string, object?>> data, string column)
    {
        var list = new List<double>();
        foreach (var row in data)
            if (row.TryGetValue(column, out var v) && v != null && double.TryParse(v.ToString(), out var d)) list.Add(d);
        return list;
    }

    private double ApplyAggregation(List<double> values, string func)
    {
        if (!values.Any()) return 0;
        return func switch
        {
            AggregationFunctions.Sum => values.Sum(),
            AggregationFunctions.Average => values.Average(),
            AggregationFunctions.Count => values.Count,
            AggregationFunctions.Min => values.Min(),
            AggregationFunctions.Max => values.Max(),
            AggregationFunctions.Median => CalculateMedian(values),
            AggregationFunctions.StandardDeviation => CalculateStd(values),
            _ => values.Sum()
        };
    }

    private double CalculateMedian(List<double> values)
    { var s = values.OrderBy(v => v).ToList(); return s.Count % 2 == 0 ? (s[s.Count / 2 - 1] + s[s.Count / 2]) / 2.0 : s[s.Count / 2]; }
    private double CalculateStd(List<double> values)
    { if (values.Count <= 1) return 0; var mean = values.Average(); var varc = values.Select(v => Math.Pow(v - mean, 2)).Average(); return Math.Sqrt(varc); }

    private List<string> ExtractCategories(List<Dictionary<string, object?>> data, string column, List<string> selected)
    {
        var cats = data.Select(r => r.TryGetValue(column, out var v) ? v?.ToString() ?? "" : "")
                       .Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().OrderBy(v => v).ToList();
        if (selected != null && selected.Any()) cats = cats.Where(c => selected.Contains(c)).ToList();
        return cats;
    }

    private DataType DetermineDataType(ColumnInfo c)
    {
        if (c.IsNumeric) return c.Type.Contains("Int") ? DataType.Integer : DataType.Decimal;
        if (c.Type.Contains("DateTime") || c.Type.Contains("Date")) return DataType.DateTime;
        if (c.Type.Contains("Boolean")) return DataType.Boolean;
        if (c.IsCategory) return DataType.Category;
        return DataType.String;
    }
}
