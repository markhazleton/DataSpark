using System.Text;
using System.Text.Json;
using DataSpark.Core.Interfaces;
using DataSpark.Core.Models;
using DataSpark.Web.Models.Database;
using Microsoft.AspNetCore.Mvc;

namespace DataSpark.Web.Controllers;

public class DatabaseController : Controller
{
    private readonly IDatabaseAnalysisService _databaseAnalysisService;
    private readonly IPersistedFileService _persistedFileService;
    private readonly IExportPackagingService _exportPackagingService;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(
        IDatabaseAnalysisService databaseAnalysisService,
        IPersistedFileService persistedFileService,
        IExportPackagingService exportPackagingService,
        ILogger<DatabaseController> logger)
    {
        _databaseAnalysisService = databaseAnalysisService;
        _persistedFileService = persistedFileService;
        _exportPackagingService = exportPackagingService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var files = await _persistedFileService.GetAvailableFilesAsync().ConfigureAwait(false);
        return View(new DatabaseIndexViewModel { Files = files });
    }

    [HttpGet]
    public async Task<IActionResult> ManageFiles()
    {
        var files = await _persistedFileService.GetAvailableFilesAsync().ConfigureAwait(false);
        return View("Index", new DatabaseIndexViewModel { Files = files });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, string? description)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return await ReturnIndexWithErrorAsync("Please select a SQLite database file.").ConfigureAwait(false);
            }

            var extension = Path.GetExtension(file.FileName);
            var allowed = new[] { ".db", ".sqlite", ".sqlite3" };
            if (!allowed.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return await ReturnIndexWithErrorAsync("Only SQLite files are supported (.db, .sqlite, .sqlite3).").ConfigureAwait(false);
            }

            var tempPath = Path.Combine(Path.GetTempPath(), $"dataspark-upload-{Guid.NewGuid():N}{extension}");
            await using (var stream = System.IO.File.Create(tempPath))
            {
                await file.CopyToAsync(stream).ConfigureAwait(false);
            }

            var validation = await _databaseAnalysisService.ValidateDatabaseFileAsync(tempPath).ConfigureAwait(false);
            if (!validation.Success)
            {
                System.IO.File.Delete(tempPath);
                return await ReturnIndexWithErrorAsync(validation.ErrorMessage ?? "Database validation failed.").ConfigureAwait(false);
            }

            var persisted = await _persistedFileService.SavePersistedFileAsync(
                new UploadedFormFileInfo(file),
                tempPath,
                validation.TableCount,
                description).ConfigureAwait(false);

            TempData["DatabaseSuccess"] = $"Uploaded '{persisted.OriginalFileName}' successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading database file");
            return await ReturnIndexWithErrorAsync("Upload failed. Please try again.").ConfigureAwait(false);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return RedirectToAction(nameof(Index));
        }

        var deleted = await _persistedFileService.DeletePersistedFileAsync(fileId).ConfigureAwait(false);
        TempData[deleted ? "DatabaseSuccess" : "DatabaseError"] = deleted
            ? "File deleted successfully."
            : "Unable to delete file.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Analyze(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var file = await _persistedFileService.GetPersistedFileAsync(fileId).ConfigureAwait(false);
            if (file == null)
            {
                return await ReturnIndexWithErrorAsync("Database file not found.").ConfigureAwait(false);
            }

            await _persistedFileService.UpdateLastAccessedAsync(fileId).ConfigureAwait(false);
            var analysis = await _databaseAnalysisService.AnalyzeDatabaseAsync(file.StoredFilePath).ConfigureAwait(false);

            return View(new DatabaseAnalyzeViewModel
            {
                File = file,
                Analysis = analysis
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing database file {FileId}", fileId);
            return await ReturnIndexWithErrorAsync("Failed to analyze database.").ConfigureAwait(false);
        }
    }

    [HttpGet]
    public async Task<IActionResult> AnalyzeTable(string fileId, string tableName)
    {
        if (string.IsNullOrWhiteSpace(fileId) || string.IsNullOrWhiteSpace(tableName))
        {
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var file = await _persistedFileService.GetPersistedFileAsync(fileId).ConfigureAwait(false);
            if (file == null)
            {
                return await ReturnIndexWithErrorAsync("Database file not found.").ConfigureAwait(false);
            }

            var analysis = await _databaseAnalysisService.AnalyzeTableAsync(file.StoredFilePath, tableName).ConfigureAwait(false);
            return View(new DatabaseTableViewModel
            {
                File = file,
                Analysis = analysis
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing table {TableName} in file {FileId}", tableName, fileId);
            return await ReturnIndexWithErrorAsync("Failed to analyze table.").ConfigureAwait(false);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportTables(
        string fileId,
        List<string>? tableNames,
        string delimiter = ",",
        bool includeHeaders = true,
        bool exportAll = false,
        bool download = true)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var file = await _persistedFileService.GetPersistedFileAsync(fileId).ConfigureAwait(false);
            if (file == null)
            {
                return await ReturnIndexWithErrorAsync("Database file not found.").ConfigureAwait(false);
            }

            List<string> selectedTables;
            if (exportAll || tableNames == null || tableNames.Count == 0)
            {
                var analysis = await _databaseAnalysisService.AnalyzeDatabaseAsync(file.StoredFilePath).ConfigureAwait(false);
                selectedTables = analysis.Tables.Select(t => t.Name).ToList();
            }
            else
            {
                selectedTables = tableNames;
            }

            var exportResults = await _databaseAnalysisService
                .ExportTablesToCsvAsync(file.StoredFilePath, selectedTables)
                .ConfigureAwait(false);

            if (download)
            {
                if (exportResults.Count == 1)
                {
                    var single = exportResults[0];
                    var bytes = Encoding.UTF8.GetBytes(single.FileContent);
                    return File(bytes, "text/csv", single.FileName);
                }

                var zipBytes = _exportPackagingService.PackageAsZip(exportResults);
                var safeDbName = Path.GetFileNameWithoutExtension(file.OriginalFileName);
                return File(zipBytes, "application/zip", $"{safeDbName}-tables.zip");
            }

            return View("ExportResults", new DatabaseExportResultsViewModel
            {
                File = file,
                Results = exportResults
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting tables for file {FileId}", fileId);
            return await ReturnIndexWithErrorAsync("Table export failed.").ConfigureAwait(false);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateCode(string fileId, List<string>? tableNames, string namespaceName = "DataSpark.Generated")
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var file = await _persistedFileService.GetPersistedFileAsync(fileId).ConfigureAwait(false);
            if (file == null)
            {
                return await ReturnIndexWithErrorAsync("Database file not found.").ConfigureAwait(false);
            }

            List<string> selectedTables;
            if (tableNames == null || tableNames.Count == 0)
            {
                var analysis = await _databaseAnalysisService.AnalyzeDatabaseAsync(file.StoredFilePath).ConfigureAwait(false);
                selectedTables = analysis.Tables.Select(t => t.Name).ToList();
            }
            else
            {
                selectedTables = tableNames;
            }

            var generated = await _databaseAnalysisService
                .GenerateCodeAsync(file.StoredFilePath, selectedTables, namespaceName)
                .ConfigureAwait(false);

            return View("CodeResults", new DatabaseCodeResultsViewModel
            {
                File = file,
                NamespaceName = namespaceName,
                Results = generated
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating code for file {FileId}", fileId);
            return await ReturnIndexWithErrorAsync("Code generation failed.").ConfigureAwait(false);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetTableData(string fileId, string tableName, [FromForm] DataTablesRequest request)
    {
        if (string.IsNullOrWhiteSpace(fileId) || string.IsNullOrWhiteSpace(tableName))
        {
            return Json(new TableDataResult { Error = "Missing file ID or table name." });
        }

        var file = await _persistedFileService.GetPersistedFileAsync(fileId).ConfigureAwait(false);
        if (file == null)
        {
            return Json(new TableDataResult { Error = "Database file not found." });
        }

        var result = await _databaseAnalysisService
            .GetTableDataAsync(file.StoredFilePath, tableName, request)
            .ConfigureAwait(false);

        return Json(result);
    }

    private async Task<ViewResult> ReturnIndexWithErrorAsync(string message)
    {
        var files = await _persistedFileService.GetAvailableFilesAsync().ConfigureAwait(false);
        return View("Index", new DatabaseIndexViewModel
        {
            Files = files,
            ErrorMessage = message,
            SuccessMessage = TempData["DatabaseSuccess"]?.ToString()
        });
    }

}
