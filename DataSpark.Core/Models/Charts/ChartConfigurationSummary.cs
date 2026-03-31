namespace DataSpark.Core.Models.Charts;

/// <summary>
/// Lightweight summary/projection of a <see cref="ChartConfiguration"/> used for listings & selection UIs.
/// </summary>
public class ChartConfigurationSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ChartType { get; set; } = string.Empty;
    public string DataSource { get; set; } = string.Empty;
    public string? CsvFile { get; set; }
    public int SeriesCount { get; set; }
    public int FilterCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
}
