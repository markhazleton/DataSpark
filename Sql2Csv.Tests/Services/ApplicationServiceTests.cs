using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sql2Csv.Core.Configuration;
using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Models;
using Sql2Csv.Core.Services;
using FluentAssertions;

namespace Sql2Csv.Tests.Services;

[TestClass]
public class ApplicationServiceTests
{
    private Mock<ILogger<ApplicationService>> _mockLogger = null!;
    private Mock<IDatabaseDiscoveryService> _mockDiscoveryService = null!;
    private Mock<IExportService> _mockExportService = null!;
    private Mock<ISchemaService> _mockSchemaService = null!;
    private Mock<ICodeGenerationService> _mockCodeGenerationService = null!;
    private Mock<IOptions<Sql2CsvOptions>> _mockOptions = null!;
    private ApplicationService _applicationService = null!;
    private Sql2CsvOptions _options = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<ApplicationService>>();
        _mockDiscoveryService = new Mock<IDatabaseDiscoveryService>();
        _mockExportService = new Mock<IExportService>();
        _mockSchemaService = new Mock<ISchemaService>();
        _mockCodeGenerationService = new Mock<ICodeGenerationService>();
        _mockOptions = new Mock<IOptions<Sql2CsvOptions>>();

        _options = new Sql2CsvOptions
        {
            RootPath = @"C:\TestRoot",
            Database = new DatabaseOptions { DefaultName = "TestDB", Timeout = 300 },
            Export = new ExportOptions { IncludeHeaders = true, Delimiter = "," }
        };

        _mockOptions.Setup(o => o.Value).Returns(_options);

        _applicationService = new ApplicationService(
            _mockLogger.Object,
            _mockDiscoveryService.Object,
            _mockExportService.Object,
            _mockSchemaService.Object,
            _mockCodeGenerationService.Object,
            _mockOptions.Object);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ApplicationService(
            null!,
            _mockDiscoveryService.Object,
            _mockExportService.Object,
            _mockSchemaService.Object,
            _mockCodeGenerationService.Object,
            _mockOptions.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [TestMethod]
    public void Constructor_WithNullDiscoveryService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ApplicationService(
            _mockLogger.Object,
            null!,
            _mockExportService.Object,
            _mockSchemaService.Object,
            _mockCodeGenerationService.Object,
            _mockOptions.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("discoveryService");
    }

    [TestMethod]
    public void Constructor_WithNullExportService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ApplicationService(
            _mockLogger.Object,
            _mockDiscoveryService.Object,
            null!,
            _mockSchemaService.Object,
            _mockCodeGenerationService.Object,
            _mockOptions.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("exportService");
    }

    [TestMethod]
    public void Constructor_WithNullSchemaService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ApplicationService(
            _mockLogger.Object,
            _mockDiscoveryService.Object,
            _mockExportService.Object,
            null!,
            _mockCodeGenerationService.Object,
            _mockOptions.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("schemaService");
    }

    [TestMethod]
    public void Constructor_WithNullCodeGenerationService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ApplicationService(
            _mockLogger.Object,
            _mockDiscoveryService.Object,
            _mockExportService.Object,
            _mockSchemaService.Object,
            null!,
            _mockOptions.Object);

        action.Should().Throw<ArgumentNullException>().WithParameterName("codeGenerationService");
    }

    [TestMethod]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ApplicationService(
            _mockLogger.Object,
            _mockDiscoveryService.Object,
            _mockExportService.Object,
            _mockSchemaService.Object,
            _mockCodeGenerationService.Object,
            null!);

