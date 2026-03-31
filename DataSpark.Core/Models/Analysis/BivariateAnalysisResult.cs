namespace DataSpark.Core.Models.Analysis;

/// <summary>
/// Result payload for bivariate analysis operations.
/// </summary>
public sealed class BivariateAnalysisResult
{
    public string FileName { get; set; } = string.Empty;

    public string Column1 { get; set; } = string.Empty;

    public string Column2 { get; set; } = string.Empty;

    public string Col1Type { get; set; } = string.Empty;

    public string Col2Type { get; set; } = string.Empty;

    public double? Correlation { get; set; }

    public RegressionResult? Regression { get; set; }

    public List<double[]>? Scatter { get; set; }

    public Dictionary<string, Dictionary<string, int>>? ContingencyTable { get; set; }

    public Dictionary<string, GroupStatsResult>? GroupStats { get; set; }
}

/// <summary>
/// Simple linear regression output.
/// </summary>
public sealed class RegressionResult
{
    public double Intercept { get; init; }

    public double Slope { get; init; }
}

/// <summary>
/// Group-level statistics for numeric/categorical bivariate analysis.
/// </summary>
public sealed class GroupStatsResult
{
    public int Count { get; init; }

    public double? Min { get; init; }

    public double? Max { get; init; }

    public double? Mean { get; init; }

    public double? Std { get; init; }

    public List<double> Values { get; init; } = [];
}
