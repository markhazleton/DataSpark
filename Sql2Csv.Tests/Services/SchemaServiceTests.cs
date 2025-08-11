using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sql2Csv.Core.Configuration;
using Sql2Csv.Core.Services;
using Sql2Csv.Tests.Infrastructure;
using FluentAssertions;

namespace Sql2Csv.Tests.Services;

[TestClass]
public class SchemaServiceTests : DatabaseTestBase
{
    private Mock<ILogger<SchemaService>> _mockLogger = null!;
    private Mock<IOptions<Sql2CsvOptions>> _mockOptions = null!;
    private SchemaService _schemaService = null!;
    private Sql2CsvOptions _options = null!;

    [TestInitialize]
    public override async Task TestInitialize()
    {
        await base.TestInitialize();

        _mockLogger = new Mock<ILogger<SchemaService>>();
        _mockOptions = new Mock<IOptions<Sql2CsvOptions>>();

        _options = new Sql2CsvOptions
        {
            Database = new DatabaseOptions { Timeout = 300 }
        };

        _mockOptions.Setup(o => o.Value).Returns(_options);
        _schemaService = new SchemaService(_mockLogger.Object, _mockOptions.Object);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new SchemaService(null!, _mockOptions.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [TestMethod]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new SchemaService(_mockLogger.Object, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [TestMethod]
    public async Task GetTablesAsync_WithValidConnection_ShouldReturnAllTables()
    {
        // Act
        var tables = await _schemaService.GetTablesAsync(ConnectionString);

        // Assert
        var tableList = tables.ToList();
        tableList.Should().HaveCount(3);

        var tableNames = tableList.Select(t => t.Name).ToList();
        tableNames.Should().Contain("Users");
        tableNames.Should().Contain("Orders");
        tableNames.Should().Contain("Products");

        // Verify table details
        var usersTable = tableList.First(t => t.Name == "Users");
        usersTable.Columns.Should().NotBeEmpty();
        usersTable.Schema.Should().Be("main");
    }

    [TestMethod]
    public async Task GetTablesAsync_WithNullConnectionString_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _schemaService.GetTablesAsync(null!);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task GetTablesAsync_WithEmptyConnectionString_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _schemaService.GetTablesAsync(string.Empty);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task GetTablesAsync_WithWhitespaceConnectionString_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _schemaService.GetTablesAsync("   ");
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task GetTableNamesAsync_WithValidConnection_ShouldReturnTableNames()
    {
        // Act
        var tableNames = await _schemaService.GetTableNamesAsync(ConnectionString);

        // Assert
        var nameList = tableNames.ToList();
        nameList.Should().HaveCount(3);
        nameList.Should().Contain("Users");
        nameList.Should().Contain("Orders");
        nameList.Should().Contain("Products");
    }

    [TestMethod]
    public async Task GetTableNamesAsync_WithInvalidConnection_ShouldThrowException()
    {
        // Act & Assert
        var action = async () => await _schemaService.GetTableNamesAsync("Invalid Connection String");
        await action.Should().ThrowAsync<Exception>();
    }

    [TestMethod]
    public async Task GetTableColumnsAsync_WithValidTable_ShouldReturnColumns()
    {
        // Act
        var columns = await _schemaService.GetTableColumnsAsync(ConnectionString, "Users");

        // Assert
        var columnList = columns.ToList();
        columnList.Should().HaveCount(5);

        var columnNames = columnList.Select(c => c.Name).ToList();
        columnNames.Should().Contain("Id");
        columnNames.Should().Contain("Name");
        columnNames.Should().Contain("Email");
        columnNames.Should().Contain("Age");
        columnNames.Should().Contain("CreatedDate");

        // Verify primary key
        var idColumn = columnList.First(c => c.Name == "Id");
        idColumn.IsPrimaryKey.Should().BeTrue();
        idColumn.DataType.Should().Be("INTEGER");
        idColumn.IsNullable.Should().BeFalse();

        // Verify nullable column
        var emailColumn = columnList.First(c => c.Name == "Email");
        emailColumn.IsNullable.Should().BeTrue();
        emailColumn.IsPrimaryKey.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetTableColumnsAsync_WithNonExistentTable_ShouldReturnEmptyCollection()
    {
        // Act
        var columns = await _schemaService.GetTableColumnsAsync(ConnectionString, "NonExistentTable");

        // Assert
        columns.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetTableColumnsAsync_WithNullTableName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _schemaService.GetTableColumnsAsync(ConnectionString, null!);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task GenerateSchemaReportAsync_WithValidConnection_ShouldReturnReport()
    {
        // Act
        var report = await _schemaService.GenerateSchemaReportAsync(ConnectionString);

        // Assert
        report.Should().NotBeNullOrEmpty();
        report.Should().Contain("Database Schema Report");
        report.Should().Contain("Users");
        report.Should().Contain("Orders");
        report.Should().Contain("Products");

        // Should contain column information
        report.Should().Contain("Id");
        report.Should().Contain("Name");
        report.Should().Contain("Email");

        // Should contain data types
        report.Should().Contain("INTEGER");
        report.Should().Contain("TEXT");

        // Should contain nullability info
        report.Should().Contain("NOT NULL");

        // Should contain primary key info
        report.Should().Contain("PRIMARY KEY");
    }

    [TestMethod]
    public async Task GenerateSchemaReportAsync_WithInvalidConnection_ShouldThrowException()
    {
        // Act & Assert
        var action = async () => await _schemaService.GenerateSchemaReportAsync("Invalid Connection String");
        await action.Should().ThrowAsync<Exception>();
    }

    [TestMethod]
    public async Task GetTablesAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = async () => await _schemaService.GetTablesAsync(ConnectionString, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task GetTableNamesAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = async () => await _schemaService.GetTableNamesAsync(ConnectionString, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task GetTableColumnsAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = async () => await _schemaService.GetTableColumnsAsync(ConnectionString, "Users", cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task GenerateSchemaReportAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = async () => await _schemaService.GenerateSchemaReportAsync(ConnectionString, cancellationToken: cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task GetTablesAsync_ShouldIncludeRowCounts()
    {
        // Act
        var tables = await _schemaService.GetTablesAsync(ConnectionString);

        // Assert
        var tableList = tables.ToList();

        var usersTable = tableList.First(t => t.Name == "Users");
        usersTable.RowCount.Should().Be(3);

        var ordersTable = tableList.First(t => t.Name == "Orders");
        ordersTable.RowCount.Should().Be(4);

        var productsTable = tableList.First(t => t.Name == "Products");
        productsTable.RowCount.Should().Be(3);
    }

    [TestMethod]
    public async Task GenerateSchemaReportAsync_WithJsonFormat_ShouldProduceValidJson()
    {
        // Act
        var report = await _schemaService.GenerateSchemaReportAsync(ConnectionString, format: "json");

        // Assert
        report.Should().StartWith("[").And.EndWith("]");
        report.Should().Contain("\"table\": \"Users\"");
        report.Should().Contain("\"columns\"");
        // Basic JSON parse
        Action parse = () => { _ = System.Text.Json.JsonSerializer.Deserialize<object>(report)!; };
        parse.Should().NotThrow();
    }

    [TestMethod]
    public async Task GenerateSchemaReportAsync_WithMarkdownFormat_ShouldContainTableSections()
    {
        // Act
        var report = await _schemaService.GenerateSchemaReportAsync(ConnectionString, format: "markdown");

        // Assert
        report.Should().Contain("# Database Schema Report");
        report.Should().Contain("## Table: Users");
        report.Should().Contain("| Column | Type | Nullable | PK | Default |");
        report.Should().Contain("| Id | INTEGER | No | Yes");
    }
}
