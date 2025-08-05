using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sql2Csv.Core.Configuration;
using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Models;
using Sql2Csv.Core.Services;
using Sql2Csv.Tests.Infrastructure;
using FluentAssertions;
using System.IO;

namespace Sql2Csv.Tests.Services;

[TestClass]
public class ExportServiceTests : DatabaseTestBase
{
    private Mock<ILogger<ExportService>> _mockLogger = null!;
    private Mock<ISchemaService> _mockSchemaService = null!;
    private Mock<IOptions<Sql2CsvOptions>> _mockOptions = null!;
    private ExportService _exportService = null!;
    private Sql2CsvOptions _options = null!;
    private string _outputDirectory = null!;

    [TestInitialize]
    public override async Task TestInitialize()
    {
        await base.TestInitialize();

        _mockLogger = new Mock<ILogger<ExportService>>();
        _mockSchemaService = new Mock<ISchemaService>();
        _mockOptions = new Mock<IOptions<Sql2CsvOptions>>();

        _options = new Sql2CsvOptions
        {
            Export = new ExportOptions
            {
                IncludeHeaders = true,
                Delimiter = ",",
                Encoding = "UTF-8"
            },
            Database = new DatabaseOptions
            {
                Timeout = 300
            }
        };

        _mockOptions.Setup(o => o.Value).Returns(_options);

        // Setup mock schema service
        var tableNames = new[] { "Users", "Orders", "Products" };
        _mockSchemaService.Setup(s => s.GetTableNamesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tableNames);

        _exportService = new ExportService(_mockLogger.Object, _mockSchemaService.Object, _mockOptions.Object);

        _outputDirectory = Path.Combine(Path.GetTempPath(), $"test_output_{Guid.NewGuid()}");
        Directory.CreateDirectory(_outputDirectory);
    }

