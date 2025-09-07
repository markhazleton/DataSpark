namespace Sql2Csv.Core.Models;

public class UploadedCsvFile
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OriginalFilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long FileSizeBytes { get; set; }
}
