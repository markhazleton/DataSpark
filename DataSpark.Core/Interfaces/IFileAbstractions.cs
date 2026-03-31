namespace DataSpark.Core.Interfaces;

/// <summary>
/// Abstraction for uploaded file information to avoid web dependencies in Core
/// </summary>
public interface IUploadedFileInfo
{
    /// <summary>
    /// Gets the original filename
    /// </summary>
    string FileName { get; }

    /// <summary>
    /// Gets the file size in bytes
    /// </summary>
    long Length { get; }

    /// <summary>
    /// Opens a stream to read the file contents
    /// </summary>
    /// <returns>A stream for reading the file</returns>
    Stream OpenReadStream();

    /// <summary>
    /// Copies the file contents to the specified stream
    /// </summary>
    /// <param name="target">The target stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the copy operation</returns>
    Task CopyToAsync(Stream target, CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration options for file storage
/// </summary>
public interface IFileStorageOptions
{
    /// <summary>
    /// Gets the directory path for persisted files
    /// </summary>
    string PersistedDirectory { get; }

    /// <summary>
    /// Gets the maximum file age before cleanup
    /// </summary>
    TimeSpan MaxFileAge { get; }

    /// <summary>
    /// Gets the maximum storage size in bytes
    /// </summary>
    long MaxStorageSizeBytes { get; }
}
