using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.Analysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sql2Csv.Core.Models;
using System.Globalization;
using System.Text;

namespace Sql2Csv.Core.Services;

public class CsvFileService
{
    private readonly string _defaultFilesPath;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CsvFileService> _logger;

    public CsvFileService(IConfiguration configuration, ILogger<CsvFileService> logger, string? basePath = null)
    {
        _configuration = configuration;
        _logger = logger;
        _defaultFilesPath = basePath ?? AppContext.BaseDirectory;
    }

    public string GetCsvFilesPath()
    {
        var outputFolder = _configuration["CsvOutputFolder"];
        if (!string.IsNullOrEmpty(outputFolder)) return outputFolder;
        var path = Path.Combine(_defaultFilesPath, "files");
        return path;
    }

    public List<string> GetCsvFileNames()
    {
        var filesPath = GetCsvFilesPath();
        if (!Directory.Exists(filesPath))
        {
            _logger.LogWarning("CSV files directory does not exist: {Path}", filesPath);
            return new List<string>();
        }
        return Directory.GetFiles(filesPath, "*.csv")
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Select(f => f!)
            .ToList();
    }

    public string? GetFilePath(string fileName)
    {
        if (!IsValidFileName(fileName)) return null;
        var filesPath = GetCsvFilesPath();
        if (!Directory.Exists(filesPath)) return null;
        var filePath = Path.Combine(filesPath, fileName);
        return File.Exists(filePath) ? filePath : null;
    }

    public bool FileExists(string fileName) => GetFilePath(fileName) != null;

    private string SanitizeForExcel(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (value.StartsWith("=") || value.StartsWith("+") || value.StartsWith("-") || value.StartsWith("@"))
            return "'" + value;
        return value;
    }

