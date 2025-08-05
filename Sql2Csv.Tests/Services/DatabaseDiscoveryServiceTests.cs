using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Services;
using FluentAssertions;
using System.IO;

namespace Sql2Csv.Tests.Services;

[TestClass]
public class DatabaseDiscoveryServiceTests
{
    private Mock<ILogger<DatabaseDiscoveryService>> _mockLogger = null!;
    private DatabaseDiscoveryService _discoveryService = null!;
    private string _testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<DatabaseDiscoveryService>>();
        _discoveryService = new DatabaseDiscoveryService(_mockLogger.Object);
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
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DatabaseDiscoveryService(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [TestMethod]
    public async Task DiscoverDatabasesAsync_WithNullDirectoryPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _discoveryService.DiscoverDatabasesAsync(null!);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task DiscoverDatabasesAsync_WithEmptyDirectoryPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _discoveryService.DiscoverDatabasesAsync(string.Empty);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task DiscoverDatabasesAsync_WithWhitespaceDirectoryPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _discoveryService.DiscoverDatabasesAsync("   ");
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task DiscoverDatabasesAsync_WithNonExistentDirectory_ShouldReturnEmptyCollection()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = await _discoveryService.DiscoverDatabasesAsync(nonExistentPath);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task DiscoverDatabasesAsync_WithEmptyDirectory_ShouldReturnEmptyCollection()
    {
        // Act
        var result = await _discoveryService.DiscoverDatabasesAsync(_testDirectory);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task DiscoverDatabasesAsync_WithDatabaseFiles_ShouldReturnConfigurations()
    {
        // Arrange
        var dbFile1 = Path.Combine(_testDirectory, "database1.db");
        var dbFile2 = Path.Combine(_testDirectory, "database2.db");
        await File.WriteAllTextAsync(dbFile1, "dummy content");
        await File.WriteAllTextAsync(dbFile2, "dummy content");

        // Act
        var result = await _discoveryService.DiscoverDatabasesAsync(_testDirectory);

        // Assert
        result.Should().HaveCount(2);

        var resultList = result.ToList();
        resultList.Should().Contain(db => db.Name == "database1");
        resultList.Should().Contain(db => db.Name == "database2");
        resultList.Should().OnlyContain(db => db.Type == Sql2Csv.Core.Models.DatabaseType.SQLite);
        resultList.Should().OnlyContain(db => db.ConnectionString.StartsWith("Data Source="));
    }

    [TestMethod]
    public async Task DiscoverDatabasesAsync_WithMixedFiles_ShouldOnlyReturnDatabaseFiles()
    {
        // Arrange
        var dbFile = Path.Combine(_testDirectory, "database.db");
        var txtFile = Path.Combine(_testDirectory, "document.txt");
        var xlsFile = Path.Combine(_testDirectory, "spreadsheet.xlsx");

        await File.WriteAllTextAsync(dbFile, "dummy content");
        await File.WriteAllTextAsync(txtFile, "dummy content");
        await File.WriteAllTextAsync(xlsFile, "dummy content");

        // Act
        var result = await _discoveryService.DiscoverDatabasesAsync(_testDirectory);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("database");
    }

    [TestMethod]
    public async Task DiscoverDatabasesAsync_WithSubdirectories_ShouldOnlySearchTopLevel()
    {
        // Arrange
        var subDirectory = Path.Combine(_testDirectory, "subdirectory");
        Directory.CreateDirectory(subDirectory);

        var topLevelDb = Path.Combine(_testDirectory, "toplevel.db");
        var subDirDb = Path.Combine(subDirectory, "sublevel.db");

        await File.WriteAllTextAsync(topLevelDb, "dummy content");
        await File.WriteAllTextAsync(subDirDb, "dummy content");

        // Act
        var result = await _discoveryService.DiscoverDatabasesAsync(_testDirectory);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("toplevel");
    }

    [TestMethod]
    public async Task DiscoverDatabasesAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = async () => await _discoveryService.DiscoverDatabasesAsync(_testDirectory, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task DiscoverDatabasesAsync_WithValidDirectory_ShouldLogInformation()
    {
        // Arrange
        var dbFile = Path.Combine(_testDirectory, "test.db");
        await File.WriteAllTextAsync(dbFile, "dummy content");

        // Act
        await _discoveryService.DiscoverDatabasesAsync(_testDirectory);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Discovering databases in directory")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Discovered 1 databases")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task DiscoverDatabasesAsync_WithNonExistentDirectory_ShouldLogWarning()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        await _discoveryService.DiscoverDatabasesAsync(nonExistentPath);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Directory does not exist")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
