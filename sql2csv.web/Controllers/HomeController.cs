using Microsoft.AspNetCore.Mvc;
using Sql2Csv.Web.Models;
using Sql2Csv.Web.Services;
using System.Diagnostics;

namespace Sql2Csv.Web.Controllers;

public class HomeController : Controller
{
    private readonly IWebDatabaseService _databaseService;
    private readonly IPersistedFileService _persistedFileService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IWebDatabaseService databaseService, IPersistedFileService persistedFileService, ILogger<HomeController> logger)
    {
        _databaseService = databaseService;
        _persistedFileService = persistedFileService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var persistedFiles = await _persistedFileService.GetAvailableFilesAsync();
        var model = new FileUploadViewModel
        {
            AvailableFiles = persistedFiles
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Upload(FileUploadViewModel model)
    {
        // Special validation for existing file selection vs new file upload
        if (!string.IsNullOrEmpty(model.SelectedFileId))
        {
            // User selected an existing file - clear file upload validation errors
            ModelState.Remove(nameof(model.DatabaseFile));
        }
        else if (model.DatabaseFile == null)
        {
            // No file uploaded and no existing file selected
            ModelState.AddModelError(nameof(model.DatabaseFile), "Please select a database file or choose an existing file.");
        }

        if (!ModelState.IsValid)
        {
            // Reload available files for view
            model.AvailableFiles = await _persistedFileService.GetAvailableFilesAsync();
            model.ErrorMessage = "Please fix the validation errors below.";
            return View("Index", model);
        }

        try
        {
            string? filePath = null;
            string? fileName = null;

            // Check if user selected an existing file
            if (!string.IsNullOrEmpty(model.SelectedFileId))
            {
                _logger.LogInformation("Processing existing file selection with ID: {SelectedFileId}", model.SelectedFileId);

                var persistedFile = await _persistedFileService.GetPersistedFileAsync(model.SelectedFileId);
                if (persistedFile != null)
                {
                    _logger.LogInformation("Found persisted file: {OriginalFileName} at path: {StoredFilePath}",
                        persistedFile.OriginalFileName, persistedFile.StoredFilePath);

                    // Check if the persisted file still exists on disk
                    if (!System.IO.File.Exists(persistedFile.StoredFilePath))
                    {
                        _logger.LogWarning("Persisted file no longer exists on disk: {StoredFilePath}", persistedFile.StoredFilePath);
                        model.ErrorMessage = "Selected file is no longer available on disk.";
                        model.AvailableFiles = await _persistedFileService.GetAvailableFilesAsync();
                        return View("Index", model);
                    }

                    filePath = persistedFile.StoredFilePath;
                    fileName = persistedFile.OriginalFileName;
                    await _persistedFileService.UpdateLastAccessedAsync(model.SelectedFileId);

                    _logger.LogInformation("Successfully set up persisted file for analysis");
                }
                else
                {
                    _logger.LogWarning("Persisted file not found in metadata: {SelectedFileId}", model.SelectedFileId);
                    model.ErrorMessage = "Selected file is no longer available.";
                    model.AvailableFiles = await _persistedFileService.GetAvailableFilesAsync();
                    return View("Index", model);
                }
            }
            // Handle new file upload
            else if (model.DatabaseFile != null)
            {
                var (success, errorMessage, tempFilePath, tableCount) = await _databaseService.SaveUploadedFileAsync(model.DatabaseFile);

                if (!success)
                {
                    model.ErrorMessage = errorMessage;
                    model.AvailableFiles = await _persistedFileService.GetAvailableFilesAsync();
                    return View("Index", model);
                }

                filePath = tempFilePath;
                fileName = model.DatabaseFile.FileName;

                // If user wants to save for future use, persist the file
                if (model.SaveForFutureUse && !string.IsNullOrEmpty(tempFilePath))
                {
                    try
                    {
                        _logger.LogInformation("Starting to persist file for future use: {TempFilePath}", tempFilePath);

                        await _persistedFileService.SavePersistedFileAsync(
                            model.DatabaseFile,
                            tempFilePath,
                            tableCount,
                            model.FileDescription
                        );

                        _logger.LogInformation("Successfully persisted file for future use");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to persist file, but continuing with temporary file");
                    }
                }
            }
            else
            {
                model.ErrorMessage = "Please select a database file or choose an existing file.";
                model.AvailableFiles = await _persistedFileService.GetAvailableFilesAsync();
                return View("Index", model);
            }

            // Store the file path in TempData for the next request
            TempData["DatabaseFilePath"] = filePath;
            TempData["DatabaseFileName"] = fileName;

            return RedirectToAction("Analyze");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during file upload");
            model.ErrorMessage = "An error occurred while processing your file. Please try again.";
            model.AvailableFiles = await _persistedFileService.GetAvailableFilesAsync();
            return View("Index", model);
        }
    }

    /// <summary>
    /// AJAX drag & drop upload endpoint returning JSON payload.
    /// </summary>
    [HttpPost]
    [Route("api/upload")] // /api/upload
    public async Task<IActionResult> UploadApi(IFormFile? file, bool persist = false, string? description = null, CancellationToken cancellationToken = default)
    {
        if (file == null)
            return BadRequest(new { error = "No file provided." });

        var (success, errorMessage, tempFilePath, tableCount) = await _databaseService.SaveUploadedFileAsync(file, cancellationToken);
        if (!success || string.IsNullOrEmpty(tempFilePath))
            return BadRequest(new { error = errorMessage ?? "Upload failed." });

        string? persistedId = null;
        try
        {
            if (persist)
            {
                var persisted = await _persistedFileService.SavePersistedFileAsync(file, tempFilePath, tableCount, description);
                persistedId = persisted.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist uploaded file (continuing)");
        }

        // Stash for analysis flow
        TempData["DatabaseFilePath"] = tempFilePath;
        TempData["DatabaseFileName"] = file.FileName;

        return Ok(new { id = persistedId, name = file.FileName, tableCount, persisted = persistedId != null });
    }

    public async Task<IActionResult> ManageFiles()
    {
        var files = await _persistedFileService.GetPersistedFilesAsync();
        var totalSize = await _persistedFileService.GetTotalStorageSizeAsync();

        var model = new FileManagementViewModel
        {
            Files = files,
            TotalStorageSize = totalSize
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteFile(string fileId)
    {
        try
        {
            var success = await _persistedFileService.DeletePersistedFileAsync(fileId);
            if (success)
            {
                TempData["SuccessMessage"] = "File deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "File not found or could not be deleted.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting persisted file {FileId}", fileId);
            TempData["ErrorMessage"] = "An error occurred while deleting the file.";
        }

        return RedirectToAction("ManageFiles");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateDescription(string fileId, string? description)
    {
        try
        {
            var success = await _persistedFileService.UpdateFileDescriptionAsync(fileId, description);
            if (success)
            {
                TempData["SuccessMessage"] = "Description updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "File not found or could not be updated.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating description for file {FileId}", fileId);
            TempData["ErrorMessage"] = "An error occurred while updating the description.";
        }

        return RedirectToAction("ManageFiles");
    }

    public async Task<IActionResult> Analyze()
    {
        var filePath = TempData["DatabaseFilePath"] as string;
        var fileName = TempData["DatabaseFileName"] as string;

        _logger.LogInformation("Analyze action called with FilePath: {FilePath}, FileName: {FileName}", filePath, fileName);

        if (string.IsNullOrEmpty(filePath))
        {
            _logger.LogWarning("No file path found in TempData, redirecting to Index");
            return RedirectToAction("Index");
        }

        // Check if file exists before attempting analysis
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogError("File not found at path: {FilePath}", filePath);
            TempData["ErrorMessage"] = "The selected database file is no longer available.";
            return RedirectToAction("Index");
        }

        try
        {
            _logger.LogInformation("Starting database analysis for file: {FilePath}", filePath);

            // Add timeout to the analysis operation
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2)); // 2 minute overall timeout

            var analysis = await _databaseService.AnalyzeDatabaseAsync(filePath, cts.Token);

            // Persist file path in session for AJAX table data retrieval
            HttpContext.Session.SetString("CurrentDatabaseFilePath", filePath);
            if (!string.IsNullOrEmpty(fileName))
                HttpContext.Session.SetString("CurrentDatabaseFileName", fileName);

            // Keep the file path for subsequent operations
            TempData.Keep("DatabaseFilePath");
            TempData.Keep("DatabaseFileName");

            _logger.LogInformation("Database analysis completed successfully");
            return View(analysis);
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Database analysis timed out for file: {FilePath}", filePath);
            TempData["ErrorMessage"] = "Database analysis timed out. The file might be too large or corrupted.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing database: {FilePath}", filePath);
            TempData["ErrorMessage"] = "An error occurred while analyzing the database.";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> ExportTables(List<string> selectedTables)
    {
        var filePath = TempData["DatabaseFilePath"] as string;
        if (string.IsNullOrEmpty(filePath) || !selectedTables.Any())
        {
            return RedirectToAction("Index");
        }

        try
        {
            var exportResults = await _databaseService.ExportTablesToCsvAsync(filePath, selectedTables);

            // Keep the file path for subsequent operations
            TempData.Keep("DatabaseFilePath");
            TempData.Keep("DatabaseFileName");

            return View("ExportResults", exportResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting tables");
            TempData["ErrorMessage"] = "An error occurred while exporting tables.";
            return RedirectToAction("Analyze");
        }
    }

    [HttpPost]
    public async Task<IActionResult> GenerateCode(List<string> selectedTables, string namespaceName = "Generated.Models")
    {
        var filePath = TempData["DatabaseFilePath"] as string;
        if (string.IsNullOrEmpty(filePath) || !selectedTables.Any())
        {
            return RedirectToAction("Index");
        }

        try
        {
            var codeResults = await _databaseService.GenerateCodeAsync(filePath, selectedTables, namespaceName);

            // Keep the file path for subsequent operations
            TempData.Keep("DatabaseFilePath");
            TempData.Keep("DatabaseFileName");

            return View("CodeResults", codeResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating code");
            TempData["ErrorMessage"] = "An error occurred while generating code.";
            return RedirectToAction("Analyze");
        }
    }

    public async Task<IActionResult> AnalyzeTable(string tableName)
    {
        var filePath = TempData["DatabaseFilePath"] as string;
        var fileName = TempData["DatabaseFileName"] as string;

        _logger.LogInformation("AnalyzeTable action called with TableName: {TableName}, FilePath: {FilePath}", tableName, filePath);

        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(tableName))
        {
            _logger.LogWarning("Missing file path or table name, redirecting to Index");
            return RedirectToAction("Index");
        }

        // Check if file exists before attempting analysis
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogError("File not found at path: {FilePath}", filePath);
            TempData["ErrorMessage"] = "The selected database file is no longer available.";
            return RedirectToAction("Index");
        }

        try
        {
            _logger.LogInformation("Starting table analysis for table: {TableName} in file: {FilePath}", tableName, filePath);

            // Add timeout to the analysis operation
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // 5 minute timeout for table analysis

            var analysis = await _databaseService.AnalyzeTableAsync(filePath, tableName, cts.Token);

            // Keep session file path up to date
            HttpContext.Session.SetString("CurrentDatabaseFilePath", filePath);
            if (!string.IsNullOrEmpty(fileName))
                HttpContext.Session.SetString("CurrentDatabaseFileName", fileName);

            // Keep the file path for subsequent operations
            TempData.Keep("DatabaseFilePath");
            TempData.Keep("DatabaseFileName");

            _logger.LogInformation("Table analysis completed successfully for table: {TableName}", tableName);
            return View(analysis);
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Table analysis timed out for table: {TableName} in file: {FilePath}", tableName, filePath);
            TempData["ErrorMessage"] = "Table analysis timed out. The table might be too large.";
            return RedirectToAction("Analyze");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing table: {TableName} in file: {FilePath}", tableName, filePath);
            TempData["ErrorMessage"] = "An error occurred while analyzing the table.";
            return RedirectToAction("Analyze");
        }
    }

    public async Task<IActionResult> ViewTableData(string tableName)
    {
        var filePath = TempData["DatabaseFilePath"] as string;
        var fileName = TempData["DatabaseFileName"] as string;

        _logger.LogInformation("ViewData action called with TableName: {TableName}, FilePath: {FilePath}", tableName, filePath);

        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(tableName))
        {
            _logger.LogWarning("Missing file path or table name, redirecting to Index");
            return RedirectToAction("Index");
        }

        // Check if file exists before attempting analysis
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogError("File not found at path: {FilePath}", filePath);
            TempData["ErrorMessage"] = "The selected database file is no longer available.";
            return RedirectToAction("Index");
        }

        try
        {
            _logger.LogInformation("Getting table analysis for table: {TableName} in file: {FilePath}", tableName, filePath);

            // Use existing table analysis to get column information
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var analysis = await _databaseService.AnalyzeTableAsync(filePath, tableName, cts.Token);

            // Persist in session for subsequent AJAX calls
            HttpContext.Session.SetString("CurrentDatabaseFilePath", filePath);
            if (!string.IsNullOrEmpty(fileName))
                HttpContext.Session.SetString("CurrentDatabaseFileName", fileName);

            // Keep the file path for subsequent operations
            TempData.Keep("DatabaseFilePath");
            TempData.Keep("DatabaseFileName");

            var model = new ViewDataViewModel
            {
                TableName = tableName,
                DatabaseName = analysis.DatabaseName,
                FilePath = filePath,
                Columns = analysis.ColumnAnalyses.Select(c => new ColumnInfoViewModel
                {
                    Name = c.ColumnName,
                    DataType = c.DataType,
                    IsNullable = c.IsNullable,
                    IsPrimaryKey = c.IsPrimaryKey
                }).ToList()
            };

            _logger.LogInformation("Table analysis retrieved successfully for table: {TableName}", tableName);
            return View(model);
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("ViewTableData operation timed out for table: {TableName} in file: {FilePath}", tableName, filePath);
            TempData["ErrorMessage"] = "Operation timed out. The table might be too large.";
            return RedirectToAction("Analyze");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting table data view: {TableName} in file: {FilePath}", tableName, filePath);
            TempData["ErrorMessage"] = "An error occurred while accessing the table.";
            return RedirectToAction("Analyze");
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetTableData(string tableName, [FromForm] DataTablesRequest request)
    {
        var filePath = HttpContext.Session.GetString("CurrentDatabaseFilePath");
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(tableName))
        {
            return Json(new TableDataResult { Error = "Missing file path or table name" });
        }

        if (!System.IO.File.Exists(filePath))
        {
            return Json(new TableDataResult { Error = "Database file not found" });
        }

        try
        {
            _logger.LogInformation("Getting table data for table: {TableName}, Start: {Start}, Length: {Length}",
                tableName, request.Start, request.Length);

            // Add timeout to the operation
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2)); // 2 minute timeout

            var result = await _databaseService.GetTableDataAsync(filePath, tableName, request, cts.Token);

            return Json(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("GetTableData timed out for table: {TableName}", tableName);
            return Json(new TableDataResult { Error = "Operation timed out" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting table data for table: {TableName}", tableName);
            return Json(new TableDataResult { Error = ex.Message });
        }
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult TableDemo()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
