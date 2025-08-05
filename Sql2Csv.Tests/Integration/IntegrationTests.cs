using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sql2Csv.Core.Configuration;
using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Services;
using Sql2Csv.Tests.Infrastructure;
using FluentAssertions;
using System.IO;

namespace Sql2Csv.Tests.Integration;

[TestClass]
public class IntegrationTests : DatabaseTestBase
{
    private ServiceProvider _serviceProvider = null!;
    private string _outputDirectory = null!;

    [TestInitialize]
    public override async Task TestInitialize()
    {
        await base.TestInitialize();

        // Setup configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sql2Csv:Database:Timeout"] = "300",
                ["Sql2Csv:Export:IncludeHeaders"] = "true",
                ["Sql2Csv:Export:Delimiter"] = ",",
                ["Sql2Csv:Export:Encoding"] = "UTF-8"
            })
            .Build();

        // Setup DI container
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.Configure<Sql2CsvOptions>(configuration.GetSection("Sql2Csv"));

        // Register services
        services.AddScoped<IDatabaseDiscoveryService, DatabaseDiscoveryService>();
        services.AddScoped<ISchemaService, SchemaService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<ICodeGenerationService, CodeGenerationService>();
        services.AddScoped<ApplicationService>();

        _serviceProvider = services.BuildServiceProvider();

        _outputDirectory = Path.Combine(Path.GetTempPath(), $"integration_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_outputDirectory);
    }

    [TestCleanup]
    public override void TestCleanup()
    {
        base.TestCleanup();
        _serviceProvider?.Dispose();

        if (Directory.Exists(_outputDirectory))
        {
            Directory.Delete(_outputDirectory, true);
        }
    }

    [TestMethod]
    public async Task FullWorkflow_DatabaseDiscoveryToExport_ShouldWorkEndToEnd()
    {
        // Arrange
        var databaseDirectory = Path.GetDirectoryName(TestDatabasePath)!;
        var applicationService = _serviceProvider.GetRequiredService<ApplicationService>();

        // Act
        await applicationService.ExportDatabasesAsync(databaseDirectory, _outputDirectory);

        // Assert
        var testDbDirectory = Path.Combine(_outputDirectory, Path.GetFileNameWithoutExtension(TestDatabasePath));
        Directory.Exists(testDbDirectory).Should().BeTrue();

        var csvFiles = Directory.GetFiles(testDbDirectory, "*.csv");
        csvFiles.Should().HaveCount(3);

        var usersCsv = csvFiles.First(f => Path.GetFileName(f).StartsWith("Users"));
        var csvContent = await File.ReadAllTextAsync(usersCsv);
        csvContent.Should().Contain("Id,Name,Email,Age,CreatedDate");
        csvContent.Should().Contain("John Doe");
    }

    [TestMethod]
    public async Task SchemaReportGeneration_ShouldGenerateReport()
    {
        // Arrange
        var databaseDirectory = Path.GetDirectoryName(TestDatabasePath)!;
        var applicationService = _serviceProvider.GetRequiredService<ApplicationService>();

        // Act & Assert (Should not throw)
        await applicationService.GenerateSchemaReportsAsync(databaseDirectory);
    }

    [TestMethod]
    public async Task CodeGeneration_ShouldGenerateDtoClasses()
    {
        // Arrange
        var databaseDirectory = Path.GetDirectoryName(TestDatabasePath)!;
        var applicationService = _serviceProvider.GetRequiredService<ApplicationService>();
        var namespaceName = "TestNamespace.Models";

        // Act
        await applicationService.GenerateCodeAsync(databaseDirectory, _outputDirectory, namespaceName);

        // Assert
        var testDbDirectory = Path.Combine(_outputDirectory, Path.GetFileNameWithoutExtension(TestDatabasePath));
        Directory.Exists(testDbDirectory).Should().BeTrue();

        var csFiles = Directory.GetFiles(testDbDirectory, "*.cs");
        csFiles.Should().HaveCount(3);

        var usersCs = csFiles.First(f => Path.GetFileName(f) == "Users.cs");
        var classContent = await File.ReadAllTextAsync(usersCs);
        classContent.Should().Contain($"namespace {namespaceName}");
        classContent.Should().Contain("public class Users");
    }

    [TestMethod]
    public async Task ExportService_WithDifferentDelimiter_ShouldUseCorrectDelimiter()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sql2Csv:Export:Delimiter"] = ";"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<Sql2CsvOptions>(configuration.GetSection("Sql2Csv"));
        services.AddScoped<ISchemaService, SchemaService>();
        services.AddScoped<IExportService, ExportService>();

        using var serviceProvider = services.BuildServiceProvider();
        var exportService = serviceProvider.GetRequiredService<IExportService>();

        var databaseConfig = new Sql2Csv.Core.Models.DatabaseConfiguration("TestDB", ConnectionString);
        var outputFilePath = Path.Combine(_outputDirectory, "users_semicolon.csv");

        // Act
        var result = await exportService.ExportTableToCsvAsync(databaseConfig, "Users", outputFilePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var csvContent = await File.ReadAllTextAsync(outputFilePath);
        csvContent.Should().Contain("Id;Name;Email;Age;CreatedDate");
    }

    [TestMethod]
    public async Task DatabaseDiscoveryService_WithMultipleDatabases_ShouldDiscoverAll()
    {
        // Arrange
        var testDirectory = Path.Combine(_outputDirectory, "databases");
        Directory.CreateDirectory(testDirectory);

        // Create additional test databases
        var db1Path = Path.Combine(testDirectory, "test1.db");
        var db2Path = Path.Combine(testDirectory, "test2.db");

        File.Copy(TestDatabasePath, db1Path);
        File.Copy(TestDatabasePath, db2Path);

        var discoveryService = _serviceProvider.GetRequiredService<IDatabaseDiscoveryService>();

        // Act
        var databases = await discoveryService.DiscoverDatabasesAsync(testDirectory);

        // Assert
        databases.Should().HaveCount(2);
        databases.Should().Contain(db => db.Name == "test1");
        databases.Should().Contain(db => db.Name == "test2");
    }

    [TestMethod]
    public async Task SchemaService_WithRealDatabase_ShouldReturnCorrectSchema()
    {
        // Arrange
        var schemaService = _serviceProvider.GetRequiredService<ISchemaService>();

        // Act
        var tables = await schemaService.GetTablesAsync(ConnectionString);

        // Assert
        var tableList = tables.ToList();
        tableList.Should().HaveCount(3);

        var usersTable = tableList.First(t => t.Name == "Users");
        usersTable.Columns.Should().HaveCount(5);
        usersTable.RowCount.Should().Be(3);

        var idColumn = usersTable.Columns.First(c => c.Name == "Id");
        idColumn.IsPrimaryKey.Should().BeTrue();
        idColumn.DataType.Should().Be("INTEGER");
    }

    [TestMethod]
    public void ServiceRegistration_ShouldResolveAllServices()
    {
        // Act & Assert
        _serviceProvider.GetRequiredService<IDatabaseDiscoveryService>().Should().NotBeNull();
        _serviceProvider.GetRequiredService<ISchemaService>().Should().NotBeNull();
        _serviceProvider.GetRequiredService<IExportService>().Should().NotBeNull();
        _serviceProvider.GetRequiredService<ICodeGenerationService>().Should().NotBeNull();
        _serviceProvider.GetRequiredService<ApplicationService>().Should().NotBeNull();
    }

    [TestMethod]
    public void Configuration_ShouldBindCorrectly()
    {
        // Act
        var options = _serviceProvider.GetRequiredService<IOptions<Sql2CsvOptions>>().Value;

        // Assert
        options.Should().NotBeNull();
        options.Database.Timeout.Should().Be(300);
        options.Export.IncludeHeaders.Should().BeTrue();
        options.Export.Delimiter.Should().Be(",");
        options.Export.Encoding.Should().Be("UTF-8");
    }
}
