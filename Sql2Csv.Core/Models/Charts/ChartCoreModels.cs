using System.ComponentModel.DataAnnotations; // Intentionally retained for validation attributes
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Sql2Csv.Core.Models.Charts;

/// <summary>
/// Core chart configuration (web/persistence layer may extend via partial classes if needed).
/// </summary>
public class ChartConfiguration
{
    [Key]
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(255)] public string CsvFile { get; set; } = string.Empty;
    [Required, StringLength(50)] public string ChartType { get; set; } = "Column";
    [StringLength(10)] public string ChartStyle { get; set; } = "2D";
    [StringLength(50)] public string ChartPalette { get; set; } = "BrightPastel";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 400;
    [StringLength(200)] public string Title { get; set; } = string.Empty;
    [StringLength(200)] public string SubTitle { get; set; } = string.Empty;
    [StringLength(255)] public string BackgroundImage { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    [StringLength(100)] public string CreatedBy { get; set; } = string.Empty;
    public List<ChartSeries> Series { get; set; } = new();
    public ChartAxis? XAxis { get; set; }
    public ChartAxis? YAxis { get; set; }
    public ChartAxis? Y2Axis { get; set; }
    public List<ChartFilter> Filters { get; set; } = new();
    public bool ShowLegend { get; set; } = true;
    [StringLength(20)] public string LegendPosition { get; set; } = "Right";
    public bool ShowTooltips { get; set; } = true;
    public bool EnableZoom { get; set; } = true;
    public bool EnablePan { get; set; } = true;
    public bool IsAnimated { get; set; } = true;
    public int AnimationDuration { get; set; } = 1000;
    [StringLength(20)] public string Theme { get; set; } = "Default";
    public string? ChartOptions { get; set; }

    public ValidationResult Validate()
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(Name)) result.Errors.Add("Chart name is required");
        if (string.IsNullOrWhiteSpace(CsvFile)) result.Errors.Add("CSV file is required");
        if (Series == null || Series.Count == 0) result.Errors.Add("At least one data series is required");
        if (Width <= 0 || Width > 2000) result.Errors.Add("Width must be between 1 and 2000 pixels");
        if (Height <= 0 || Height > 2000) result.Errors.Add("Height must be between 1 and 2000 pixels");
        return result;
    }

    public ChartConfiguration Clone()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(this);
        var clone = System.Text.Json.JsonSerializer.Deserialize<ChartConfiguration>(json)!;
        clone.Id = 0;
        clone.CreatedDate = DateTime.UtcNow;
        clone.ModifiedDate = DateTime.UtcNow;
        return clone;
    }
}

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ChartAxis
{
    [Key] public int Id { get; set; }
    public int ChartConfigurationId { get; set; }
    [Required, StringLength(10)] public string AxisType { get; set; } = "X"; // X, Y, Y2
    [StringLength(100)] public string DataColumn { get; set; } = string.Empty;
    [StringLength(200)] public string Title { get; set; } = string.Empty;
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public double? Interval { get; set; }
    public bool IsLogarithmic { get; set; }
    public double LogarithmBase { get; set; } = 10;
    [StringLength(20)] public string ScaleType { get; set; } = "Linear"; // Linear, Logarithmic, DateTime
    public string? SelectedValuesJson { get; set; }
    public bool ShowGridLines { get; set; } = true;
    [StringLength(20)] public string GridLineColor { get; set; } = "#E0E0E0";
    [StringLength(20)] public string GridLineStyle { get; set; } = "Solid";
    public int GridLineWidth { get; set; } = 1;
    public bool ShowTickMarks { get; set; } = true;
    [StringLength(20)] public string TickMarkColor { get; set; } = "#808080";
    public int TickMarkLength { get; set; } = 3;
    public bool ShowLabels { get; set; } = true;
    [StringLength(50)] public string LabelFont { get; set; } = "Arial";
    public int LabelFontSize { get; set; } = 10;
    [StringLength(20)] public string LabelColor { get; set; } = "#000000";
    public double LabelAngle { get; set; }
    public bool AutoLabelAngle { get; set; } = true;
    [StringLength(50)] public string TitleFont { get; set; } = "Arial";
    public int TitleFontSize { get; set; } = 12;
    [StringLength(20)] public string TitleColor { get; set; } = "#000000";
    [StringLength(50)] public string DateTimeFormat { get; set; } = "MM/dd/yyyy";
    [StringLength(20)] public string DateTimeIntervalType { get; set; } = "Auto";

    [NotMapped]
    public List<string> SelectedValues
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SelectedValuesJson)) return new();
            try { return JsonSerializer.Deserialize<List<string>>(SelectedValuesJson) ?? new(); } catch { return new(); }
        }
        set => SelectedValuesJson = JsonSerializer.Serialize(value);
    }

    public List<string> Validate()
    {
        var errors = new List<string>();
        var validAxisTypes = new[] { "X", "Y", "Y2" };
        if (!validAxisTypes.Contains(AxisType)) errors.Add($"Invalid axis type. Valid options: {string.Join(", ", validAxisTypes)}");
        if (MinValue.HasValue && MaxValue.HasValue && MinValue >= MaxValue) errors.Add("Minimum value must be less than maximum value");
        if (Interval.HasValue && Interval <= 0) errors.Add("Interval must be greater than zero");
        if (IsLogarithmic && LogarithmBase <= 1) errors.Add("Logarithm base must be greater than 1");
        var validScaleTypes = new[] { "Linear", "Logarithmic", "DateTime" };
        if (!validScaleTypes.Contains(ScaleType)) errors.Add($"Invalid scale type. Valid options: {string.Join(", ", validScaleTypes)}");
        if (LabelFontSize <= 0 || LabelFontSize > 72) errors.Add("Label font size must be between 1 and 72");
        if (TitleFontSize <= 0 || TitleFontSize > 72) errors.Add("Title font size must be between 1 and 72");
        if (Math.Abs(LabelAngle) > 90) errors.Add("Label angle must be between -90 and 90 degrees");
        return errors;
    }
}

