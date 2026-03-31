using DataSpark.Core.Models;
using DataSpark.Core.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataSpark.Tests.Services;

[TestClass]
public class ExportPackagingServiceTests
{
    private ExportPackagingService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new ExportPackagingService();
    }

    [TestMethod]
    public void PackageAsZip_WithNullResults_ShouldThrowArgumentNullException()
    {
        var action = () => _service.PackageAsZip(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void PackageAsZip_WithEmptyResults_ShouldReturnValidEmptyZip()
    {
        var results = Enumerable.Empty<ExportResult>();

        var zip = _service.PackageAsZip(results);

        zip.Should().NotBeNull();
        zip.Length.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void PackageAsZip_WithSuccessfulResults_ShouldContainEntries()
    {
        var results = new[]
        {
            new ExportResult
            {
                DatabaseName = "test",
                TableName = "Users",
                FileName = "Users.csv",
                FileContent = "Id,Name\n1,Alice\n2,Bob",
                IsSuccess = true,
                RowCount = 2
            },
            new ExportResult
            {
                DatabaseName = "test",
                TableName = "Orders",
                FileName = "Orders.csv",
                FileContent = "Id,Total\n1,100",
                IsSuccess = true,
                RowCount = 1
            }
        };

        var zip = _service.PackageAsZip(results);

        zip.Should().NotBeNull();
        zip.Length.Should().BeGreaterThan(0);

        // Verify it's a valid ZIP by reading it back
        using var stream = new MemoryStream(zip);
        using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
        archive.Entries.Should().HaveCount(2);
        archive.Entries.Select(e => e.Name).Should().Contain("Users.csv").And.Contain("Orders.csv");
    }

    [TestMethod]
    public void PackageAsZip_ShouldExcludeFailedResults()
    {
        var results = new[]
        {
            new ExportResult
            {
                DatabaseName = "test",
                TableName = "Good",
                FileName = "Good.csv",
                FileContent = "col\nval",
                IsSuccess = true,
                RowCount = 1
            },
            new ExportResult
            {
                DatabaseName = "test",
                TableName = "Bad",
                FileName = "Bad.csv",
                FileContent = "",
                IsSuccess = false,
                ErrorMessage = "Export failed"
            }
        };

        var zip = _service.PackageAsZip(results);

        using var stream = new MemoryStream(zip);
        using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
        archive.Entries.Should().HaveCount(1);
        archive.Entries[0].Name.Should().Be("Good.csv");
    }

    [TestMethod]
    public void PackageAsZip_ShouldPreserveFileContent()
    {
        var content = "Id,Name\n1,Alice\n2,Bob";
        var results = new[]
        {
            new ExportResult
            {
                DatabaseName = "test",
                TableName = "Users",
                FileName = "Users.csv",
                FileContent = content,
                IsSuccess = true,
                RowCount = 2
            }
        };

        var zip = _service.PackageAsZip(results);

        using var stream = new MemoryStream(zip);
        using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);
        using var entryStream = archive.Entries[0].Open();
        using var reader = new StreamReader(entryStream);
        var readContent = reader.ReadToEnd();
        readContent.Should().Be(content);
    }
}
