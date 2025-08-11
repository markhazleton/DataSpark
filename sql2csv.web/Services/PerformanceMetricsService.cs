using Markdig;
using System.Text.Json;
using Sql2Csv.Web.ViewModels;

namespace Sql2Csv.Web.Services;

public sealed class PerformanceMetricsService : IPerformanceMetricsService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PerformanceMetricsService> _logger;
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public PerformanceMetricsService(IWebHostEnvironment env, ILogger<PerformanceMetricsService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<PerformanceMetricsViewModel> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var root = _env.ContentRootPath;
        var coverageXml = Path.Combine(root, "data", "coverage.cobertura.xml");
        var badgeJson = Path.Combine(root, "coverage-badge.json");
        var badgeJsonAlt = Path.GetFullPath(Path.Combine(root, "..", "coverage-badge.json"));
        var resultsDir = Path.Combine(root, "BenchmarkDotNet.Artifacts", "results");
        var solutionBenchDir = Path.Combine(root, "..", "Sql2Csv.Benchmarks", "BenchmarkDotNet.Artifacts", "results");
        var baselineFile = Path.Combine(root, "..", "Sql2Csv.Benchmarks", "baseline", "BenchmarkBaseline.md");

        var coverage = new ViewModels.CoverageMetrics { Source = "none" };
        // Try XML first
        if (File.Exists(coverageXml))
        {
            try
            {
                var xml = await File.ReadAllTextAsync(coverageXml, cancellationToken);
                var doc = System.Xml.Linq.XDocument.Parse(xml);
                var cov = doc.Root;
                coverage.Source = "xml";
                coverage.Percent = cov?.Attribute("line-rate")?.Value is string lr ? (Math.Round(double.Parse(lr) * 100, 2) + "%") : null;
                coverage.LinesCovered = int.TryParse(cov?.Attribute("lines-covered")?.Value, out var lc) ? lc : null;
                coverage.LinesTotal = int.TryParse(cov?.Attribute("lines-valid")?.Value, out var lt) ? lt : null;
                coverage.BranchesCovered = int.TryParse(cov?.Attribute("branches-covered")?.Value, out var bc) ? bc : null;
                coverage.BranchesTotal = int.TryParse(cov?.Attribute("branches-valid")?.Value, out var bt) ? bt : null;
                // Optionally parse classes, files, methods, statements if present
                // ...
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed parsing coverage XML");
            }
        }
        // Fallback: JSON badge
        if (coverage.Percent == null)
        {
            string? badgePath = File.Exists(badgeJson) ? badgeJson : (File.Exists(badgeJsonAlt) ? badgeJsonAlt : null);
            if (badgePath != null)
            {
                try
                {
                    using var fs = File.OpenRead(badgePath);
                    var doc = await JsonDocument.ParseAsync(fs, cancellationToken: cancellationToken);
                    coverage.Source = "json";
                    coverage.Percent = doc.RootElement.GetProperty("message").GetString();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed parsing coverage badge JSON");
                }
            }
        }

        string? benchmarkMarkdown = null;
        string? benchmarkHtml = null;

        string? pickDir = Directory.Exists(resultsDir) ? resultsDir : (Directory.Exists(solutionBenchDir) ? solutionBenchDir : null);
        if (pickDir != null)
        {
            try
            {
                var mdFiles = Directory.EnumerateFiles(pickDir, "*.md", SearchOption.TopDirectoryOnly)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .ToList();
                var latest = mdFiles.FirstOrDefault();
                if (latest != null)
                {
                    benchmarkMarkdown = await File.ReadAllTextAsync(latest.FullName, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed loading benchmark markdown");
            }
        }
        if (benchmarkMarkdown == null && File.Exists(baselineFile))
        {
            benchmarkMarkdown = await File.ReadAllTextAsync(baselineFile, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(benchmarkMarkdown))
        {
            benchmarkHtml = Markdig.Markdown.ToHtml(benchmarkMarkdown, _pipeline);
        }

        return new PerformanceMetricsViewModel
        {
            Coverage = coverage,
            BenchmarkMarkdown = benchmarkMarkdown,
            BenchmarkHtml = benchmarkHtml
        };
    }
}