public class ChartFilter
{
    [Key] public int Id { get; set; }
    public int ChartConfigurationId { get; set; }
    [Required, StringLength(100)] public string Column { get; set; } = string.Empty;
    [Required, StringLength(20)] public string FilterType { get; set; } = "Include";
    public string? IncludedValuesJson { get; set; }
    public string? ExcludedValuesJson { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    [StringLength(200)] public string? Pattern { get; set; }
    public bool IsCaseSensitive { get; set; }
    public bool IsRegex { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int DisplayOrder { get; set; }

    [NotMapped]
    public List<string> IncludedValues
    {
        get
        {
            if (string.IsNullOrWhiteSpace(IncludedValuesJson)) return new();
            try { return JsonSerializer.Deserialize<List<string>>(IncludedValuesJson) ?? new(); } catch { return new(); }
        }
        set => IncludedValuesJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public List<string> ExcludedValues
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ExcludedValuesJson)) return new();
            try { return JsonSerializer.Deserialize<List<string>>(ExcludedValuesJson) ?? new(); } catch { return new(); }
        }
        set => ExcludedValuesJson = JsonSerializer.Serialize(value);
    }

    public List<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Column)) errors.Add("Filter column is required");
        var validFilterTypes = new[] { "Include", "Exclude", "Range", "Pattern" };
        if (!validFilterTypes.Contains(FilterType)) errors.Add($"Invalid filter type. Valid options: {string.Join(", ", validFilterTypes)}");
        switch (FilterType)
        {
            case "Include": if (IncludedValues.Count == 0) errors.Add("Include filter must have at least one value"); break;
            case "Exclude": if (ExcludedValues.Count == 0) errors.Add("Exclude filter must have at least one value"); break;
            case "Range": if (string.IsNullOrWhiteSpace(MinValue) && string.IsNullOrWhiteSpace(MaxValue)) errors.Add("Range filter must have at least min or max value"); break;
            case "Pattern": if (string.IsNullOrWhiteSpace(Pattern)) errors.Add("Pattern filter must have a pattern"); break;
        }
        return errors;
    }
}

