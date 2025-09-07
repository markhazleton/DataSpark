using Sql2Csv.Core.Models.Analysis;
using DataSpark.Web.Models;
using Sql2Csv.Core.Models.Charts;
using DataSpark.Web.Services;
using DataSpark.Web.Services.Chart;
using Sql2Csv.Core.Services.Charts;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using DataSpark.Web.Models.Chart;

namespace DataSpark.Web.Controllers;

public class BaseController : Controller
{
    protected readonly IWebHostEnvironment _env;
    protected readonly ILogger _logger;
    protected readonly CsvFileService _csvFileService;
    protected readonly Sql2Csv.Core.Services.Analysis.ICsvProcessingService _csvProcessingService;
    protected readonly Sql2Csv.Core.Services.Export.IExportService _exportService;

    // Chart services (optional for controllers that don't need them)
    protected readonly IChartService? _chartService;
    protected readonly IChartDataService? _dataService;
    protected readonly IChartRenderingService? _renderingService;
    protected readonly IChartValidationService? _validationService;
    protected readonly IChartConfigurationViewModelBuilder? _chartViewModelBuilder;

    // Constructor for CSV-only controllers (like HomeController)
    public BaseController(IWebHostEnvironment env, ILogger logger,
        CsvFileService csvFileService,
        Sql2Csv.Core.Services.Analysis.ICsvProcessingService csvProcessingService,
        Sql2Csv.Core.Services.Export.IExportService exportService)
    {
        _env = env;
        _logger = logger;
        _csvFileService = csvFileService;
        _csvProcessingService = csvProcessingService;
        _exportService = exportService;
    }

    // Constructor for Chart controllers (like ChartController)
    public BaseController(IWebHostEnvironment env, ILogger logger,
        CsvFileService csvFileService,
        Sql2Csv.Core.Services.Analysis.ICsvProcessingService csvProcessingService,
        Sql2Csv.Core.Services.Export.IExportService exportService,
        IChartService chartService, IChartDataService dataService,
        IChartRenderingService renderingService, IChartValidationService validationService,
        IChartConfigurationViewModelBuilder chartViewModelBuilder)
        : this(env, logger, csvFileService, csvProcessingService, exportService)
    {
        _chartService = chartService;
        _dataService = dataService;
        _renderingService = renderingService;
        _validationService = validationService;
        _chartViewModelBuilder = chartViewModelBuilder;
    }

