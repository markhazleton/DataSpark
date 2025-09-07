using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;

namespace DataSpark.Web.Services
{
    public class DataExportService : IDataExportService
    {
        public byte[] ExportCsv(List<Dictionary<string, object>> data)
        {
            return ExportToDelimitedString(data, ",");
        }

        public byte[] ExportTsv(List<Dictionary<string, object>> data)
        {
            return ExportToDelimitedString(data, "\t");
        }

        public byte[] ExportJson<T>(List<Dictionary<string, object>> data, T config)
        {
            var jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            return Encoding.UTF8.GetBytes(jsonString);
        }

        private byte[] ExportToDelimitedString(List<Dictionary<string, object>> data, string delimiter)
        {
            if (data == null || !data.Any())
            {
                return Array.Empty<byte>();
            }

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter
            };
            using var csv = new CsvWriter(writer, csvConfig);

            var headers = data.First().Keys;
            foreach (var header in headers)
            {
                csv.WriteField(header);
            }
            csv.NextRecord();

            foreach (var record in data)
            {
                foreach (var header in headers)
                {
                    csv.WriteField(record.ContainsKey(header) ? record[header] : string.Empty);
                }
                csv.NextRecord();
            }

            writer.Flush();
            return memoryStream.ToArray();
        }
    }
}