        action.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [TestMethod]
    public async Task ExportDatabasesAsync_WithValidDatabases_ShouldExportSuccessfully()
    {
        // Arrange
        var databasePath = @"C:\Databases";
        var outputPath = @"C:\Output";
        var databases = new List<DatabaseConfiguration>
        {
            new("DB1", "Data Source=db1.db"),
            new("DB2", "Data Source=db2.db")
        };

        var exportResults = new List<ExportResult>
        {
            new() { DatabaseName = "DB1", TableName = "Users", OutputFilePath = @"C:\Output\DB1\users.csv", RowCount = 100, IsSuccess = true, Duration = TimeSpan.FromSeconds(1) },
            new() { DatabaseName = "DB2", TableName = "Orders", OutputFilePath = @"C:\Output\DB2\orders.csv", RowCount = 50, IsSuccess = true, Duration = TimeSpan.FromSeconds(2) }
        };

        _mockDiscoveryService.Setup(d => d.DiscoverDatabasesAsync(databasePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(databases);

        _mockExportService.Setup(e => e.ExportDatabaseToCsvAsync(It.IsAny<DatabaseConfiguration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportResults.Take(1));

        // Act
        await _applicationService.ExportDatabasesAsync(databasePath, outputPath);

        // Assert
        _mockDiscoveryService.Verify(d => d.DiscoverDatabasesAsync(databasePath, It.IsAny<CancellationToken>()), Times.Once);
        _mockExportService.Verify(e => e.ExportDatabaseToCsvAsync(It.IsAny<DatabaseConfiguration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task ExportDatabasesAsync_WithNoDatabases_ShouldLogWarningAndReturn()
    {
        // Arrange
        var databasePath = @"C:\Databases";
        var outputPath = @"C:\Output";
        var databases = new List<DatabaseConfiguration>();

        _mockDiscoveryService.Setup(d => d.DiscoverDatabasesAsync(databasePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(databases);

        // Act
        await _applicationService.ExportDatabasesAsync(databasePath, outputPath);

        // Assert
        _mockDiscoveryService.Verify(d => d.DiscoverDatabasesAsync(databasePath, It.IsAny<CancellationToken>()), Times.Once);
        _mockExportService.Verify(e => e.ExportDatabaseToCsvAsync(It.IsAny<DatabaseConfiguration>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ExportDatabasesAsync_WithDiscoveryException_ShouldThrowException()
    {
        // Arrange
        var databasePath = @"C:\Databases";
        var outputPath = @"C:\Output";
        var exception = new InvalidOperationException("Discovery failed");

        _mockDiscoveryService.Setup(d => d.DiscoverDatabasesAsync(databasePath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act & Assert
        var action = async () => await _applicationService.ExportDatabasesAsync(databasePath, outputPath);
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Discovery failed");
    }

    [TestMethod]
    public async Task GenerateSchemaReportsAsync_WithValidDatabases_ShouldGenerateReports()
    {
        // Arrange
        var databasePath = @"C:\Databases";
        var databases = new List<DatabaseConfiguration>
        {
            new("DB1", "Data Source=db1.db")
        };

        var schemaReport = "Schema report content";

        _mockDiscoveryService.Setup(d => d.DiscoverDatabasesAsync(databasePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(databases);

        _mockSchemaService.Setup(s => s.GenerateSchemaReportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(schemaReport);

        // Act
        await _applicationService.GenerateSchemaReportsAsync(databasePath);

        // Assert
        _mockDiscoveryService.Verify(d => d.DiscoverDatabasesAsync(databasePath, It.IsAny<CancellationToken>()), Times.Once);
        _mockSchemaService.Verify(s => s.GenerateSchemaReportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task GenerateSchemaReportsAsync_WithNoDatabases_ShouldLogWarningAndReturn()
    {
        // Arrange
        var databasePath = @"C:\Databases";
        var databases = new List<DatabaseConfiguration>();

        _mockDiscoveryService.Setup(d => d.DiscoverDatabasesAsync(databasePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(databases);

        // Act
        await _applicationService.GenerateSchemaReportsAsync(databasePath);

        // Assert
        _mockDiscoveryService.Verify(d => d.DiscoverDatabasesAsync(databasePath, It.IsAny<CancellationToken>()), Times.Once);
        _mockSchemaService.Verify(s => s.GenerateSchemaReportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task GenerateCodeAsync_WithValidDatabases_ShouldGenerateCode()
    {
        // Arrange
        var databasePath = @"C:\Databases";
        var outputPath = @"C:\Output";
        var namespaceName = "TestNamespace";
        var databases = new List<DatabaseConfiguration>
        {
            new("DB1", "Data Source=db1.db"),
            new("DB2", "Data Source=db2.db")
        };

        _mockDiscoveryService.Setup(d => d.DiscoverDatabasesAsync(databasePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(databases);

        // Act
        await _applicationService.GenerateCodeAsync(databasePath, outputPath, namespaceName);

        // Assert
        _mockDiscoveryService.Verify(d => d.DiscoverDatabasesAsync(databasePath, It.IsAny<CancellationToken>()), Times.Once);
        _mockCodeGenerationService.Verify(c => c.GenerateDtoClassesAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            namespaceName,
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task GenerateCodeAsync_WithNoDatabases_ShouldLogWarningAndReturn()
    {
        // Arrange
        var databasePath = @"C:\Databases";
        var outputPath = @"C:\Output";
        var namespaceName = "TestNamespace";
        var databases = new List<DatabaseConfiguration>();

        _mockDiscoveryService.Setup(d => d.DiscoverDatabasesAsync(databasePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(databases);

        // Act
        await _applicationService.GenerateCodeAsync(databasePath, outputPath, namespaceName);

        // Assert
        _mockDiscoveryService.Verify(d => d.DiscoverDatabasesAsync(databasePath, It.IsAny<CancellationToken>()), Times.Once);
        _mockCodeGenerationService.Verify(c => c.GenerateDtoClassesAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task GenerateCodeAsync_WithCodeGenerationException_ShouldThrowException()
    {
        // Arrange
        var databasePath = @"C:\Databases";
        var outputPath = @"C:\Output";
        var namespaceName = "TestNamespace";
        var databases = new List<DatabaseConfiguration>
        {
            new("DB1", "Data Source=db1.db")
        };
        var exception = new InvalidOperationException("Code generation failed");

        _mockDiscoveryService.Setup(d => d.DiscoverDatabasesAsync(databasePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(databases);

        _mockCodeGenerationService.Setup(c => c.GenerateDtoClassesAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act & Assert
        var action = async () => await _applicationService.GenerateCodeAsync(databasePath, outputPath, namespaceName);
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Code generation failed");
    }

    [TestMethod]
    public async Task ExportDatabasesAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var databasePath = @"C:\Databases";
        var outputPath = @"C:\Output";
        var databases = new List<DatabaseConfiguration>
        {
            new("DB1", "Data Source=db1.db")
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockDiscoveryService.Setup(d => d.DiscoverDatabasesAsync(databasePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(databases);

        // Act & Assert
        var action = async () => await _applicationService.ExportDatabasesAsync(databasePath, outputPath, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}