    // Protected method for handling common file validation logic
    protected IActionResult? HandleFileNotFound(string fileName, string errorMessage = "No file specified.")
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return ReturnToIndexWithError(errorMessage);
        }
        return null;
    }

    // Protected method for handling empty CSV files
    protected IActionResult? HandleEmptyCsv(CsvViewModel model, string fileName)
    {
        if (model == null || model.RowCount == 0)
        {
            return ReturnToIndexWithError("The selected CSV file is empty or could not be read.");
        }
        return null;
    }

    // Protected method for handling file upload errors
    protected IActionResult? HandleFileUploadError(IFormFile file, string errorMessage = "Please upload a valid CSV file.")
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("No file uploaded.");
            return ReturnToIndexWithError(errorMessage);
        }
        return null;
    }

    // Protected method for handling file save failures
    protected IActionResult? HandleFileSaveFailure(string? savedFileName, string errorMessage = "Failed to save the uploaded file.")
    {
        if (savedFileName == null)
        {
            return ReturnToIndexWithError(errorMessage);
        }
        return null;
    }

    // Protected method for handling empty CSV data
    protected IActionResult? HandleEmptyCsvData(IEnumerable<dynamic> records, string errorMessage = "The CSV file is empty.")
    {
        if (!records.Any())
        {
            return ReturnToIndexWithError(errorMessage);
        }
        return null;
    }

    // Protected method for handling exceptions with logging
    protected IActionResult HandleException(Exception ex, string errorMessagePrefix = "An error occurred while processing the file")
    {
        _logger.LogError(ex, errorMessagePrefix);
        return ReturnToIndexWithError($"{errorMessagePrefix}: {ex.Message}");
    }

    // Protected method for setting up successful file upload response
    protected IActionResult SetupSuccessfulUploadResponse(string savedFileName, IEnumerable<dynamic> records, int recordsToShow = 10)
    {
        ViewBag.Message = "CSV file uploaded and saved successfully!";
        ViewBag.Records = records.Take(recordsToShow).ToList();
        ViewBag.FilePath = $"/files/{savedFileName}";
        var filesList = _csvFileService.GetCsvFileNames();
        return View("Index", filesList);
    }

    // Protected method for getting files list and returning to Index view with error
    protected IActionResult ReturnToIndexWithError(string errorMessage)
    {
        ViewBag.ErrorMessage = errorMessage;
        var files = _csvFileService.GetCsvFileNames();
        return View("Index", files);
    }

    // Protected method for getting files list and returning to Index view
    protected IActionResult ReturnToIndexWithFiles(object? model = null)
    {
        var files = _csvFileService.GetCsvFileNames();
        return View("Index", model ?? files);
    }

    // Chart-related helper methods
    // Removed BuildConfigurationViewModel & CreateDefaultConfiguration from BaseController.
    // These concerns are now handled by IChartConfigurationViewModelBuilder to thin controllers.

    protected List<ExportOption> GetExportOptions()
    {
        return new List<ExportOption>
        {
            new ExportOption { Key = "png", Name = "PNG Image", Description = "Portable Network Graphics", MimeType = "image/png", FileExtension = "png" },
            new ExportOption { Key = "jpg", Name = "JPEG Image", Description = "Joint Photographic Experts Group", MimeType = "image/jpeg", FileExtension = "jpg" },
            new ExportOption { Key = "svg", Name = "SVG Vector", Description = "Scalable Vector Graphics", MimeType = "image/svg+xml", FileExtension = "svg" },
            new ExportOption { Key = "pdf", Name = "PDF Document", Description = "Portable Document Format", MimeType = "application/pdf", FileExtension = "pdf" },
            new ExportOption { Key = "csv", Name = "CSV Data", Description = "Comma Separated Values", MimeType = "text/csv", FileExtension = "csv" },
            new ExportOption { Key = "tsv", Name = "TSV Data", Description = "Tab Separated Values", MimeType = "text/tab-separated-values", FileExtension = "tsv" },
            new ExportOption { Key = "json", Name = "JSON Data", Description = "JavaScript Object Notation", MimeType = "application/json", FileExtension = "json" }
        };
    }

    // Protected method for handling chart errors with logging
    protected IActionResult HandleChartException(Exception ex, string errorMessagePrefix = "An error occurred while processing the chart", string? viewName = null)
    {
        _logger.LogError(ex, errorMessagePrefix);

        var viewModel = new ChartIndexViewModel
        {
            ErrorMessage = $"{errorMessagePrefix}: {ex.Message}",
            AvailableDataSources = new List<string>(),
            SavedConfigurations = new List<ChartConfigurationSummary>()
        };

        if (!string.IsNullOrEmpty(viewName))
        {
            return View(viewName, viewModel);
        }

        return View(viewModel);
    }

    // Protected method for handling chart configuration errors
    protected IActionResult HandleChartConfigurationError(string errorMessage, ChartConfiguration? configuration = null, string? dataSource = null)
    {
        if (configuration != null && !string.IsNullOrEmpty(dataSource))
        {
            var viewModel = new Models.Chart.ChartConfigurationViewModel
            {
                Configuration = configuration,
                ErrorMessage = errorMessage,
                DataSource = dataSource
            };
            return View("Configure", viewModel);
        }

        return View("Configure", new Models.Chart.ChartConfigurationViewModel
        {
            ErrorMessage = errorMessage
        });
    }

    // Pivot Table helper methods
    protected List<Dictionary<string, object>> ConvertCsvToJsonData(
        List<dynamic> records,
        List<string>? selectedColumns = null)
    {
        var result = new List<Dictionary<string, object>>();

        foreach (var record in records)
        {
            var rowDict = new Dictionary<string, object>();
            var dynamicRecord = (IDictionary<string, object>)record;

            foreach (var kvp in dynamicRecord)
            {
                if (selectedColumns == null || selectedColumns.Contains(kvp.Key))
                {
                    rowDict[kvp.Key] = kvp.Value ?? string.Empty;
                }
            }
            result.Add(rowDict);
        }

        return result;
    }

    protected IActionResult ExportAsCsv(List<Dictionary<string, object>> data, string fileName)
        => data.Any() ? File(_exportService.ExportCsv(data), "text/csv", fileName) : BadRequest("No data to export");

    protected IActionResult ExportAsTsv(List<Dictionary<string, object>> data, string fileName)
        => data.Any() ? File(_exportService.ExportTsv(data), "text/tab-separated-values", fileName) : BadRequest("No data to export");

    protected IActionResult ExportAsJson<T>(List<Dictionary<string, object>> data, T config, string fileName)
        => File(_exportService.ExportJson(data, config), "application/json", fileName);

    protected IActionResult ExportAsExcel(List<Dictionary<string, object>> data, string fileName)
        => ExportAsCsv(data, fileName.Replace(".xlsx", ".csv"));

    protected PivotTableViewModel BuildPivotTableViewModel(string? currentFile = null)
    {
        var model = new PivotTableViewModel
        {
            AvailableFiles = _csvFileService.GetCsvFileNames(),
            CurrentFile = currentFile ?? HttpContext.Session.GetString("CurrentCsvFile") ?? string.Empty
        };

        if (!string.IsNullOrEmpty(model.CurrentFile))
        {
            try
            {
                var records = _csvFileService.ReadCsvRecords(model.CurrentFile);
                if (records.Any())
                {
                    var firstRecord = (IDictionary<string, object>)records.First();
                    model.ColumnHeaders = firstRecord.Keys.ToList();
                    model.RecordCount = records.Count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load current file {FileName}", model.CurrentFile);
            }
        }

        return model;
    }

    protected LoadCsvDataResponse ProcessLoadCsvDataRequest(LoadCsvDataRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FileName))
            {
                return new LoadCsvDataResponse
                {
                    Success = false,
                    Error = "File name is required"
                };
            }

            var records = _csvFileService.ReadCsvRecords(request.FileName);
            if (!records.Any())
            {
                return new LoadCsvDataResponse
                {
                    Success = false,
                    Error = "File not found or contains no data"
                };
            }

            var processedData = ConvertCsvToJsonData(records, request.SelectedColumns);
            var firstRecord = (IDictionary<string, object>)records.First();
            var columns = request.SelectedColumns?.Any() == true
                ? request.SelectedColumns
                : firstRecord.Keys.ToList();

            HttpContext.Session.SetString("CurrentCsvFile", request.FileName);

            return new LoadCsvDataResponse
            {
                Success = true,
                Data = processedData,
                Columns = columns,
                RecordCount = processedData.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading CSV data for file: {FileName}", request?.FileName);
            return new LoadCsvDataResponse
            {
                Success = false,
                Error = "Failed to load CSV data: " + ex.Message
            };
        }
    }
}
