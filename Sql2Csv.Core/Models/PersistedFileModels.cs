using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sql2Csv.Core.Models;

/// <summary>
/// Model representing a persisted database file
/// </summary>
public record PersistedDatabaseFile
{
    public required string Id { get; init; }
    public required string OriginalFileName { get; init; }
    public required string StoredFilePath { get; init; }
    public required DateTime UploadedAt { get; init; }
    public required long FileSizeBytes { get; init; }
    public int TableCount { get; init; }
    public string? Description { get; init; }
    public DateTime LastAccessedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the display name for the file
    /// </summary>
    [JsonIgnore]
    public string DisplayName => !string.IsNullOrEmpty(Description) ? Description : OriginalFileName;

    /// <summary>
    /// Gets the formatted file size
    /// </summary>
    [JsonIgnore]
    public string FormattedFileSize
    {
        get
        {
            if (FileSizeBytes < 1024)
                return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1024 * 1024)
                return $"{FileSizeBytes / 1024.0:F1} KB";
            return $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";
        }
    }

    /// <summary>
    /// Gets the formatted upload date
    /// </summary>
    [JsonIgnore]
    public string FormattedUploadDate
    {
        get
        {
            var timeSpan = DateTime.UtcNow - UploadedAt;
            if (timeSpan.Days > 0)
                return $"{timeSpan.Days} day{(timeSpan.Days == 1 ? "" : "s")} ago";
            if (timeSpan.Hours > 0)
                return $"{timeSpan.Hours} hour{(timeSpan.Hours == 1 ? "" : "s")} ago";
            if (timeSpan.Minutes > 0)
                return $"{timeSpan.Minutes} minute{(timeSpan.Minutes == 1 ? "" : "s")} ago";
            return "Just now";
        }
    }
}

/// <summary>
/// Model for updating file description
/// </summary>
public class UpdateFileDescriptionModel
{
    [Required]
    public required string FileId { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }
}

/// <summary>
/// Result of file validation operations
/// </summary>
public record FileValidationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int TableCount { get; init; }

    public static FileValidationResult Successful(int tableCount) => new()
    {
        Success = true,
        TableCount = tableCount
    };

    public static FileValidationResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
