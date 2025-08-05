using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sql2Csv.Core.Configuration;
using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Models;
using Sql2Csv.Core.Services;
using FluentAssertions;
using System.IO;

namespace Sql2Csv.Tests.Services;

[TestClass]
public class CodeGenerationServiceTests
{
    private Mock<ILogger<CodeGenerationService>> _mockLogger = null!;
    private Mock<ISchemaService> _mockSchemaService = null!;
    private Mock<IOptions<Sql2CsvOptions>> _mockOptions = null!;
    private CodeGenerationService _codeGenerationService = null!;
    private Sql2CsvOptions _options = null!;
    private string _outputDirectory = null!;
    private string _testConnectionString = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILogger<CodeGenerationService>>();
        _mockSchemaService = new Mock<ISchemaService>();
        _mockOptions = new Mock<IOptions<Sql2CsvOptions>>();

        _options = new Sql2CsvOptions
        {
            Database = new DatabaseOptions { Timeout = 300 }
        };

        _mockOptions.Setup(o => o.Value).Returns(_options);
        _testConnectionString = "Data Source=test.db";

        // Setup mock schema service with test data
        var testTables = new List<TableInfo>
        {
            new()
            {
                Name = "Users",
                Schema = "main",
                Columns = new List<ColumnInfo>
                {
                    new() { Name = "Id", DataType = "INTEGER", IsPrimaryKey = true, IsNullable = false },
                    new() { Name = "Name", DataType = "TEXT", IsPrimaryKey = false, IsNullable = false },
                    new() { Name = "Email", DataType = "TEXT", IsPrimaryKey = false, IsNullable = true },
                    new() { Name = "Age", DataType = "INTEGER", IsPrimaryKey = false, IsNullable = true },
                    new() { Name = "CreatedDate", DataType = "TEXT", IsPrimaryKey = false, IsNullable = true, DefaultValue = "CURRENT_TIMESTAMP" }
                }.AsReadOnly(),
                RowCount = 3
            },
            new()
            {
                Name = "Orders",
                Schema = "main",
                Columns = new List<ColumnInfo>
                {
                    new() { Name = "Id", DataType = "INTEGER", IsPrimaryKey = true, IsNullable = false },
                    new() { Name = "UserId", DataType = "INTEGER", IsPrimaryKey = false, IsNullable = true },
                    new() { Name = "Amount", DataType = "REAL", IsPrimaryKey = false, IsNullable = true },
                    new() { Name = "OrderDate", DataType = "TEXT", IsPrimaryKey = false, IsNullable = true, DefaultValue = "CURRENT_TIMESTAMP" }
                }.AsReadOnly(),
                RowCount = 4
            }
        };

        _mockSchemaService.Setup(s => s.GetTablesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testTables);

        _codeGenerationService = new CodeGenerationService(_mockLogger.Object, _mockSchemaService.Object, _mockOptions.Object);