    [TestCleanup]
    public override void TestCleanup()
    {
        base.TestCleanup();

        if (Directory.Exists(_outputDirectory))
        {
            Directory.Delete(_outputDirectory, true);
        }
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ExportService(null!, _mockSchemaService.Object, _mockOptions.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [TestMethod]
    public void Constructor_WithNullSchemaService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ExportService(_mockLogger.Object, null!, _mockOptions.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("schemaService");
    }

    [TestMethod]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ExportService(_mockLogger.Object, _mockSchemaService.Object, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [TestMethod]
    public async Task ExportDatabaseToCsvAsync_WithValidDatabase_ShouldExportAllTables()
    {
        // Arrange
        var databaseConfig = new DatabaseConfiguration("TestDB", ConnectionString);

        // Act
        var results = await _exportService.ExportDatabaseToCsvAsync(databaseConfig, _outputDirectory);

        // Assert
        var resultList = results.ToList();
        resultList.Should().HaveCount(3);
        resultList.Should().OnlyContain(r => r.IsSuccess);
        resultList.Should().OnlyContain(r => r.DatabaseName == "TestDB");

        // Verify files were created
        var usersCsvPath = Path.Combine(_outputDirectory, "Users_extract.csv");
        var ordersCsvPath = Path.Combine(_outputDirectory, "Orders_extract.csv");
        var productsCsvPath = Path.Combine(_outputDirectory, "Products_extract.csv");

        File.Exists(usersCsvPath).Should().BeTrue();
        File.Exists(ordersCsvPath).Should().BeTrue();
        File.Exists(productsCsvPath).Should().BeTrue();

        // Verify content
        var usersCsv = await File.ReadAllTextAsync(usersCsvPath);
        usersCsv.Should().Contain("Id,Name,Email,Age,CreatedDate");
        usersCsv.Should().Contain("John Doe");
        usersCsv.Should().Contain("jane@example.com");
    }

    [TestMethod]
    public async Task ExportDatabaseToCsvAsync_WithNullDatabaseConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = async () => await _exportService.ExportDatabaseToCsvAsync(null!, _outputDirectory);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public async Task ExportDatabaseToCsvAsync_WithNullOutputDirectory_ShouldThrowArgumentException()
    {
        // Arrange
        var databaseConfig = new DatabaseConfiguration("TestDB", ConnectionString);

        // Act & Assert
        var action = async () => await _exportService.ExportDatabaseToCsvAsync(databaseConfig, null!);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task ExportDatabaseToCsvAsync_WithEmptyOutputDirectory_ShouldThrowArgumentException()
    {
        // Arrange
        var databaseConfig = new DatabaseConfiguration("TestDB", ConnectionString);

        // Act & Assert
        var action = async () => await _exportService.ExportDatabaseToCsvAsync(databaseConfig, string.Empty);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task ExportTableToCsvAsync_WithValidTable_ShouldExportSuccessfully()
    {
        // Arrange
        var databaseConfig = new DatabaseConfiguration("TestDB", ConnectionString);
        var outputFilePath = Path.Combine(_outputDirectory, "users_export.csv");

        // Act
        var result = await _exportService.ExportTableToCsvAsync(databaseConfig, "Users", outputFilePath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.DatabaseName.Should().Be("TestDB");
        result.TableName.Should().Be("Users");
        result.OutputFilePath.Should().Be(outputFilePath);
        result.RowCount.Should().Be(3);
        result.ErrorMessage.Should().BeNull();

        File.Exists(outputFilePath).Should().BeTrue();

        var csvContent = await File.ReadAllTextAsync(outputFilePath);
        csvContent.Should().Contain("Id,Name,Email,Age,CreatedDate");
        csvContent.Should().Contain("John Doe");
        csvContent.Should().Contain("Jane Smith");
        csvContent.Should().Contain("Bob Johnson");
    }

    [TestMethod]
    public async Task ExportTableToCsvAsync_WithNonExistentTable_ShouldReturnFailedResult()
    {
        // Arrange
        var databaseConfig = new DatabaseConfiguration("TestDB", ConnectionString);
        var outputFilePath = Path.Combine(_outputDirectory, "nonexistent.csv");

        // Act
        var result = await _exportService.ExportTableToCsvAsync(databaseConfig, "NonExistentTable", outputFilePath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.DatabaseName.Should().Be("TestDB");
        result.TableName.Should().Be("NonExistentTable");
        result.RowCount.Should().Be(0);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task ExportTableToCsvAsync_WithInvalidConnectionString_ShouldReturnFailedResult()
    {
        // Arrange
        var databaseConfig = new DatabaseConfiguration("TestDB", "Invalid Connection String");
        var outputFilePath = Path.Combine(_outputDirectory, "test.csv");

        // Act
        var result = await _exportService.ExportTableToCsvAsync(databaseConfig, "Users", outputFilePath);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task ExportTableToCsvAsync_WithCustomDelimiter_ShouldUseCustomDelimiter()
    {
        // Arrange
        _options.Export.Delimiter = ";";
        var databaseConfig = new DatabaseConfiguration("TestDB", ConnectionString);
        var outputFilePath = Path.Combine(_outputDirectory, "users_semicolon.csv");

        // Act
        var result = await _exportService.ExportTableToCsvAsync(databaseConfig, "Users", outputFilePath);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var csvContent = await File.ReadAllTextAsync(outputFilePath);
        csvContent.Should().Contain("Id;Name;Email;Age;CreatedDate");
        csvContent.Should().Contain("John Doe");
    }

    [TestMethod]
    public async Task ExportTableToCsvAsync_WithHeadersDisabled_ShouldNotIncludeHeaders()
    {
        // Arrange
        _options.Export.IncludeHeaders = false;
        var databaseConfig = new DatabaseConfiguration("TestDB", ConnectionString);
        var outputFilePath = Path.Combine(_outputDirectory, "users_no_headers.csv");

        // Act
        var result = await _exportService.ExportTableToCsvAsync(databaseConfig, "Users", outputFilePath);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var csvContent = await File.ReadAllTextAsync(outputFilePath);
        csvContent.Should().NotContain("Id,Name,Email,Age,CreatedDate");
        csvContent.Should().Contain("John Doe");
    }

    [TestMethod]
    public async Task ExportDatabaseToCsvAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var databaseConfig = new DatabaseConfiguration("TestDB", ConnectionString);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = async () => await _exportService.ExportDatabaseToCsvAsync(databaseConfig, _outputDirectory, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task ExportTableToCsvAsync_ShouldCreateOutputDirectoryIfNotExists()
    {
        // Arrange
        var databaseConfig = new DatabaseConfiguration("TestDB", ConnectionString);
        var newDirectory = Path.Combine(_outputDirectory, "new_subdir");
        var outputFilePath = Path.Combine(newDirectory, "users.csv");

        // Act
        var result = await _exportService.ExportTableToCsvAsync(databaseConfig, "Users", outputFilePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Directory.Exists(newDirectory).Should().BeTrue();
        File.Exists(outputFilePath).Should().BeTrue();
    }
}