public class ChartSeries
{
    [Key] public int Id { get; set; }
    public int ChartConfigurationId { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(100)] public string DataColumn { get; set; } = string.Empty;
    [Required, StringLength(50)] public string AggregationFunction { get; set; } = "Sum";
    [StringLength(50)] public string SeriesChartType { get; set; } = string.Empty;
    [StringLength(20)] public string Color { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public int DisplayOrder { get; set; }
    [StringLength(20)] public string LineStyle { get; set; } = "Solid";
    public int LineWidth { get; set; } = 2;
    [StringLength(20)] public string MarkerStyle { get; set; } = "None";
    public int MarkerSize { get; set; } = 6;
    public bool ShowDataLabels { get; set; }
    [StringLength(20)] public string DataLabelPosition { get; set; } = "Top";
    [StringLength(10)] public string YAxisType { get; set; } = "Primary";

    public string GetEffectiveChartType(string parentType) => string.IsNullOrWhiteSpace(SeriesChartType) ? parentType : SeriesChartType;

    public List<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name)) errors.Add("Series name is required");
        if (string.IsNullOrWhiteSpace(DataColumn)) errors.Add("Data column is required");
        if (string.IsNullOrWhiteSpace(AggregationFunction)) errors.Add("Aggregation function is required");
        var validAggregations = AggregationFunctions.All;
        if (!validAggregations.Contains(AggregationFunction)) errors.Add($"Invalid aggregation function. Valid options: {string.Join(", ", validAggregations)}");
        if (DisplayOrder < 0) errors.Add("Display order must be non-negative");
        return errors;
    }
}

