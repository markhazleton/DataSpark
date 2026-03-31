using Microsoft.Extensions.Options;
using DataSpark.Core.Interfaces;

namespace DataSpark.Core.Configuration;

/// <summary>
/// File storage configuration options
/// </summary>
public class FileStorageOptions : IFileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Gets or sets the directory path for persisted files
    /// </summary>
    public string PersistedDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum file age before cleanup (defaults to 30 days)
    /// </summary>
    public TimeSpan MaxFileAge { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Gets or sets the maximum storage size in bytes (defaults to 1GB)
    /// </summary>
    public long MaxStorageSizeBytes { get; set; } = 1024L * 1024 * 1024; // 1GB
}

/// <summary>
/// Options wrapper for IFileStorageOptions
/// </summary>
public class FileStorageOptionsWrapper : IFileStorageOptions
{
    private readonly FileStorageOptions _options;

    public FileStorageOptionsWrapper(IOptions<FileStorageOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        // Set default directory if not configured
        if (string.IsNullOrEmpty(_options.PersistedDirectory))
        {
            _options.PersistedDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "DataSpark", 
                "PersistedDatabases");
        }
    }

    public string PersistedDirectory => _options.PersistedDirectory;
    public TimeSpan MaxFileAge => _options.MaxFileAge;
    public long MaxStorageSizeBytes => _options.MaxStorageSizeBytes;
}
