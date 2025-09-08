using Microsoft.AspNetCore.Mvc;
using Sql2Csv.Core.Models;
using Sql2Csv.Web.Models;
using Sql2Csv.Web.Services;

namespace Sql2Csv.Web.Controllers;

/// <summary>
/// Unified controller for handling database tables using a common data access pattern
/// </summary>
public class UnifiedDataController : Controller
{
    private readonly IUnifiedWebDataService _unifiedDataService;
    private readonly ILogger<UnifiedDataController> _logger;

    public UnifiedDataController(
        IUnifiedWebDataService unifiedDataService,
        ILogger<UnifiedDataController> logger)
    {
        _unifiedDataService = unifiedDataService;
        _logger = logger;
    }

    /// <summary>
    /// Index page for unified data analysis - shows available data sources
    /// </summary>
    public IActionResult Index()
    {
        ViewData["Title"] = "Unified Data Analysis";
        return View();
    }

    /// <summary>
    /// Unified analysis endpoint that handles database tables
    /// </summary>
    /// <param name="fileId">Unique identifier for the file</param>
    /// <param name="fileType">Type of data source (Database)</param>
    /// <param name="dataSourceName">Table name for databases</param>
    /// <returns>Unified analysis view</returns>
    public async Task<IActionResult> Analyze(string fileId, DataSourceType fileType, string? dataSourceName = null)
    {
        try
        {
            _logger.LogInformation("Starting unified analysis for file: {FileId}, Type: {FileType}, DataSource: {DataSourceName}", 
                fileId, fileType, dataSourceName);

            // Get file path from session or TempData
            var filePath = GetFilePathFromSession(fileId, fileType);
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogWarning("File path not found for fileId: {FileId}", fileId);
                TempData["ErrorMessage"] = "File not found. Please upload a file first.";
                return RedirectToAction("Index", "Home");
            }

            // Validate file exists
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError("File not found at path: {FilePath}", filePath);
                TempData["ErrorMessage"] = "The selected file is no longer available.";
                return RedirectToAction("Index", "Home");
            }

            // Add timeout for analysis operation
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            // Perform unified analysis
            var analysis = await _unifiedDataService.AnalyzeDataSourceAsync(filePath, fileType, dataSourceName, cts.Token);

            // Create unified view model
            var viewModel = new UnifiedDataSourceAnalysisViewModel
            {
                FileId = fileId,
                FilePath = filePath,
                FileType = fileType,
                DataSourceName = dataSourceName ?? Path.GetFileNameWithoutExtension(filePath),
                DisplayName = GenerateDisplayName(fileType, filePath, dataSourceName),
                Analysis = analysis,
                CanViewData = true,
                CanExport = true
            };

            // Update session state
            UpdateSessionState(fileId, filePath, fileType, dataSourceName);

