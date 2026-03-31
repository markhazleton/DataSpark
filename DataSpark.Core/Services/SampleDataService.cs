using DataSpark.Core.Configuration;
using DataSpark.Core.Interfaces;
using DataSpark.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataSpark.Core.Services;

/// <summary>
/// Lists bundled sample CSV files from a configured directory.
/// </summary>
public sealed class SampleDataService : ISampleDataService
{
    private readonly ILogger<SampleDataService> _logger;
    private readonly SampleDataOptions _options;

    public SampleDataService(ILogger<SampleDataService> logger, IOptions<SampleDataOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public IReadOnlyList<SampleDataFile> GetSampleFiles()
    {
        if (string.IsNullOrWhiteSpace(_options.Path))
        {
            return [];
        }

        var resolvedPath = Path.GetFullPath(_options.Path);
        if (!Directory.Exists(resolvedPath))
        {
            _logger.LogWarning("Sample data directory does not exist: {Path}", resolvedPath);
            return [];
        }

        var files = Directory
            .GetFiles(resolvedPath, "*.csv", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderBy(info => info.Name, StringComparer.OrdinalIgnoreCase)
            .Select(info => new SampleDataFile
            {
                FileName = info.Name,
                FullPath = info.FullName,
                FileSizeBytes = info.Length,
                IsReadOnly = true
            })
            .ToList();

        return files;
    }
}
