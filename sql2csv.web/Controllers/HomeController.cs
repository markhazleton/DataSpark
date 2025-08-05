using Microsoft.AspNetCore.Mvc;
using Sql2Csv.Web.Models;
using Sql2Csv.Web.Services;
using System.Diagnostics;

namespace Sql2Csv.Web.Controllers;

public class HomeController : Controller
{
    private readonly IWebDatabaseService _databaseService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IWebDatabaseService databaseService, ILogger<HomeController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(new FileUploadViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Upload(FileUploadViewModel model)
    {
        if (!ModelState.IsValid || model.DatabaseFile == null)
        {
            model.ErrorMessage = "Please select a valid database file.";
            return View("Index", model);
        }

        try
        {
            var (success, errorMessage, filePath) = await _databaseService.SaveUploadedFileAsync(model.DatabaseFile);

            if (!success)
            {
                model.ErrorMessage = errorMessage;
                return View("Index", model);
            }

            // Store the file path in TempData for the next request
            TempData["DatabaseFilePath"] = filePath;
            TempData["DatabaseFileName"] = model.DatabaseFile.FileName;

            return RedirectToAction("Analyze");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during file upload");
            model.ErrorMessage = "An error occurred while processing your file. Please try again.";
            return View("Index", model);
        }
    }

    public async Task<IActionResult> Analyze()
    {
        var filePath = TempData["DatabaseFilePath"] as string;
        if (string.IsNullOrEmpty(filePath))
        {
            return RedirectToAction("Index");
        }

        try
        {
            var analysis = await _databaseService.AnalyzeDatabaseAsync(filePath);

            // Keep the file path for subsequent operations
            TempData.Keep("DatabaseFilePath");
            TempData.Keep("DatabaseFileName");

            return View(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing database");
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

    public IActionResult About()
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
