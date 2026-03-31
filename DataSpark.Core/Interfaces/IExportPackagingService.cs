using DataSpark.Core.Models;

namespace DataSpark.Core.Interfaces;

/// <summary>
/// Service for packaging exported results into downloadable archives.
/// </summary>
public interface IExportPackagingService
{
    /// <summary>
    /// Packages a collection of export results into a ZIP archive.
    /// </summary>
    /// <param name="results">The export results to package.</param>
    /// <returns>The ZIP archive as a byte array.</returns>
    byte[] PackageAsZip(IEnumerable<ExportResult> results);
}
