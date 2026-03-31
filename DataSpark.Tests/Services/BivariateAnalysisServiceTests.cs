using DataSpark.Core.Services.Analysis;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DataSpark.Tests.Services;

[TestClass]
public class BivariateAnalysisServiceTests
{
    private Mock<ILogger<BivariateAnalysisService>> _mockLogger = null!;
    private BivariateAnalysisService _service = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockLogger = new Mock<ILogger<BivariateAnalysisService>>();
        _service = new BivariateAnalysisService(_mockLogger.Object);
    }

    [TestMethod]
    public async Task AnalyzeAsync_WithNumericColumns_ShouldReturnScatterAndRegression()
    {
        var filePath = CreateTempCsv(
            "A,B,Category",
            "1,2,X",
            "2,4,X",
            "3,6,Y",
            "4,8,Y");

        try
        {
            var result = await _service.AnalyzeAsync(filePath, "A", "B");

            result.Column1.Should().Be("A");
            result.Column2.Should().Be("B");
            result.Scatter.Should().NotBeNull();
            result.Scatter!.Count.Should().Be(4);
            result.Regression.Should().NotBeNull();
            result.Correlation.Should().NotBeNull();
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [TestMethod]
    public async Task AnalyzeAsync_WithMissingColumn_ShouldThrowInvalidOperationException()
    {
        var filePath = CreateTempCsv(
            "A,B",
            "1,2",
            "2,3");

        try
        {
            var action = async () => await _service.AnalyzeAsync(filePath, "A", "Missing");
            await action.Should().ThrowAsync<InvalidOperationException>();
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateTempCsv(params string[] lines)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"bivariate-test-{Guid.NewGuid():N}.csv");
        File.WriteAllLines(filePath, lines);
        return filePath;
    }
}
