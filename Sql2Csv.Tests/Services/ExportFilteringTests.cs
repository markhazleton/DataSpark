using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Sql2Csv.Core.Configuration;
using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Models;
using Sql2Csv.Core.Services;

namespace Sql2Csv.Tests.Services;

[TestClass]
public class ExportFilteringTests
{
    [TestMethod]
    public async Task ExportDatabaseToCsvAsync_FiltersTables()
    {
        // Arrange
        var schemaSvc = new Mock<ISchemaService>();
        schemaSvc.Setup(s => s.GetTableNamesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<string> { "Users", "Orders", "Products" });

        var exportSvc = new ExportService(NullLogger<ExportService>.Instance, schemaSvc.Object, Options.Create(new Sql2CsvOptions()));
        var db = new DatabaseConfiguration { Name = "TestDb", ConnectionString = "Data Source=:memory:" };
        var filter = new[] { "users", "products" };

        // Act
        var results = await exportSvc.ExportDatabaseToCsvAsync(db, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), filter, null, null, CancellationToken.None);

        // Assert
        // Only Users and Products should be attempted
        Assert.IsTrue(results.All(r => r.TableName is "Users" or "Products"));
    }
}
