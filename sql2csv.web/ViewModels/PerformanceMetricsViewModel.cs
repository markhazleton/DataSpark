namespace Sql2Csv.Web.ViewModels;

public sealed class PerformanceMetricsViewModel
{
    public CoverageMetrics? Coverage { get; set; }
    public string? BenchmarkMarkdown { get; set; }
    public string? BenchmarkHtml { get; set; }
}
