using System.IO.Compression;
using System.Text;
using DataSpark.Core.Interfaces;
using DataSpark.Core.Models;

namespace DataSpark.Core.Services;

/// <summary>
/// Packages exported results into downloadable ZIP archives.
/// </summary>
public sealed class ExportPackagingService : IExportPackagingService
{
    /// <inheritdoc />
    public byte[] PackageAsZip(IEnumerable<ExportResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var result in results.Where(r => r.IsSuccess))
            {
                var entry = archive.CreateEntry(result.FileName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                writer.Write(result.FileContent);
            }
        }

        return memoryStream.ToArray();
    }
}
