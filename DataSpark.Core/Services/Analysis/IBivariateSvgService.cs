namespace DataSpark.Core.Services.Analysis;

/// <summary>
/// Generates SVG output for bivariate analysis visualizations.
/// </summary>
public interface IBivariateSvgService
{
    /// <summary>
    /// Builds a bivariate SVG chart for the requested columns.
    /// </summary>
    /// <param name="filePath">Absolute path to the CSV file.</param>
    /// <param name="column1">First selected column.</param>
    /// <param name="column2">Second selected column.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>SVG XML payload.</returns>
    Task<string> GenerateSvgAsync(string filePath, string column1, string column2, CancellationToken cancellationToken = default);
}
