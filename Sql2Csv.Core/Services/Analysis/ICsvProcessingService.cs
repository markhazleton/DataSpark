using Microsoft.Data.Analysis;
using Sql2Csv.Core.Models.Analysis;

namespace Sql2Csv.Core.Services.Analysis;

/// <summary>
/// Abstraction for processing CSV files into analytical representations (DataFrame, statistics, visual encodings).
/// </summary>
public interface ICsvProcessingService
{
    Task<CsvViewModel> ProcessCsvWithFallbackAsync(string fileName, char delimiter = ',');
    Task<string> ProcessCsvToJsonAsync(string fileName, bool useSafeMethod, char delimiter = ',');
    string ProcessCsvToJson(string filePath, bool useSafeMethod, char delimiter = ',');
    string GetScottPlotSvg(string columnName, System.Data.DataTable dataTable);
    string GetScottBarPlotSvg(string columnName, System.Data.DataTable dataTable);
}