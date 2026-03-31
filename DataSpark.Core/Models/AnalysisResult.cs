namespace DataSpark.Core.Models;

/// <summary>
/// Unified analysis result payload supporting EDA, univariate, bivariate, and AI outputs.
/// </summary>
public sealed class AnalysisResult
{
    public string FileName { get; set; } = string.Empty;

    public object? Eda { get; set; }

    public object? Univariate { get; set; }

    public object? Bivariate { get; set; }

    public object? AiInsight { get; set; }

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
