using System.Text;
using System.Text.Json;

namespace Sql2Csv.Core.Services.Export;

public interface IExportService
{
    byte[] ExportCsv(List<Dictionary<string, object>> data);
    byte[] ExportTsv(List<Dictionary<string, object>> data);
    byte[] ExportJson<TConfig>(List<Dictionary<string, object>> data, TConfig config);
}

public class ExportService : IExportService
{
    public byte[] ExportCsv(List<Dictionary<string, object>> data)
    {
        if (!data.Any()) return Array.Empty<byte>();
        var sb = new StringBuilder();
        var headers = data.First().Keys.ToList();
        sb.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));
        foreach (var row in data)
        {
            var values = headers.Select(h => $"\"{row.GetValueOrDefault(h, string.Empty)}\"");
            sb.AppendLine(string.Join(",", values));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] ExportTsv(List<Dictionary<string, object>> data)
    {
        if (!data.Any()) return Array.Empty<byte>();
        var sb = new StringBuilder();
        var headers = data.First().Keys.ToList();
        sb.AppendLine(string.Join("\t", headers));
        foreach (var row in data)
        {
            var values = headers.Select(h => row.GetValueOrDefault(h, string.Empty)?.ToString() ?? string.Empty);
            sb.AppendLine(string.Join("\t", values));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] ExportJson<TConfig>(List<Dictionary<string, object>> data, TConfig config)
    {
        var exportObject = new
        {
            Data = data,
            Configuration = config,
            ExportedAt = DateTime.UtcNow,
            RecordCount = data.Count
        };
        var json = JsonSerializer.Serialize(exportObject, new JsonSerializerOptions { WriteIndented = true });
        return Encoding.UTF8.GetBytes(json);
    }
}