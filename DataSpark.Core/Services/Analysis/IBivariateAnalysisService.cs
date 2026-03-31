using DataSpark.Core.Models.Analysis;

namespace DataSpark.Core.Services.Analysis;

/// <summary>
/// Provides core bivariate analysis operations for CSV-backed datasets.
/// </summary>
public interface IBivariateAnalysisService
{
    /// <summary>
    /// Analyzes the relationship between two columns and returns structured statistical output.
    /// </summary>
    /// <param name="filePath">Absolute path to the CSV file.</param>
    /// <param name="column1">First selected column.</param>
    /// <param name="column2">Second selected column.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Computed bivariate analysis result.</returns>
    Task<BivariateAnalysisResult> AnalyzeAsync(string filePath, string column1, string column2, CancellationToken cancellationToken = default);
}
