using DataSpark.Core.Interfaces;
using DataSpark.Core.Models;
using DataSpark.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DataSpark.Tests.Services;

[TestClass]
public class DatabaseDiscoverySummaryServiceTests
{
    private Mock<IDatabaseDiscoveryService> _mockDiscovery = null!;
    private Mock<ISchemaService> _mockSchema = null!;
    private Mock<ILogger<DatabaseDiscoverySummaryService>> _mockLogger = null!;
    private DatabaseDiscoverySummaryService _service = null!;
    private string _testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockDiscovery = new Mock<IDatabaseDiscoveryService>();
        _mockSchema = new Mock<ISchemaService>();
        _mockLogger = new Mock<ILogger<DatabaseDiscoverySummaryService>>();
        _service = new DatabaseDiscoverySummaryService(_mockDiscovery.Object, _mockSchema.Object, _mockLogger.Object);
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [TestMethod]
    public void Constructor_WithNullDiscoveryService_ShouldThrowArgumentNullException()
    {
        var action = () => new DatabaseDiscoverySummaryService(null!, _mockSchema.Object, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("discoveryService");
    }

    [TestMethod]
    public void Constructor_WithNullSchemaService_ShouldThrowArgumentNullException()
    {
        var action = () => new DatabaseDiscoverySummaryService(_mockDiscovery.Object, null!, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("schemaService");
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        var action = () => new DatabaseDiscoverySummaryService(_mockDiscovery.Object, _mockSchema.Object, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [TestMethod]
    public async Task ScanAsync_WithNullPath_ShouldThrowArgumentException()
    {
        var action = async () => await _service.ScanAsync(null!);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task ScanAsync_WithEmptyPath_ShouldThrowArgumentException()
    {
        var action = async () => await _service.ScanAsync(string.Empty);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task ScanAsync_WithNoDatabases_ShouldReturnEmptyResult()
    {
        _mockDiscovery
            .Setup(d => d.DiscoverDatabasesAsync(_testDirectory, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<DatabaseConfiguration>());

        var result = await _service.ScanAsync(_testDirectory);

        result.Databases.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ScanAsync_WithDatabases_ShouldReturnSummaries()
    {
        var dbPath = Path.Combine(_testDirectory, "test.db");
        File.WriteAllBytes(dbPath, new byte[1024]);

        var config = new DatabaseConfiguration("test", $"Data Source={dbPath}");
        _mockDiscovery
            .Setup(d => d.DiscoverDatabasesAsync(_testDirectory, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { config });
        _mockSchema
            .Setup(s => s.GetTableNamesAsync($"Data Source={dbPath}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "Table1", "Table2" });

        var result = await _service.ScanAsync(_testDirectory);

        result.Databases.Should().HaveCount(1);
        result.Databases[0].Path.Should().Be(dbPath);
        result.Databases[0].SizeBytes.Should().Be(1024);
        result.Databases[0].TableCount.Should().Be(2);
    }

    [TestMethod]
    public async Task ScanAsync_WithDuplicates_ShouldDeduplicateByConnectionString()
    {
        var dbPath = Path.Combine(_testDirectory, "test.db");
        File.WriteAllBytes(dbPath, new byte[512]);

        var config = new DatabaseConfiguration("test", $"Data Source={dbPath}");
        _mockDiscovery
            .Setup(d => d.DiscoverDatabasesAsync(_testDirectory, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { config, config });
        _mockSchema
            .Setup(s => s.GetTableNamesAsync($"Data Source={dbPath}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "T1" });

        var result = await _service.ScanAsync(_testDirectory);

        result.Databases.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task ScanAsync_Recursive_ShouldScanSubdirectories()
    {
        var subDir = Path.Combine(_testDirectory, "sub");
        Directory.CreateDirectory(subDir);

        var dbPath1 = Path.Combine(_testDirectory, "a.db");
        var dbPath2 = Path.Combine(subDir, "b.db");
        File.WriteAllBytes(dbPath1, new byte[100]);
        File.WriteAllBytes(dbPath2, new byte[200]);

        var config1 = new DatabaseConfiguration("a", $"Data Source={dbPath1}");
        var config2 = new DatabaseConfiguration("b", $"Data Source={dbPath2}");

        _mockDiscovery
            .Setup(d => d.DiscoverDatabasesAsync(_testDirectory, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { config1 });
        _mockDiscovery
            .Setup(d => d.DiscoverDatabasesAsync(subDir, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { config2 });

        _mockSchema
            .Setup(s => s.GetTableNamesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "T1" });

        var result = await _service.ScanAsync(_testDirectory, recursive: true);

        result.Databases.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task ScanAsync_WithCancellation_ShouldThrowOperationCancelledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var action = async () => await _service.ScanAsync(_testDirectory, cancellationToken: cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}