    public async Task<CsvOperationResult<string>> GetCsvHeadersAsync(string fileName, char delimiter = ',', Encoding? encoding = null)
    {
        var result = new CsvOperationResult<string>();
        if (!IsValidFileName(fileName)) { result.ErrorMessage = "Invalid file name."; return result; }
        var filePath = GetFilePath(fileName);
        if (filePath == null) { result.ErrorMessage = "File not found."; return result; }
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = delimiter.ToString() });
            if (await csv.ReadAsync().ConfigureAwait(false))
            {
                csv.ReadHeader();
                result.Data = csv.HeaderRecord?.ToList() ?? new List<string>();
                result.Success = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading headers from CSV file: {FileName}", fileName);
            result.ErrorMessage = ex.Message;
        }
        return result;
    }

    public async Task<CsvOperationResult<dynamic>> ReadCsvRecordsAsync(string fileName, char delimiter = ',', Encoding? encoding = null)
    {
        var result = new CsvOperationResult<dynamic>();
        if (!IsValidFileName(fileName)) { result.ErrorMessage = "Invalid file name."; return result; }
        var filePath = GetFilePath(fileName);
        if (filePath == null) { result.ErrorMessage = "File not found."; return result; }
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = delimiter.ToString() });
            await foreach (var record in csv.GetRecordsAsync<dynamic>().ConfigureAwait(false)) { result.Data.Add(record); }
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading CSV records from file: {FileName}", fileName);
            result.ErrorMessage = ex.Message;
        }
        return result;
    }

    public async Task<CsvOperationResult<T>> ReadCsvRecordsAsync<T>(string fileName, char delimiter = ',', Encoding? encoding = null)
    {
        var result = new CsvOperationResult<T>();
        if (!IsValidFileName(fileName)) { result.ErrorMessage = "Invalid file name."; return result; }
        var filePath = GetFilePath(fileName);
        if (filePath == null) { result.ErrorMessage = "File not found."; return result; }
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = delimiter.ToString() });
            await foreach (var record in csv.GetRecordsAsync<T>().ConfigureAwait(false)) { result.Data.Add(record); }
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading CSV records from file: {FileName}", fileName);
            result.ErrorMessage = ex.Message;
        }
        return result;
    }

    public async Task<CsvOperationResult<DataFrame>> ReadCsvAsDataFrameAsync(string fileName, char delimiter = ',', Encoding? encoding = null, bool allString = false)
    {
        var result = new CsvOperationResult<DataFrame>();
        if (!IsValidFileName(fileName)) { result.ErrorMessage = "Invalid file name."; return result; }
        var filePath = GetFilePath(fileName);
        if (filePath == null) { result.ErrorMessage = "File not found."; return result; }
        try
        {
            var df = await Task.Run(() =>
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (allString)
                {
                    int colCount = CsvProcessingUtils.GetColumnCount(filePath);
                    return DataFrame.LoadCsv(stream, separator: delimiter, header: true, dataTypes: Enumerable.Repeat(typeof(string), colCount).ToArray(), encoding: encoding ?? Encoding.UTF8, cultureInfo: CultureInfo.InvariantCulture);
                }
                else
                {
                    try
                    {
                        return DataFrame.LoadCsv(stream, separator: delimiter, header: true, encoding: encoding ?? Encoding.UTF8, cultureInfo: CultureInfo.InvariantCulture);
                    }
                    catch (FormatException)
                    {
                        stream.Position = 0;
                        int colCount = CsvProcessingUtils.GetColumnCount(filePath);
                        return DataFrame.LoadCsv(stream, separator: delimiter, header: true, dataTypes: Enumerable.Repeat(typeof(string), colCount).ToArray(), encoding: encoding ?? Encoding.UTF8, cultureInfo: CultureInfo.InvariantCulture);
                    }
                }
            }).ConfigureAwait(false);
            result.Data.Add(df); result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading DataFrame from CSV file: {FileName}", fileName);
            result.ErrorMessage = ex.Message; result.Data.Add(new DataFrame()); result.Success = false;
        }
        return result;
    }

    public async Task<CsvVisualizationResult> ReadCsvForVisualizationAsync(string fileName, bool sanitizeForExcel = false)
    {
        var recordsResult = await ReadCsvRecordsAsync<dynamic>(fileName).ConfigureAwait(false);
        if (!recordsResult.Success || !recordsResult.Data.Any())
            return new CsvVisualizationResult { Headers = new List<string>(), Columns = new Dictionary<string, List<string>>(), Records = new List<dynamic>() };
        var records = recordsResult.Data;
        var headers = ((IDictionary<string, object>)records[0]).Keys.ToList();
        var columns = new Dictionary<string, List<string>>();
        foreach (var header in headers)
        {
            columns[header] = records.Select(r => sanitizeForExcel ? SanitizeForExcel(((IDictionary<string, object>)r)[header]?.ToString() ?? string.Empty) : ((IDictionary<string, object>)r)[header]?.ToString() ?? string.Empty).ToList();
        }
        return new CsvVisualizationResult { Headers = headers, Columns = columns, Records = records };
    }

    public CsvVisualizationResult ReadCsvForVisualization(string fileName, bool sanitizeForExcel = false) => ReadCsvForVisualizationAsync(fileName, sanitizeForExcel).GetAwaiter().GetResult();
    public List<dynamic> ReadCsvRecords(string fileName) => ReadCsvRecordsAsync(fileName).GetAwaiter().GetResult().Data;
    public List<T> ReadCsvRecords<T>(string fileName) => ReadCsvRecordsAsync<T>(fileName).GetAwaiter().GetResult().Data;

    private bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
        if (fileName.Contains("..") || fileName.Contains(Path.DirectorySeparatorChar) || fileName.Contains(Path.AltDirectorySeparatorChar)) return false;
        return true;
    }
}

public class CsvVisualizationResult
{
    public List<string> Headers { get; set; } = new();
    public Dictionary<string, List<string>> Columns { get; set; } = new();
    public List<dynamic> Records { get; set; } = new();
}

public class CsvOperationResult<T>
{
    public bool Success { get; set; }
    public List<T> Data { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