public class ProcessedChartData
{
    public List<ChartDataPoint> DataPoints { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public List<string> SeriesNames { get; set; } = new();
    public Dictionary<string, DataType> ColumnTypes { get; set; } = new();
    public int TotalRows { get; set; }
    public DateTime ProcessedDate { get; set; } = DateTime.UtcNow;
    public string ProcessingNotes { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
    public List<ChartDataPoint> GetSeriesData(string seriesName) => DataPoints.Where(dp => dp.SeriesName == seriesName).ToList();
    public List<string> GetUniqueCategories() => DataPoints.Select(dp => dp.Category).Distinct().OrderBy(c => c).ToList();
    public DataSummary GetSummary() => new()
    {
        TotalDataPoints = DataPoints.Count,
        SeriesCount = SeriesNames.Count,
        CategoryCount = Categories.Count,
        MinValue = DataPoints.Any() ? DataPoints.Min(dp => dp.Value) : 0,
        MaxValue = DataPoints.Any() ? DataPoints.Max(dp => dp.Value) : 0,
        AverageValue = DataPoints.Any() ? DataPoints.Average(dp => dp.Value) : 0
    };
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string SeriesName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
    public string? Color { get; set; }
    public bool IsHighlighted { get; set; }
    public string? Tooltip { get; set; }
    public T? GetProperty<T>(string key) => Properties.TryGetValue(key, out var value) && value is T typed ? typed : default;
    public void SetProperty(string key, object value) => Properties[key] = value;
}

public class DataSummary
{
    public int TotalDataPoints { get; set; }
    public int SeriesCount { get; set; }
    public int CategoryCount { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double AverageValue { get; set; }
}

public class ChartRenderResult
{
    public bool Success { get; set; }
    public string? ChartHtml { get; set; }
    public string? ChartJson { get; set; }
    public string? ChartScript { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public static ChartRenderResult CreateSuccess(string html, string json, string script) => new() { Success = true, ChartHtml = html, ChartJson = json, ChartScript = script };
    public static ChartRenderResult CreateFailure(params string[] errors) => new() { Success = false, Errors = errors.ToList() };
}

public static class ChartTypes
{
    public static readonly Dictionary<string, ChartTypeInfo> All = new()
    {
        { "Column", new ChartTypeInfo("Column", "Column Chart", "Vertical bars for comparing values", true, false) },
        { "StackedColumn", new ChartTypeInfo("StackedColumn", "Stacked Column", "Stacked vertical bars", true, false) },
        { "StackedColumn100", new ChartTypeInfo("StackedColumn100", "100% Stacked Column", "Stacked bars showing percentages", true, false) },
        { "Bar", new ChartTypeInfo("Bar", "Bar Chart", "Horizontal bars for comparing values", true, false) },
        { "StackedBar", new ChartTypeInfo("StackedBar", "Stacked Bar", "Stacked horizontal bars", true, false) },
        { "StackedBar100", new ChartTypeInfo("StackedBar100", "100% Stacked Bar", "Stacked horizontal bars showing percentages", true, false) },
        { "Line", new ChartTypeInfo("Line", "Line Chart", "Connected data points over time", false, true) },
        { "Spline", new ChartTypeInfo("Spline", "Spline Chart", "Smooth curved line chart", false, true) },
        { "StepLine", new ChartTypeInfo("StepLine", "Step Line", "Step-wise connected points", false, true) },
        { "Area", new ChartTypeInfo("Area", "Area Chart", "Filled area under line", false, true) },
        { "SplineArea", new ChartTypeInfo("SplineArea", "Spline Area", "Smooth curved area chart", false, true) },
        { "StackedArea", new ChartTypeInfo("StackedArea", "Stacked Area", "Stacked filled areas", false, true) },
        { "StackedArea100", new ChartTypeInfo("StackedArea100", "100% Stacked Area", "Stacked areas showing percentages", false, true) },
        { "Pie", new ChartTypeInfo("Pie", "Pie Chart", "Circular chart showing proportions", false, false) },
        { "Doughnut", new ChartTypeInfo("Doughnut", "Doughnut Chart", "Pie chart with hollow center", false, false) },
        { "Point", new ChartTypeInfo("Point", "Point Chart", "Individual data points", false, false) },
        { "Bubble", new ChartTypeInfo("Bubble", "Bubble Chart", "Points with varying sizes", false, false) },
        { "Scatter", new ChartTypeInfo("Scatter", "Scatter Plot", "X-Y coordinate plotting", false, false) },
        { "Radar", new ChartTypeInfo("Radar", "Radar Chart", "Multi-variable circular chart", false, false) },
        { "Polar", new ChartTypeInfo("Polar", "Polar Chart", "Circular coordinate system", false, false) }
    };
    public static ChartTypeInfo? GetInfo(string chartType) => string.IsNullOrWhiteSpace(chartType) ? null : (All.TryGetValue(chartType, out var info) ? info : null);
    public static List<string> GetNames() => All.Keys.ToList();
}

public class ChartTypeInfo
{
    public string Key { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool SupportsStacking { get; set; }
    public bool RequiresContinuousXAxis { get; set; }
    public ChartTypeInfo(string key, string name, string description, bool supportsStacking, bool requiresContinuousXAxis)
    { Key = key; Name = name; Description = description; SupportsStacking = supportsStacking; RequiresContinuousXAxis = requiresContinuousXAxis; }
}

public static class ColorPalettes
{
    public static readonly Dictionary<string, string[]> All = new()
    {
        { "BrightPastel", new[] { "#418CF0", "#FCB441", "#E0400A", "#056492", "#BFBFBF", "#1A3B69", "#FFE382", "#129CDD", "#CA6B4B", "#005CDB" } },
        { "Grayscale", new[] { "#4C4C4C", "#999999", "#333333", "#808080", "#B3B3B3", "#1A1A1A", "#E6E6E6", "#666666", "#CCCCCC", "#000000" } },
        { "Excel", new[] { "#5B9BD5", "#ED7D31", "#A5A5A5", "#FFC000", "#4472C4", "#70AD47", "#FF6600", "#9966CC", "#00B050", "#990000" } },
        { "Fire", new[] { "#FFD700", "#FF6347", "#FF4500", "#DC143C", "#B22222", "#8B0000", "#FFA500", "#FF8C00", "#FF1493", "#800080" } },
        { "Light", new[] { "#E6F3FF", "#FFE6CC", "#FFCCCC", "#E6FFCC", "#CCE6FF", "#F0E6FF", "#FFFFCC", "#FFCCFF", "#CCF0FF", "#F5F5DC" } },
        { "Pastel", new[] { "#FFB3BA", "#FFDFBA", "#FFFFBA", "#BAFFC9", "#BAE1FF", "#C9BAFF", "#FFBAF0", "#F0FFBA", "#BAFFFF", "#FFBADF" } },
        { "SeaGreen", new[] { "#2E8B57", "#3CB371", "#66CDAA", "#98FB98", "#90EE90", "#00FA9A", "#00FF7F", "#7FFF00", "#ADFF2F", "#32CD32" } },
        { "Berry", new[] { "#8B008B", "#9370DB", "#9932CC", "#BA55D3", "#DA70D6", "#EE82EE", "#DDA0DD", "#D8BFD8", "#DDA0DD", "#E6E6FA" } }
    };
    public static string[] GetColors(string palette) => All.TryGetValue(palette, out var colors) ? colors : All["BrightPastel"];
    public static List<string> GetNames() => All.Keys.ToList();
}

public enum DataType { Unknown, String, Integer, Decimal, DateTime, Boolean, Category }

public static class AggregationFunctions
{
    public const string Sum = "Sum";
    public const string Average = "Average";
    public const string Count = "Count";
    public const string Min = "Min";
    public const string Max = "Max";
    public const string Median = "Median";
    public const string StandardDeviation = "StdDev";
    public static readonly string[] All = { Sum, Average, Count, Min, Max, Median, StandardDeviation };
}
