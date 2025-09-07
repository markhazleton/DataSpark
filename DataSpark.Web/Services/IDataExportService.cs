using System.Collections.Generic;

namespace DataSpark.Web.Services
{
    public interface IDataExportService
    {
        byte[] ExportCsv(List<Dictionary<string, object>> data);
        byte[] ExportTsv(List<Dictionary<string, object>> data);
        byte[] ExportJson<T>(List<Dictionary<string, object>> data, T config);
    }
}
