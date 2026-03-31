using DataSpark.Core.Models;

namespace DataSpark.Core.Interfaces;

/// <summary>
/// Provides access to bundled sample CSV datasets.
/// </summary>
public interface ISampleDataService
{
    /// <summary>
    /// Gets all available sample CSV files.
    /// </summary>
    IReadOnlyList<SampleDataFile> GetSampleFiles();
}
