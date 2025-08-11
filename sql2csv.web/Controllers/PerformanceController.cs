using Microsoft.AspNetCore.Mvc;
using Sql2Csv.Web.Services;

namespace Sql2Csv.Web.Controllers;

public sealed class PerformanceController : Controller
{
    private readonly IPerformanceMetricsService _metrics;
    private readonly ILogger<PerformanceController> _logger;

    public PerformanceController(IPerformanceMetricsService metrics, ILogger<PerformanceController> logger)
    {
        _metrics = metrics;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _metrics.GetMetricsAsync(cancellationToken);
        ViewData["Title"] = "Performance";
        return View(model);
    }

    [HttpGet("api/performance/metrics")]
    public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken)
    {
        var model = await _metrics.GetMetricsAsync(cancellationToken);
        return Json(new { coverage = model.Coverage?.Percent, hasBenchmarks = model.BenchmarkHtml != null });
    }
}