            _logger.LogInformation("Unified analysis completed successfully for {DisplayName}", viewModel.DisplayName);
            return View("UnifiedAnalysis", viewModel);
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Analysis timed out for file: {FileId}", fileId);
            TempData["ErrorMessage"] = "Analysis timed out. The file might be too large.";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing data source: {FileId} ({FileType})", fileId, fileType);
            TempData["ErrorMessage"] = "An error occurred while analyzing the data source.";
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// Unified data viewing endpoint with pagination support
    /// </summary>
    public IActionResult ViewUnifiedData(string fileId, DataSourceType fileType, string? dataSourceName = null)
    {
        try
        {
            var filePath = GetFilePathFromSession(fileId, fileType);
            if (string.IsNullOrEmpty(filePath))
            {
                TempData["ErrorMessage"] = "File not found.";
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new UnifiedDataViewViewModel
            {
                FileId = fileId,
                FilePath = filePath,
                FileType = fileType,
                DataSourceName = dataSourceName ?? Path.GetFileNameWithoutExtension(filePath),
                DisplayName = GenerateDisplayName(fileType, filePath, dataSourceName)
            };

            return View("UnifiedDataView", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up data view for: {FileId}", fileId);
            TempData["ErrorMessage"] = "An error occurred while setting up data view.";
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// API endpoint for DataTables server-side processing - handles database data
    /// </summary>
    [HttpPost]
    public async Task<JsonResult> GetDataTableData([FromBody] UnifiedDataTableRequest request)
    {
        try
        {
            var filePath = GetFilePathFromSession(request.FileId, request.FileType);
            if (string.IsNullOrEmpty(filePath))
            {
                return Json(new { error = "File not found" });
            }

            var dataTablesRequest = new DataTablesRequest
            {
                Draw = request.Draw,
                Start = request.Start,
                Length = request.Length,
                SearchValue = request.Search?.Value,
                Columns = request.Columns?.Select(c => new DataTablesColumn
                {
                    Data = c.Data ?? string.Empty,
                    Name = c.Name ?? string.Empty,
                    Searchable = c.Searchable,
                    Orderable = c.Orderable
                }).ToList() ?? new List<DataTablesColumn>(),
                Order = request.Order?.Select(o => new DataTablesOrder
                {
                    Column = o.Column,
                    Dir = o.Dir
                }).ToList() ?? new List<DataTablesOrder>()
            };

            var result = await _unifiedDataService.GetDataAsync(filePath, request.FileType, request.DataSourceName, dataTablesRequest);

            return Json(new
            {
                draw = result.Draw,
                recordsTotal = result.RecordsTotal,
                recordsFiltered = result.RecordsFiltered,
                data = result.Data,
                error = result.Error
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data table data for file: {FileId}", request.FileId);
            return Json(new { error = "An error occurred while loading data" });
        }
    }

    /// <summary>
    /// Unified export endpoint
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Export(string fileId, DataSourceType fileType, List<string> selectedDataSources)
    {
        try
        {
            var filePath = GetFilePathFromSession(fileId, fileType);
            if (string.IsNullOrEmpty(filePath))
            {
                TempData["ErrorMessage"] = "File not found.";
                return RedirectToAction("Index", "Home");
            }

            if (!selectedDataSources.Any())
            {
                TempData["ErrorMessage"] = "Please select at least one data source to export.";
                return RedirectToAction("Analyze", new { fileId, fileType });
            }

            var results = await _unifiedDataService.ExportToCsvAsync(filePath, fileType, selectedDataSources);

            var viewModel = new ExportResultsViewModel
            {
                FileId = fileId,
                FileType = fileType,
                Results = results
            };

            TempData["SuccessMessage"] = $"Successfully exported {results.Count} data source(s).";
            return View("ExportResults", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data for file: {FileId}", fileId);
            TempData["ErrorMessage"] = "An error occurred during export.";
            return RedirectToAction("Analyze", new { fileId, fileType });
        }
    }

    /// <summary>
    /// Gets the file path from session state based on file ID and type
    /// </summary>
    private string? GetFilePathFromSession(string fileId, DataSourceType fileType)
    {
        // Try to get from TempData first (most recent)
        var filePath = TempData[$"FilePath_{fileId}"] as string;
        if (!string.IsNullOrEmpty(filePath))
        {
            TempData.Keep($"FilePath_{fileId}"); // Keep for next request
            return filePath;
        }

        // Try session state
        filePath = HttpContext.Session.GetString($"FilePath_{fileId}");
        if (!string.IsNullOrEmpty(filePath))
        {
            return filePath;
        }

        // Legacy fallback for existing session keys
        if (fileType == DataSourceType.Database)
        {
            return HttpContext.Session.GetString("CurrentDatabaseFilePath") ??
                   TempData["DatabaseFilePath"] as string;
        }
        else
        {
            return HttpContext.Session.GetString("CurrentDataFilePath") ??
                   TempData["DataFilePath"] as string;
        }
    }

    /// <summary>
    /// Updates session state with file information
    /// </summary>
    private void UpdateSessionState(string fileId, string filePath, DataSourceType fileType, string? dataSourceName)
    {
        // Store with unique file ID
        HttpContext.Session.SetString($"FilePath_{fileId}", filePath);
        HttpContext.Session.SetString($"FileType_{fileId}", fileType.ToString());
        
        if (!string.IsNullOrEmpty(dataSourceName))
        {
            HttpContext.Session.SetString($"DataSourceName_{fileId}", dataSourceName);
        }

        // Also store in TempData for immediate access
        TempData[$"FilePath_{fileId}"] = filePath;
        TempData[$"FileType_{fileId}"] = fileType.ToString();
        
        if (!string.IsNullOrEmpty(dataSourceName))
        {
            TempData[$"DataSourceName_{fileId}"] = dataSourceName;
        }
    }

    /// <summary>
    /// Generates a user-friendly display name for the data source
    /// </summary>
    private static string GenerateDisplayName(DataSourceType fileType, string filePath, string? dataSourceName)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        
        return fileType switch
        {
            DataSourceType.Database when !string.IsNullOrEmpty(dataSourceName) => $"{fileName}.{dataSourceName}",
            DataSourceType.Database => fileName,
            _ => fileName
        };
    }
}
