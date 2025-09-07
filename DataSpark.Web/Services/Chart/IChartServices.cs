using Sql2Csv.Core.Models.Charts;
using Sql2Csv.Core.Models.Analysis;
using DataSpark.Web.Models.Chart; // Retained for purely view-related DTOs
using Sql2Csv.Core.Services.Charts; // Core abstractions

namespace DataSpark.Web.Services.Chart;

/// <summary>
/// Service for chart configuration management
/// </summary>
// IChartService moved to Core (Sql2Csv.Core.Services.Charts)

// NOTE: Web layer now depends directly on core service interfaces (no aliases)

/// <summary>
/// Service for chart rendering and visualization
/// </summary>
public interface IChartRenderingService
{
    Task<ChartRenderResult> RenderChartAsync(ChartConfiguration config, ProcessedChartData data);
    Task<string> GenerateChartJsonAsync(ChartConfiguration config, ProcessedChartData data);
    Task<string> GenerateChartHtmlAsync(ChartConfiguration config, ProcessedChartData data);
    Task<string> GenerateChartScriptAsync(ChartConfiguration config, ProcessedChartData data);
    Task<byte[]> ExportChartAsync(ChartConfiguration config, ProcessedChartData data, string format);
    Task<string> GenerateEmbedCodeAsync(ChartConfiguration config, string baseUrl);
}

// Validation handled by Sql2Csv.Core.Services.Charts.IChartValidationService

/// <summary>
/// Repository interface for chart configuration persistence
/// </summary>
// IChartConfigurationRepository moved to Sql2Csv.Core.Services.Charts

// NOTE: Additional interfaces for templates, export, sharing, caching, and audit were removed
// from the web layer pending future implementation. If reintroduced, consider adding them to
// Core (for domain/application concerns) or a dedicated Infrastructure project.

/// <summary>
/// Audit entry for tracking changes
/// </summary>
// AuditEntry moved to Core (Sql2Csv.Core.Models.Charts)
