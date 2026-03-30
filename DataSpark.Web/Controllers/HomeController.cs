using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Models.Analysis;
using DataSpark.Web.Models;
using DataSpark.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DataSpark.Web.Controllers;


public class HomeController : BaseController
{
    public HomeController(IWebHostEnvironment env, ILogger<HomeController> logger, DataSpark.Web.Services.CsvFileService csvFileService,
        Sql2Csv.Core.Services.Analysis.ICsvProcessingService csvProcessingService,
        IExportService exportService, IDataExportService dataExportService)
        : base(env, logger, csvFileService, csvProcessingService, exportService, dataExportService)
    {
    }
    public IActionResult Index()
    {
        _logger.LogInformation("Index page accessed.");
        // Pass available CSV files to the view for dropdowns
        return ReturnToIndexWithFiles();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadCSV(IFormFile file)
    {
        // Use the base controller method for file validation
        var fileError = HandleFileUploadError(file);
        if (fileError != null) return fileError;

        try
        {
            // Use the CSV file service to save the file
            var savedFileName = await _csvFileService.SaveUploadedFileAsync(file);

            // Check if file save failed
            var saveError = HandleFileSaveFailure(savedFileName);
            if (saveError != null) return saveError;

            // Read and process the saved file using the service
            var csvData = _csvFileService.ReadCsvForVisualization(savedFileName!);

            // Check if CSV data is empty
            var emptyError = HandleEmptyCsvData(csvData.Records);
            if (emptyError != null) return emptyError;

            // Return success response
            return SetupSuccessfulUploadResponse(savedFileName!, csvData.Records);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Error processing the file");
        }
    }

    public IActionResult Files()
    {
        // Pass available CSV files to the view for dropdowns
        return ReturnToIndexWithFiles();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> CompleteAnalysis(string? fileName)
    {
        // Always provide list of available files
        var files = _csvFileService.GetCsvFileNames();

        if (string.IsNullOrWhiteSpace(fileName))
        {
            // Return an empty model with file list for initial selection
            var emptyModel = new CsvViewModel
            {
                AvailableCsvFiles = files,
                FileName = string.Empty,
                Message = files.Any() ? "Select a file or upload one to view complete analysis." : "No CSV files found. Upload one to begin."
            };
            return View(emptyModel);
        }

        // Validate file exists in list
        if (!files.Contains(fileName))
        {
            var badModel = new CsvViewModel
            {
                AvailableCsvFiles = files,
                FileName = fileName,
                Message = "Specified file not found in available list."
            };
            return View(badModel);
        }

        var model = await _csvProcessingService.ProcessCsvWithFallbackAsync(fileName);
        model.AvailableCsvFiles = files;

        // If empty or failed processing, still show selector
        if (model.RowCount == 0)
        {
            model.Message = string.IsNullOrEmpty(model.Message)
                ? "File is empty or could not be processed."
                : model.Message;
        }
        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
