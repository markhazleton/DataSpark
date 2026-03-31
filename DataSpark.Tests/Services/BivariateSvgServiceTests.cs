using DataSpark.Core.Services.Analysis;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DataSpark.Tests.Services;

[TestClass]
public class BivariateSvgServiceTests
{
    private Mock<ILogger<BivariateSvgService>> _mockLogger = null!;
    private BivariateSvgService _service = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILogger<BivariateSvgService>>();
        _service = new BivariateSvgService(_mockLogger.Object);
    }

    [TestMethod]
    public async Task GenerateSvgAsync_WithNumericColumns_ShouldReturnSvgMarkup()
    {
        var filePath = CreateTempCsv(
            "A,B",
            "1,2",
            "2,4",
            "3,6",
            "4,8");

        try
        {
            var svg = await _service.GenerateSvgAsync(filePath, "A", "B");

            svg.Should().Contain("<svg");
            svg.Should().Contain("with trend line");
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [TestMethod]
    public async Task GenerateSvgAsync_WithMissingColumn_ShouldThrowInvalidOperationException()
    {
        var filePath = CreateTempCsv(
            "A,B",
            "1,2",
            "2,3");

        try
        {
            var action = async () => await _service.GenerateSvgAsync(filePath, "A", "Missing");
            await action.Should().ThrowAsync<InvalidOperationException>();
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [TestMethod]
    public async Task GenerateSvgAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        var filePath = CreateTempCsv(
            "A,B",
            "1,2",
            "2,4");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            var action = async () => await _service.GenerateSvgAsync(filePath, "A", "B", cts.Token);
            await action.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateTempCsv(params string[] lines)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"bivariate-svg-test-{Guid.NewGuid():N}.csv");
        File.WriteAllLines(filePath, lines);
        return filePath;
    }
}
