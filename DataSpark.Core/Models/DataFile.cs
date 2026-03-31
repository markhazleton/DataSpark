namespace DataSpark.Core.Models;

/// <summary>
/// Supported uploaded file types.
/// </summary>
public enum DataFileType
{
    Csv,
    Sqlite
}

/// <summary>
/// Represents an uploaded or bundled data file.
/// </summary>
public sealed class DataFile
{
    public string FileName { get; set; } = string.Empty;

    public DataFileType FileType { get; set; }

    public long FileSize { get; set; }

    public DateTime UploadDate { get; set; } = DateTime.UtcNow;

    public long RowCount { get; set; }

    public int ColumnCount { get; set; }

    public string StoragePath { get; set; } = string.Empty;

    public DateTime? RetentionExpiry { get; set; }

    public bool IsReadOnly { get; set; }

    public string? Description { get; set; }
}
