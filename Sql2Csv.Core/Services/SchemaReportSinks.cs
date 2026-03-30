using Microsoft.Extensions.Logging;
using Sql2Csv.Core.Interfaces;

namespace Sql2Csv.Core.Services;

/// <summary>
/// Simple sink that logs schema reports.
/// </summary>
public sealed class LoggingSchemaReportSink : ISchemaReportSink
{
    private readonly ILogger<LoggingSchemaReportSink> _logger;
    public LoggingSchemaReportSink(ILogger<LoggingSchemaReportSink> logger) => _logger = logger;

    public Task WriteReportAsync(string databaseName, string format, string reportContent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Schema report for {Database} ({Format})\n{Report}", databaseName, format, reportContent);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Persists schema reports to disk.
/// </summary>
public sealed class FileSchemaReportSink : ISchemaReportSink
{
    private readonly string _baseDirectory;
    private readonly ILogger<FileSchemaReportSink> _logger;
    public FileSchemaReportSink(string baseDirectory, ILogger<FileSchemaReportSink> logger)
    {
        _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory) ? "schema-reports" : baseDirectory;
        _logger = logger;
    }

    public async Task WriteReportAsync(string databaseName, string format, string reportContent, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_baseDirectory);
        var ext = format switch { "json" => "json", "markdown" => "md", _ => "txt" };
        var filePath = Path.Combine(_baseDirectory, $"{databaseName}_schema.{ext}");
        await File.WriteAllTextAsync(filePath, reportContent, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Wrote schema report to {Path}", filePath);
    }
}

/// <summary>
/// Fans out to multiple sinks.
/// </summary>
public sealed class CompositeSchemaReportSink : ISchemaReportSink
{
    private readonly IEnumerable<ISchemaReportSink> _sinks;
    public CompositeSchemaReportSink(IEnumerable<ISchemaReportSink> sinks) => _sinks = sinks;
    public Task WriteReportAsync(string databaseName, string format, string reportContent, CancellationToken cancellationToken = default)
        => Task.WhenAll(_sinks.Select(s => s.WriteReportAsync(databaseName, format, reportContent, cancellationToken)));
}