        _outputDirectory = Path.Combine(Path.GetTempPath(), $"test_codegen_{Guid.NewGuid()}");
        Directory.CreateDirectory(_outputDirectory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_outputDirectory))
        {
            try
            {
                Directory.Delete(_outputDirectory, true);
            }
            catch (IOException)
            {
                // Ignore cleanup failures
            }
        }
    }
    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CodeGenerationService(null!, _mockSchemaService.Object, _mockOptions.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [TestMethod]
    public void Constructor_WithNullSchemaService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CodeGenerationService(_mockLogger.Object, null!, _mockOptions.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("schemaService");
    }

    [TestMethod]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CodeGenerationService(_mockLogger.Object, _mockSchemaService.Object, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [TestMethod]
    public async Task GenerateDtoClassesAsync_WithValidParameters_ShouldGenerateClasses()
    {
        // Arrange
        var namespaceName = "TestNamespace.Models";

        // Act
        await _codeGenerationService.GenerateDtoClassesAsync(_testConnectionString, _outputDirectory, namespaceName);

        // Assert
        var usersFile = Path.Combine(_outputDirectory, "Users.cs");
        var ordersFile = Path.Combine(_outputDirectory, "Orders.cs");

        File.Exists(usersFile).Should().BeTrue();
        File.Exists(ordersFile).Should().BeTrue();

        // Verify Users class content
        var usersContent = await File.ReadAllTextAsync(usersFile);
        usersContent.Should().Contain($"namespace {namespaceName}");
        usersContent.Should().Contain("public class Users");
        usersContent.Should().Contain("public int Id { get; set; }");
        usersContent.Should().Contain("public string Name { get; set; }");
        usersContent.Should().Contain("public string? Email { get; set; }");
        usersContent.Should().Contain("public int? Age { get; set; }");
        usersContent.Should().Contain("public string? CreatedDate { get; set; }");

        // Verify Orders class content
        var ordersContent = await File.ReadAllTextAsync(ordersFile);
        ordersContent.Should().Contain($"namespace {namespaceName}");
        ordersContent.Should().Contain("public class Orders");
        ordersContent.Should().Contain("public int Id { get; set; }");
        ordersContent.Should().Contain("public int? UserId { get; set; }");
        ordersContent.Should().Contain("public double? Amount { get; set; }");
        ordersContent.Should().Contain("public string? OrderDate { get; set; }");
    }

    [TestMethod]
    public async Task GenerateDtoClassesAsync_WithNullConnectionString_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _codeGenerationService.GenerateDtoClassesAsync(null!, _outputDirectory, "TestNamespace");
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task GenerateDtoClassesAsync_WithEmptyConnectionString_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _codeGenerationService.GenerateDtoClassesAsync(string.Empty, _outputDirectory, "TestNamespace");
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task GenerateDtoClassesAsync_WithNullOutputDirectory_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _codeGenerationService.GenerateDtoClassesAsync(_testConnectionString, null!, "TestNamespace");
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task GenerateDtoClassesAsync_WithEmptyOutputDirectory_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _codeGenerationService.GenerateDtoClassesAsync(_testConnectionString, string.Empty, "TestNamespace");
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task GenerateDtoClassesAsync_WithNullNamespace_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _codeGenerationService.GenerateDtoClassesAsync(_testConnectionString, _outputDirectory, null!);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task GenerateDtoClassesAsync_WithEmptyNamespace_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _codeGenerationService.GenerateDtoClassesAsync(_testConnectionString, _outputDirectory, string.Empty);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task GenerateDtoClassesAsync_WithNonExistentOutputDirectory_ShouldCreateDirectory()
    {
        // Arrange
        var newDirectory = Path.Combine(_outputDirectory, "NewSubDir", "Models");
        var namespaceName = "TestNamespace.Models";

        // Act
        await _codeGenerationService.GenerateDtoClassesAsync(_testConnectionString, newDirectory, namespaceName);

        // Assert
        Directory.Exists(newDirectory).Should().BeTrue();

        var usersFile = Path.Combine(newDirectory, "Users.cs");
        File.Exists(usersFile).Should().BeTrue();
    }

    [TestMethod]
    public async Task GenerateDtoClassesAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = async () => await _codeGenerationService.GenerateDtoClassesAsync(
            _testConnectionString, _outputDirectory, "TestNamespace", cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task GenerateDtoClassesAsync_ShouldCallSchemaService()
    {
        // Act
        await _codeGenerationService.GenerateDtoClassesAsync(_testConnectionString, _outputDirectory, "TestNamespace");

        // Assert
        _mockSchemaService.Verify(s => s.GetTablesAsync(_testConnectionString, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task GenerateDtoClassesAsync_WithComplexNamespace_ShouldUseCorrectNamespace()
    {
        // Arrange
        var namespaceName = "MyCompany.MyProject.DataModels.Entities";

        // Act
        await _codeGenerationService.GenerateDtoClassesAsync(_testConnectionString, _outputDirectory, namespaceName);

        // Assert
        var usersFile = Path.Combine(_outputDirectory, "Users.cs");
        var usersContent = await File.ReadAllTextAsync(usersFile);
        usersContent.Should().Contain($"namespace {namespaceName}");
    }

    [TestMethod]
    public async Task GenerateDtoClassesAsync_ShouldGenerateValidCSharpCode()
    {
        // Arrange
        var namespaceName = "TestNamespace.Models";

        // Act
        await _codeGenerationService.GenerateDtoClassesAsync(_testConnectionString, _outputDirectory, namespaceName);

        // Assert
        var usersFile = Path.Combine(_outputDirectory, "Users.cs");
        var usersContent = await File.ReadAllTextAsync(usersFile);

        // Verify basic C# syntax
        usersContent.Should().Contain("using System;");
        usersContent.Should().Contain("{");
        usersContent.Should().Contain("}");
        usersContent.Should().Contain("public class");
        usersContent.Should().Contain("{ get; set; }");

        // Should not contain invalid characters or malformed syntax
        usersContent.Should().NotContain("<<");
        usersContent.Should().NotContain(">>");
        usersContent.Should().NotContain("${");
    }

    [TestMethod]
    public async Task GenerateDtoClassesAsync_WithNoTables_ShouldNotCreateFiles()
    {
        // Arrange
        _mockSchemaService.Setup(s => s.GetTablesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TableInfo>());

        // Act
        await _codeGenerationService.GenerateDtoClassesAsync(_testConnectionString, _outputDirectory, "TestNamespace");

        // Assert
        var files = Directory.GetFiles(_outputDirectory, "*.cs");
        files.Should().BeEmpty();
    }
}
