using Microsoft.AspNetCore.Mvc;
using DataSpark.Core.Models.Analysis;
using DataSpark.Core.Models.Charts;
using DataSpark.Core.Services.Charts;
using DataSpark.Core.Services;
using DataSpark.Core.Models;
using DataSpark.Core.Interfaces;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace DataSpark.Web.Controllers.Api;

/// <summary>
/// RESTful API controller for chart operations
/// </summary>
[Route("api/[controller]")]
[Route("api/Chart")]
[ApiController]
public class ChartApiController : ControllerBase
{
    private readonly IChartService _chartService;
    private readonly IChartDataService _dataService;
    private readonly IChartRenderingService _renderingService;
    private readonly IChartValidationService _validationService;
    private readonly ILogger<ChartApiController> _logger;

    public ChartApiController(
    IChartService chartService,
    IChartDataService dataService,
        IChartRenderingService renderingService,
        IChartValidationService validationService,
        ILogger<ChartApiController> logger)
    {
        _chartService = chartService;
        _dataService = dataService;
        _renderingService = renderingService;
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// Get chart data for a specific data source
    /// </summary>
    [HttpGet("data/{dataSource}")]
    public async Task<ActionResult<ApiResponse<ProcessedChartData>>> GetChartData(
        string dataSource,
        [FromQuery] ChartDataRequest request)
    {
        try
        {
            // Create a temporary configuration from the request
            var tempConfig = new ChartConfiguration
            {
                CsvFile = dataSource,
                ChartType = "Column", // Default
                Series = request.YColumns.Select((col, index) => new ChartSeries
                {
                    Name = col,
                    DataColumn = col,
                    AggregationFunction = request.AggregationFunction,
                    DisplayOrder = index,
                    IsVisible = true
                }).ToList(),
                XAxis = !string.IsNullOrWhiteSpace(request.XColumn)
                    ? new ChartAxis { AxisType = "X", DataColumn = request.XColumn }
                    : null,
                Filters = request.Filters.Select(f => new ChartFilter
                {
                    Column = f.Column,
                    FilterType = f.FilterType,
                    IncludedValues = f.Values,
                    MinValue = f.MinValue,
                    MaxValue = f.MaxValue,
                    Pattern = f.Pattern,
                    IsCaseSensitive = f.IsCaseSensitive,
                    IsRegex = f.IsRegex,
                    IsEnabled = true
                }).ToList()
            };

            var processedData = await _dataService.ProcessDataAsync(dataSource, tempConfig);

            // Apply limit and offset if specified
            if (request.Limit.HasValue || request.Offset.HasValue)
            {
                var skip = request.Offset ?? 0;
                var take = request.Limit ?? processedData.DataPoints.Count;

                processedData.DataPoints = processedData.DataPoints
                    .Skip(skip)
                    .Take(take)
                    .ToList();
            }

            return ApiResponse<ProcessedChartData>.SuccessResult(processedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chart data for {DataSource}", dataSource);
            return ApiResponse<ProcessedChartData>.ErrorResult("Error processing chart data");
        }
    }

    /// <summary>
    /// Get columns for a data source
    /// </summary>
    [HttpGet("columns/{dataSource}")]
    public async Task<ActionResult<ApiResponse<List<DataSpark.Core.Models.Analysis.ColumnInfo>>>> GetColumns(string dataSource)
    {
        try
        {
            var columns = await _dataService.GetColumnsAsync(dataSource);
            return ApiResponse<List<DataSpark.Core.Models.Analysis.ColumnInfo>>.SuccessResult(columns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting columns for {DataSource}", dataSource);
            return ApiResponse<List<DataSpark.Core.Models.Analysis.ColumnInfo>>.ErrorResult("Error retrieving column information");
        }
    }

    /// <summary>
    /// Get unique values for a specific column
    /// </summary>
    [HttpGet("values/{dataSource}/{column}")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetColumnValues(
        string dataSource,
        string column,
        [FromQuery] int maxValues = 1000)
    {
        try
        {
            var values = await _dataService.GetColumnValuesAsync(dataSource, column, maxValues);
            return ApiResponse<List<string>>.SuccessResult(values);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting values for column {Column} in {DataSource}", column, dataSource);
            return ApiResponse<List<string>>.ErrorResult("Error retrieving column values");
        }
    }

    /// <summary>
    /// Get values for multiple columns
    /// </summary>
    [HttpPost("values/{dataSource}")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, List<string>>>>> GetMultipleColumnValues(
        string dataSource,
        [FromBody] List<string> columns,
        [FromQuery] int maxValues = 100)
    {
        try
        {
            var values = await _dataService.GetMultipleColumnValuesAsync(dataSource, columns, maxValues);
            return ApiResponse<Dictionary<string, List<string>>>.SuccessResult(values);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting multiple column values for {DataSource}", dataSource);
            return ApiResponse<Dictionary<string, List<string>>>.ErrorResult("Error retrieving column values");
        }
    }

    /// <summary>
    /// Render a chart configuration
    /// </summary>
    [HttpPost("render")]
    public async Task<ActionResult<ApiResponse<ChartRenderResult>>> RenderChart([FromBody] ChartConfiguration config)
    {
        try
        {
            // Validate the configuration
            var validationResult = await _validationService.ValidateConfigurationAsync(config, config.CsvFile);
            if (!validationResult.IsValid)
            {
                return ApiResponse<ChartRenderResult>.ErrorResult(
                    "Configuration validation failed",
                    validationResult.Errors);
            }

            // Process the data
            var processedData = await _dataService.ProcessDataAsync(config.CsvFile, config);

            // Render the chart
            var renderResult = await _renderingService.RenderChartAsync(config, processedData);

            if (!renderResult.Success)
            {
                return ApiResponse<ChartRenderResult>.ErrorResult(
                    "Chart rendering failed",
                    renderResult.Errors);
            }

            return ApiResponse<ChartRenderResult>.SuccessResult(renderResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering chart");
            return ApiResponse<ChartRenderResult>.ErrorResult("Error rendering chart");
        }
    }

    /// <summary>
    /// Validate a chart configuration
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<ApiResponse<ValidationResult>>> ValidateConfiguration([FromBody] ValidationRequest request)
    {
        try
        {
            var validationResult = await _validationService.ValidateConfigurationAsync(
                request.Configuration,
                request.DataSource);

            return ApiResponse<ValidationResult>.SuccessResult(validationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating chart configuration");
            return ApiResponse<ValidationResult>.ErrorResult("Error validating configuration");
        }
    }

    /// <summary>
    /// Get data summary for a data source
    /// </summary>
    [HttpGet("summary/{dataSource}")]
    public async Task<ActionResult<ApiResponse<DataSummary>>> GetDataSummary(string dataSource)
    {
        try
        {
            var summary = await _dataService.GetDataSummaryAsync(dataSource);
            return ApiResponse<DataSummary>.SuccessResult(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data summary for {DataSource}", dataSource);
            return ApiResponse<DataSummary>.ErrorResult("Error retrieving data summary");
        }
    }

    /// <summary>
    /// Get available data sources
    /// </summary>
    [HttpGet("datasources")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetDataSources()
    {
        try
        {
            var dataSources = await _dataService.GetAvailableDataSourcesAsync();
            return ApiResponse<List<string>>.SuccessResult(dataSources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available data sources");
            return ApiResponse<List<string>>.ErrorResult("Error retrieving data sources");
        }
    }

    /// <summary>
    /// Get chart configurations
    /// </summary>
    [HttpGet("configurations")]
    public async Task<ActionResult<ApiResponse<List<ChartConfigurationSummary>>>> GetConfigurations(
        [FromQuery] string? dataSource = null)
    {
        try
        {
            var configurations = await _chartService.GetConfigurationsAsync(dataSource);
            return ApiResponse<List<ChartConfigurationSummary>>.SuccessResult(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chart configurations");
            return ApiResponse<List<ChartConfigurationSummary>>.ErrorResult("Error retrieving configurations");
        }
    }

    /// <summary>
    /// Get a specific chart configuration
    /// </summary>
    [HttpGet("configurations/{id}")]
    public async Task<ActionResult<ApiResponse<ChartConfiguration>>> GetConfiguration(int id)
    {
        try
        {
            var configuration = await _chartService.GetConfigurationAsync(id);
            if (configuration == null)
            {
                return NotFound(ApiResponse<ChartConfiguration>.ErrorResult("Configuration not found"));
            }

            return ApiResponse<ChartConfiguration>.SuccessResult(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chart configuration {Id}", id);
            return ApiResponse<ChartConfiguration>.ErrorResult("Error retrieving configuration");
        }
    }

    /// <summary>
    /// Save a chart configuration
    /// </summary>
    [HttpPost("configurations")]
    public async Task<ActionResult<ApiResponse<ChartConfiguration>>> SaveConfiguration([FromBody] ChartConfiguration config)
    {
        try
        {
            var savedConfig = await _chartService.SaveConfigurationAsync(config);
            return ApiResponse<ChartConfiguration>.SuccessResult(savedConfig, "Configuration saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving chart configuration");
            return ApiResponse<ChartConfiguration>.ErrorResult(ex.Message);
        }
    }

    /// <summary>
    /// Delete a chart configuration
    /// </summary>
    [HttpDelete("configurations/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteConfiguration(int id)
    {
        try
        {
            var result = await _chartService.DeleteConfigurationAsync(id);
            if (result)
            {
                return ApiResponse<bool>.SuccessResult(true, "Configuration deleted successfully");
            }
            else
            {
                return ApiResponse<bool>.ErrorResult("Configuration not found or could not be deleted");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chart configuration {Id}", id);
            return ApiResponse<bool>.ErrorResult("Error deleting configuration");
        }
    }

    /// <summary>
    /// Get available chart types
    /// </summary>
    [HttpGet("charttypes")]
    public ActionResult<ApiResponse<Dictionary<string, ChartTypeInfo>>> GetChartTypes()
    {
        try
        {
            return ApiResponse<Dictionary<string, ChartTypeInfo>>.SuccessResult(ChartTypes.All);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chart types");
            return ApiResponse<Dictionary<string, ChartTypeInfo>>.ErrorResult("Error retrieving chart types");
        }
    }

    /// <summary>
    /// Get available color palettes
    /// </summary>
    [HttpGet("palettes")]
    public ActionResult<ApiResponse<Dictionary<string, string[]>>> GetColorPalettes()
    {
        try
        {
            return ApiResponse<Dictionary<string, string[]>>.SuccessResult(ColorPalettes.All);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting color palettes");
            return ApiResponse<Dictionary<string, string[]>>.ErrorResult("Error retrieving color palettes");
        }
    }

    /// <summary>
    /// Bulk operations on configurations
    /// </summary>
    [HttpPost("configurations/bulk")]
    public async Task<ActionResult<ApiResponse<object>>> BulkOperation([FromBody] BulkOperationRequest request)
    {
        try
        {
            switch (request.Operation.ToLowerInvariant())
            {
                case "delete":
                    var deletedCount = await _chartService.DeleteConfigurationsAsync(request.ConfigurationIds);
                    return ApiResponse<object>.SuccessResult(
                        new { deletedCount },
                        $"Deleted {deletedCount} configurations");

                default:
                    return ApiResponse<object>.ErrorResult("Unknown bulk operation");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk operation: {Operation}", request.Operation);
            return ApiResponse<object>.ErrorResult("Error performing bulk operation");
        }
    }

    /// <summary>
    /// Export chart content.
    /// Supports CSV, JSON, and SVG from server-side data.
    /// PNG/JPEG are exported client-side from canvas.
    /// </summary>
    [HttpPost("export")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Export([FromBody] DataSpark.Web.Models.Chart.ChartExportRequest request)
    {
        try
        {
            var configuration = await _chartService.GetConfigurationAsync(request.ChartId).ConfigureAwait(false);
            if (configuration == null)
            {
                return NotFound(new { error = "Chart configuration not found." });
            }

            var data = await _dataService.ProcessDataAsync(configuration.CsvFile, configuration).ConfigureAwait(false);
            var format = (request.Format ?? "CSV").Trim().ToUpperInvariant();
            var safeName = string.Concat(configuration.Name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));

            return format switch
            {
                "CSV" => File(BuildCsvBytes(data), "text/csv", $"{safeName}.csv"),
                "JSON" => File(BuildJsonBytes(configuration, data), "application/json", $"{safeName}.json"),
                "SVG" => File(BuildSvgBytes(configuration, data, request.Width ?? configuration.Width, request.Height ?? configuration.Height), "image/svg+xml", $"{safeName}.svg"),
                _ => BadRequest(new { error = "Unsupported export format. Use CSV, JSON, or SVG." })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting chart {ChartId} as {Format}", request.ChartId, request.Format);
            return StatusCode(500, new { error = "Error exporting chart." });
        }
    }

    private static byte[] BuildCsvBytes(ProcessedChartData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Category,Series,Value");
        foreach (var point in data.DataPoints)
        {
            sb.Append('"').Append(point.Category.Replace("\"", "\"\"")).Append('"').Append(',');
            sb.Append('"').Append(point.SeriesName.Replace("\"", "\"\"")).Append('"').Append(',');
            sb.AppendLine(point.Value.ToString(CultureInfo.InvariantCulture));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] BuildJsonBytes(ChartConfiguration config, ProcessedChartData data)
    {
        var payload = new
        {
            configuration = config,
            summary = data.GetSummary(),
            dataPoints = data.DataPoints
        };
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static byte[] BuildSvgBytes(ChartConfiguration config, ProcessedChartData data, int width, int height)
    {
        width = Math.Clamp(width, 300, 2400);
        height = Math.Clamp(height, 200, 1600);

        var points = data.DataPoints.Take(100).ToList();
        var maxValue = points.Any() ? points.Max(p => p.Value) : 1d;
        if (maxValue <= 0) maxValue = 1d;

        var marginLeft = 50;
        var marginBottom = 40;
        var marginTop = 40;
        var chartWidth = width - marginLeft - 20;
        var chartHeight = height - marginTop - marginBottom;
        var barWidth = points.Any() ? Math.Max(2, chartWidth / Math.Max(points.Count, 1)) : chartWidth;

        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">");
        sb.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");
        sb.AppendLine($"<text x=\"{width / 2}\" y=\"24\" text-anchor=\"middle\" font-family=\"Arial\" font-size=\"16\" fill=\"#222\">{System.Security.SecurityElement.Escape(config.Title ?? config.Name)}</text>");
        sb.AppendLine($"<line x1=\"{marginLeft}\" y1=\"{marginTop + chartHeight}\" x2=\"{marginLeft + chartWidth}\" y2=\"{marginTop + chartHeight}\" stroke=\"#777\"/>");
        sb.AppendLine($"<line x1=\"{marginLeft}\" y1=\"{marginTop}\" x2=\"{marginLeft}\" y2=\"{marginTop + chartHeight}\" stroke=\"#777\"/>");

        for (var i = 0; i < points.Count; i++)
        {
            var p = points[i];
            var barHeight = (int)Math.Round((p.Value / maxValue) * chartHeight);
            var x = marginLeft + i * barWidth;
            var y = marginTop + chartHeight - barHeight;
            sb.AppendLine($"<rect x=\"{x}\" y=\"{y}\" width=\"{Math.Max(1, barWidth - 1)}\" height=\"{Math.Max(0, barHeight)}\" fill=\"#3b82f6\"/>");
        }

        sb.AppendLine("</svg>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
