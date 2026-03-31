namespace DataSpark.Core.Models;

/// <summary>
/// Represents a bundled sample CSV file.
/// </summary>
public sealed record SampleDataFile
{
    public required string FileName { get; init; }

    public required string FullPath { get; init; }

    public long FileSizeBytes { get; init; }

    public bool IsReadOnly { get; init; } = true;
}
