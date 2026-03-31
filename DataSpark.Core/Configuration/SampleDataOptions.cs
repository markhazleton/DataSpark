namespace DataSpark.Core.Configuration;

/// <summary>
/// Configuration options for bundled sample datasets.
/// </summary>
public sealed class SampleDataOptions
{
    public const string SectionName = "SampleData";

    /// <summary>
    /// Gets or sets the absolute or relative path to the sample-data directory.
    /// </summary>
    public string Path { get; set; } = "wwwroot/sample-data";
}
