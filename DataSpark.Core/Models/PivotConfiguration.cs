namespace DataSpark.Core.Models;

/// <summary>
/// Persisted pivot table configuration for interactive analysis.
/// </summary>
public class PivotConfiguration
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataSource { get; set; } = string.Empty;
    public List<string> RowFields { get; set; } = new();
    public List<string> ColumnFields { get; set; } = new();
    public List<string> ValueFields { get; set; } = new();
    public string AggregationFunction { get; set; } = PivotAggregationFunction.Sum;
    public string RendererType { get; set; } = PivotRendererType.Table;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public static class PivotAggregationFunction
{
    public const string Sum = "Sum";
    public const string Count = "Count";
    public const string Average = "Average";
    public const string Min = "Min";
    public const string Max = "Max";
}

public static class PivotRendererType
{
    public const string Table = "Table";
    public const string Heatmap = "Heatmap";
    public const string BarChart = "Bar Chart";
    public const string LineChart = "Line Chart";
}
