namespace DataSpark.Core.Models;

/// <summary>
/// Descriptive statistics for a column.
/// </summary>
public sealed class ColumnStatistics
{
    public double? Mean { get; set; }

    public double? Median { get; set; }

    public string? Mode { get; set; }

    public double? StandardDeviation { get; set; }

    public double? Variance { get; set; }

    public double? Minimum { get; set; }

    public double? Maximum { get; set; }

    public double? FirstQuartile { get; set; }

    public double? ThirdQuartile { get; set; }

    public double? InterquartileRange { get; set; }

    public double? Skewness { get; set; }

    public double? Kurtosis { get; set; }
}
