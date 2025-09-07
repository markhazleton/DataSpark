using Sql2Csv.Core.Models.Charts;
using Sql2Csv.Core.Models.Analysis;

namespace DataSpark.Web.Services.Chart;

/// <summary>
/// Presentation-layer chart rendering service (transforms processed data + configuration into client artifacts).
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
