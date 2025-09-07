using DataSpark.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Sql2Csv.Core.Interfaces;
using System.Linq;

namespace DataSpark.Web.Controllers
{
    public class VisualizationController : BaseController
    {
        public VisualizationController(IWebHostEnvironment env, ILogger<VisualizationController> logger, DataSpark.Web.Services.CsvFileService csvFileService,
            Sql2Csv.Core.Services.Analysis.ICsvProcessingService csvProcessingService,
            IExportService exportService, IDataExportService dataExportService)
            : base(env, logger, csvFileService, csvProcessingService, exportService, dataExportService)
        {
        }

        public IActionResult Index()
        {
            var files = _csvFileService.GetCsvFileNames();
            return View(files);
        }

        [HttpGet]
        public IActionResult Data(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("File name required");

            if (!_csvFileService.FileExists(fileName))
                return NotFound("File not found");

            // Use the CSV file service to read data for visualization
            var csvData = _csvFileService.ReadCsvForVisualization(fileName);
            return Json(new { headers = csvData.Headers, columns = csvData.Columns });
        }

        [HttpGet]
        public IActionResult Bivariate()
        {
            var files = _csvFileService.GetCsvFileNames();
            return View(files);
        }

        [HttpGet]
        public IActionResult Pivot()
        {
            var files = _csvFileService.GetCsvFileNames();
            return View(files);
        }

        [HttpGet]
        public IActionResult Univariate()
        {
            var files = _csvFileService.GetCsvFileNames();
            return View(files);
        }

        [HttpGet]
        public IActionResult FullAnalysisReport()
        {
            var files = _csvFileService.GetCsvFileNames();
            return View(files);
        }

        [HttpGet]
        public IActionResult CompleteAnalysis()
        {
            // ...existing code for dashboard can be migrated here if needed...
            return View();
        }
    }
}
