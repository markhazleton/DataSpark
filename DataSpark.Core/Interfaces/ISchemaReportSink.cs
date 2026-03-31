using System.Threading;
using System.Threading.Tasks;

namespace DataSpark.Core.Interfaces;

/// <summary>
/// Abstraction for writing / emitting schema reports produced by <see cref="ISchemaService"/>.
/// Allows callers to plug in logging, file persistence, aggregation or forwarding behaviors.
/// </summary>
public interface ISchemaReportSink
{
    /// <summary>
    /// Writes a schema report.
    /// </summary>
    /// <param name="databaseName">Logical database name.</param>
    /// <param name="format">Report format (text/json/markdown/custom).</param>
    /// <param name="reportContent">Rendered report body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteReportAsync(string databaseName, string format, string reportContent, CancellationToken cancellationToken = default);
}
