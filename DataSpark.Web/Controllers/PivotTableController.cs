using DataSpark.Core.Interfaces;
using DataSpark.Core.Models;
using DataSpark.Core.Models.Analysis;
using DataSpark.Web.Services;
using DataSpark.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using System.Collections.Concurrent;

namespace DataSpark.Web.Controllers;

public class PivotTableController : BaseController
{
    private static readonly ConcurrentDictionary<string, List<PivotConfiguration>> SavedPivotConfigurations = new();

    public PivotTableController(
        IWebHostEnvironment env,
        ILogger<PivotTableController> logger,
    DataSpark.Web.Services.CsvFileService csvFileService,
    DataSpark.Core.Services.Analysis.ICsvProcessingService csvProcessingService,
    IExportService exportService, IDataExportService dataExportService)
    : base(env, logger, csvFileService, csvProcessingService, exportService, dataExportService)
    {
    }

    [HttpGet]
    public IActionResult Index()
    {
        try
        {
            var model = BuildPivotTableViewModel();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pivot table interface");
            return View("Error", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }

    [HttpGet]
    public IActionResult FullPage()
    {
        var model = BuildPivotTableViewModel();
        return View(model);
    }

    [HttpGet]
    public IActionResult SelectColumn()
    {
        var model = BuildPivotTableViewModel();
        return View(model);
    }

    [HttpGet]
    public IActionResult Results(string? fileName, List<string>? columns = null)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = HttpContext.Session.GetString("CurrentCsvFile");
        }

        if (string.IsNullOrEmpty(fileName))
        {
            return RedirectToAction("SelectColumn");
        }

        try
        {
            var records = _csvFileService.ReadCsvRecords(fileName);
            if (!records.Any())
            {
                return RedirectToAction("SelectColumn");
            }

            var firstRecord = (IDictionary<string, object>)records.First();
            var model = new PivotTableViewModel
            {
                CurrentFile = fileName,
                ColumnHeaders = columns?.Any() == true ? columns : firstRecord.Keys.ToList(),
                RecordCount = records.Count,
                AvailableFiles = _csvFileService.GetCsvFileNames()
            };

            HttpContext.Session.SetString("CurrentCsvFile", fileName);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pivot table results for file {FileName}", fileName);
            return RedirectToAction("SelectColumn");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult LoadCsvData([FromBody] LoadCsvDataRequest request)
    {
        var response = ProcessLoadCsvDataRequest(request);
        return Json(response);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveConfiguration([FromBody] SaveConfigurationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                return Json(new StandardResponse
                {
                    Success = false,
                    Error = "Configuration name is required"
                });
            }

            var config = new PivotConfiguration
            {
                Id = (int)(DateTime.UtcNow.Ticks % int.MaxValue),
                Name = request.Name,
                DataSource = request.CsvFile,
                AggregationFunction = request.AggregatorName,
                RendererType = request.RendererName,
                RowFields = request.Rows,
                ColumnFields = request.Cols,
                ValueFields = request.Vals,
                CreatedDate = DateTime.UtcNow
            };

            var sessionKey = GetSessionConfigurationKey();
            var configs = SavedPivotConfigurations.GetOrAdd(sessionKey, _ => new List<PivotConfiguration>());
            lock (configs)
            {
                var existing = configs.FirstOrDefault(c =>
                    string.Equals(c.Name, config.Name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(c.DataSource, config.DataSource, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.RowFields = config.RowFields;
                    existing.ColumnFields = config.ColumnFields;
                    existing.ValueFields = config.ValueFields;
                    existing.AggregationFunction = config.AggregationFunction;
                    existing.RendererType = config.RendererType;
                    existing.CreatedDate = DateTime.UtcNow;
                    config.Id = existing.Id;
                }
                else
                {
                    configs.Add(config);
                }
            }

            _logger.LogInformation("Pivot table configuration saved: {@Config}", config);

            return Json(new StandardResponse
            {
                Success = true,
                Message = "Configuration saved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving pivot table configuration");
            return Json(new StandardResponse
            {
                Success = false,
                Error = $"Error saving configuration: {ex.Message}"
            });
        }
    }

    [HttpGet]
    public IActionResult LoadConfigurations(string? fileName = null)
    {
        try
        {
            var sessionKey = GetSessionConfigurationKey();
            var configs = SavedPivotConfigurations.GetValueOrDefault(sessionKey, new List<PivotConfiguration>());
            var result = string.IsNullOrWhiteSpace(fileName)
                ? configs
                : configs.Where(c => string.Equals(c.DataSource, fileName, StringComparison.OrdinalIgnoreCase)).ToList();

            return Json(new
            {
                success = true,
                data = result.OrderByDescending(c => c.CreatedDate).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pivot configurations");
            return Json(new { success = false, error = "Error loading saved configurations." });
        }
    }

    [HttpGet]
    public IActionResult LoadConfiguration(string name, string fileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(fileName))
            {
                return Json(new { success = false, error = "Configuration name and file name are required." });
            }

            var sessionKey = GetSessionConfigurationKey();
            var configs = SavedPivotConfigurations.GetValueOrDefault(sessionKey, new List<PivotConfiguration>());
            var config = configs.FirstOrDefault(c =>
                string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.DataSource, fileName, StringComparison.OrdinalIgnoreCase));

            if (config == null)
            {
                return Json(new { success = false, error = "Configuration not found." });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    aggregatorName = config.AggregationFunction,
                    rendererName = config.RendererType,
                    cols = config.ColumnFields,
                    rows = config.RowFields,
                    vals = config.ValueFields,
                    inclusions = new Dictionary<string, object>(),
                    exclusions = new Dictionary<string, object>()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pivot configuration {ConfigurationName}", name);
            return Json(new { success = false, error = "Error loading configuration." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Export(string format, string configuration)
    {
        try
        {
            if (string.IsNullOrEmpty(format) || string.IsNullOrEmpty(configuration))
            {
                return BadRequest("Format and configuration are required");
            }

            var config = JsonSerializer.Deserialize<PivotTableConfiguration>(configuration);
            if (config == null)
            {
                return BadRequest("Invalid configuration");
            }

            // Load the CSV data
            var records = _csvFileService.ReadCsvRecords(config.CsvFile);
            if (!records.Any())
            {
                return BadRequest("No data available for export");
            }

            // Convert to export format
            var exportData = ConvertCsvToJsonData(records);
            var fileName = $"pivot_table_export_{DateTime.Now:yyyyMMdd_HHmmss}";

            switch (format.ToLower())
            {
                case "csv":
                    return ExportAsCsv(exportData, $"{fileName}.csv");
                case "tsv":
                    return ExportAsTsv(exportData, $"{fileName}.tsv");
                case "json":
                    return ExportAsJson(exportData, config, $"{fileName}.json");
                case "excel":
                    return ExportAsExcel(exportData, $"{fileName}.xlsx");
                default:
                    return BadRequest("Unsupported export format");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting pivot table data");
            return BadRequest($"Export failed: {ex.Message}");
        }
    }

    private string GetSessionConfigurationKey()
    {
        return HttpContext.Session.GetString("PivotConfigSessionKey")
            ?? CreateSessionConfigurationKey();
    }

    private string CreateSessionConfigurationKey()
    {
        var key = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString("PivotConfigSessionKey", key);
        return key;
    }
}
