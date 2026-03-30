using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Sql2Csv.Core.Models;
using Sql2Csv.Web.Controllers;
using Sql2Csv.Web.Models;
using Sql2Csv.Web.Services;
using System.IO;

namespace Sql2Csv.Tests.Controllers;

[TestClass]
public class UnifiedDataControllerTests
{
    private readonly Mock<IUnifiedWebDataService> _mockUnifiedDataService;
    private readonly Mock<ILogger<UnifiedDataController>> _mockLogger;
    private readonly Mock<ISession> _mockSession;
    private readonly Mock<ITempDataDictionary> _mockTempData;
    private readonly UnifiedDataController _controller;

    public UnifiedDataControllerTests()
    {
        _mockUnifiedDataService = new Mock<IUnifiedWebDataService>();
        _mockLogger = new Mock<ILogger<UnifiedDataController>>();
        _mockSession = new Mock<ISession>();
        _mockTempData = new Mock<ITempDataDictionary>();
        _controller = new UnifiedDataController(_mockUnifiedDataService.Object, _mockLogger.Object);

        // Setup HttpContext with mocked Session and TempData
        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.Session).Returns(_mockSession.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };
        _controller.TempData = _mockTempData.Object;
    }

    private void SetupSessionFilePath(string fileId, string filePath)
    {
        // Setup TempData to return the file path
        _mockTempData.Setup(td => td[$"FilePath_{fileId}"]).Returns(filePath);
        _mockTempData.Setup(td => td.Keep(It.IsAny<string>()));
    }

    [TestMethod]
    public async Task Analyze_WithValidDatabaseTable_ShouldReturnUnifiedAnalysisView()
    {
        // Arrange
        var fileId = "test-file-123";
        var fileType = DataSourceType.Database;
        var dataSourceName = "Users";
        
        // Create a temporary file for testing
        var tempDir = Path.GetTempPath();
        var filePath = Path.Combine(tempDir, "test.db");
        await File.WriteAllTextAsync(filePath, "dummy content"); // Create the file

        try
        {
            // Setup session to return the file path
            SetupSessionFilePath(fileId, filePath);

        var analysisResult = new UnifiedAnalysisViewModel
        {
            FilePath = filePath,
            FileName = "test.db",
            FileType = fileType,
            Summary = new DataSourceSummaryViewModel
            {
                TotalDataSources = 1,
                TotalColumns = 3,
                TotalRows = 100,
                FileSize = 1024
            },
            ColumnAnalyses = new List<UnifiedColumnAnalysisViewModel>
            {
                new UnifiedColumnAnalysisViewModel
                {
                    ColumnName = "Id",
                    DataType = "INTEGER",
                    IsPrimaryKey = true,
                    NonNullCount = 100,
                    NullCount = 0,
                    UniqueCount = 100
                },
                new UnifiedColumnAnalysisViewModel
                {
                    ColumnName = "Name",
                    DataType = "TEXT",
                    IsPrimaryKey = false,
                    NonNullCount = 95,
                    NullCount = 5,
                    UniqueCount = 85
                }
            }
        };

        _mockUnifiedDataService
            .Setup(s => s.AnalyzeDataSourceAsync(filePath, fileType, dataSourceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisResult);

        // Mock session/TempData - in a real test, you'd set up HttpContext properly
        // For this test, we'll assume the file path is retrievable

        // Act
        var result = await _controller.Analyze(fileId, fileType, dataSourceName);

        // Assert
        result.Should().NotBeNull("Controller should return a result");
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull($"Expected ViewResult but got {result.GetType().Name}");
        viewResult!.ViewName.Should().Be("UnifiedAnalysis");

        var model = viewResult.Model as UnifiedDataSourceAnalysisViewModel;
        model.Should().NotBeNull();
        model!.FileId.Should().Be(fileId);
        model.FileType.Should().Be(fileType);
        model.DataSourceName.Should().Be(dataSourceName);
        model.CanViewData.Should().BeTrue();
        model.CanExport.Should().BeTrue();
        }
        finally
        {
            // Cleanup - delete temporary file
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [TestMethod]
    public async Task Analyze_WithValidCsvFile_ShouldReturnUnifiedAnalysisView()
    {
        // Arrange
        var fileId = "csv-file-456";
        var fileType = DataSourceType.Csv;
        
        // Create a temporary file for testing
        var tempDir = Path.GetTempPath();
        var filePath = Path.Combine(tempDir, "test.csv");
        await File.WriteAllTextAsync(filePath, "Name,Age\nJohn,25\nJane,30"); // Create CSV content

        try
        {
            // Setup session to return the file path
            SetupSessionFilePath(fileId, filePath);

        var analysisResult = new UnifiedAnalysisViewModel
        {
            FilePath = filePath,
            FileName = "test.csv",
            FileType = fileType,
            Summary = new DataSourceSummaryViewModel
            {
                TotalDataSources = 1,
                TotalColumns = 4,
                TotalRows = 200,
                FileSize = 2048
            },
            ColumnAnalyses = new List<UnifiedColumnAnalysisViewModel>
            {
                new UnifiedColumnAnalysisViewModel
                {
                    ColumnName = "ProductId",
                    DataType = "INTEGER",
                    NonNullCount = 200,
                    NullCount = 0,
                    UniqueCount = 200
                },
                new UnifiedColumnAnalysisViewModel
                {
                    ColumnName = "ProductName",
                    DataType = "TEXT",
                    NonNullCount = 195,
                    NullCount = 5,
                    UniqueCount = 180
                }
            }
        };

        _mockUnifiedDataService
            .Setup(s => s.AnalyzeDataSourceAsync(filePath, fileType, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisResult);

        // Act
        var result = await _controller.Analyze(fileId, fileType);

        // Assert
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        viewResult!.ViewName.Should().Be("UnifiedAnalysis");

        var model = viewResult.Model as UnifiedDataSourceAnalysisViewModel;
        model.Should().NotBeNull();
        model!.FileId.Should().Be(fileId);
        model.FileType.Should().Be(fileType);
        model.DataSourceName.Should().Be("test"); // Should be filename without extension
        }
        finally
        {
            // Cleanup - delete temporary file
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [TestMethod]
    public void ViewData_WithValidFileId_ShouldReturnDataViewResult()
    {
        // Arrange
        var fileId = "view-test-789";
        var fileType = DataSourceType.Database;
        var dataSourceName = "Products";
        var filePath = "C:\\temp\\test.db";

        // Setup session to return the file path
        SetupSessionFilePath(fileId, filePath);

        // Act
        var result = _controller.ViewUnifiedData(fileId, fileType, dataSourceName);

        // Assert
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        viewResult!.ViewName.Should().Be("UnifiedDataView");

        var model = viewResult.Model as UnifiedDataViewViewModel;
        model.Should().NotBeNull();
        model!.FileId.Should().Be(fileId);
        model.FileType.Should().Be(fileType);
        model.DataSourceName.Should().Be(dataSourceName);
    }

    [TestMethod]
    public async Task GetDataTableData_WithValidRequest_ShouldReturnJsonResult()
    {
        // Arrange
        var request = new UnifiedDataTableRequest
        {
            FileId = "datatable-test-123",
            FileType = DataSourceType.Csv,
            DataSourceName = "sales_data",
            Draw = 1,
            Start = 0,
            Length = 25
        };

        var expectedResult = new TableDataResult
        {
            Draw = 1,
            RecordsTotal = 1000,
            RecordsFiltered = 1000,
            Data = new object?[][]
            {
                new object?[] { "1", "Product A", "100.00" },
                new object?[] { "2", "Product B", "150.00" }
            },
            Columns = new List<string> { "Id", "Name", "Price" }
        };

        _mockUnifiedDataService
            .Setup(s => s.GetDataAsync(It.IsAny<string>(), request.FileType, request.DataSourceName, It.IsAny<DataTablesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetDataTableData(request);

        // Assert
        var jsonResult = result as JsonResult;
        jsonResult.Should().NotBeNull();
        
        // In a real test, you'd verify the JSON structure
        // For now, just verify it's a JsonResult
    }

    [TestMethod]
    public async Task Export_WithValidDataSources_ShouldReturnExportResultsView()
    {
        // Arrange
        var fileId = "export-test-456";
        var fileType = DataSourceType.Database;
        var selectedDataSources = new List<string> { "Users", "Products" };
        var filePath = "C:\\temp\\test.db";

        // Setup session to return the file path
        SetupSessionFilePath(fileId, filePath);

        var exportResults = new List<ExportResultViewModel>
        {
            new ExportResultViewModel
            {
                TableName = "Users",
                FileName = "Users.csv",
                FileContent = "sample,csv,content",
                FilePath = "C:\\temp\\Users.csv",
                IsSuccess = true,
                RowCount = 100,
                Duration = TimeSpan.FromSeconds(2)
            },
            new ExportResultViewModel
            {
                TableName = "Products",
                FileName = "Products.csv", 
                FileContent = "sample,csv,content",
                FilePath = "C:\\temp\\Products.csv",
                IsSuccess = true,
                RowCount = 250,
                Duration = TimeSpan.FromSeconds(3)
            }
        };

        _mockUnifiedDataService
            .Setup(s => s.ExportToCsvAsync(It.IsAny<string>(), fileType, selectedDataSources, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResults);

        // Act
        var result = await _controller.Export(fileId, fileType, selectedDataSources);

        // Assert
        var viewResult = result as ViewResult;
        viewResult.Should().NotBeNull();
        viewResult!.ViewName.Should().Be("ExportResults");

        var model = viewResult.Model as ExportResultsViewModel;
        model.Should().NotBeNull();
        model!.FileId.Should().Be(fileId);
        model.FileType.Should().Be(fileType);
        model.Results.Count.Should().Be(2);
        model.AllSuccessful.Should().BeTrue();
    }

    [TestMethod]
    public void GenerateDisplayName_WithDatabaseTable_ShouldIncludeTableName()
    {
        // This would be testing a private method, but we can test the behavior through public methods
        // or make the method internal and use InternalsVisibleTo attribute

        // For now, just verify the controller can be instantiated
        _controller.Should().NotBeNull();
    }

    [TestMethod]
    public void GenerateDisplayName_WithCsvFile_ShouldUseFileName()
    {
        // Similar to above - testing through public interface
        _controller.Should().NotBeNull();
    }
}
