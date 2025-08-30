using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Models;
using System.Globalization;
using System.Text;

namespace Sql2Csv.Core.Services;

/// <summary>
/// Service for analyzing CSV files.
/// </summary>
public class CsvAnalysisService : ICsvAnalysisService
{
    private readonly ILogger<CsvAnalysisService> _logger;

    public CsvAnalysisService(ILogger<CsvAnalysisService> logger)
    {
        _logger = logger;
    }

    public async Task<CsvAnalysisResult> AnalyzeCsvAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting CSV analysis for file: {FilePath}", filePath);

        var result = new CsvAnalysisResult
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            FileSize = new FileInfo(filePath).Length
        };

        try
        {
            // Detect delimiter and encoding
            var (delimiter, encoding, hasHeaders) = await DetectCsvFormatAsync(filePath, cancellationToken);
            result.Delimiter = delimiter;
            result.Encoding = encoding.EncodingName;
            result.HasHeaders = hasHeaders;

            // Configure CSV reader
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                HasHeaderRecord = hasHeaders,
                BadDataFound = null, // Ignore bad data for analysis
                MissingFieldFound = null // Ignore missing fields for analysis
            };

            using var reader = new StringReader(await File.ReadAllTextAsync(filePath, encoding, cancellationToken));
            using var csv = new CsvReader(reader, config);

            // Read headers or generate them
            List<string> headers;
            if (hasHeaders)
            {
                await csv.ReadAsync();
                csv.ReadHeader();
                headers = csv.HeaderRecord?.ToList() ?? new List<string>();
            }
            else
            {
                // For files without headers, read first row to determine column count
                if (await csv.ReadAsync())
                {
                    var recordLength = csv.Parser.Count;
                    headers = Enumerable.Range(1, recordLength).Select(i => $"Column_{i}").ToList();
                }
                else
                {
                    headers = new List<string>();
                }
            }

            result.ColumnCount = headers.Count;

            // Initialize column analyses
            var columnAnalyses = headers.Select((header, index) => new CsvColumnAnalysis
            {
                ColumnName = header,
                ColumnIndex = index,
                DataType = "TEXT",
                SampleValues = new List<string>()
            }).ToList();

            result.ColumnAnalyses = columnAnalyses;

            // Analyze data
            long rowCount = 0;
            var sampleLimit = 100; // Limit sample values per column
            
            if (hasHeaders)
            {
                await csv.ReadAsync(); // Skip header row if we read it already
            }

            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                rowCount++;

                for (int i = 0; i < columnAnalyses.Count; i++)
                {
                    var value = csv.GetField(i) ?? string.Empty;
                    var analysis = columnAnalyses[i];

                    if (string.IsNullOrEmpty(value))
                    {
                        analysis.NullCount++;
                    }
                    else
                    {
                        analysis.NonNullCount++;
                        
                        // Add to sample values if we haven't reached the limit
                        if (analysis.SampleValues.Count < sampleLimit && !analysis.SampleValues.Contains(value))
                        {
                            analysis.SampleValues.Add(value);
                        }

                        // Try to refine data type detection
                        if (analysis.DataType == "TEXT")
                        {
                            analysis.DataType = DetectDataType(value);
                        }
                    }
                }

                // Limit analysis to prevent memory issues with very large files
                if (rowCount >= 100000)
                {
                    _logger.LogWarning("CSV analysis limited to first 100,000 rows for performance");
                    break;
                }
            }

            result.RowCount = rowCount;

            // Calculate additional statistics for numeric columns
            await CalculateNumericStatisticsAsync(result, filePath, cancellationToken);

            // Calculate unique counts (sample-based for large files)
            foreach (var analysis in result.ColumnAnalyses)
            {
                analysis.UniqueCount = analysis.SampleValues.Count;
            }

            _logger.LogInformation("CSV analysis completed. Rows: {RowCount}, Columns: {ColumnCount}", 
                result.RowCount, result.ColumnCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing CSV file: {FilePath}", filePath);
            result.Errors.Add($"Analysis error: {ex.Message}");
        }

        return result;
    }

    public async Task<IEnumerable<ColumnInfo>> GetCsvColumnsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var analysis = await AnalyzeCsvAsync(filePath, cancellationToken);
        
        return analysis.ColumnAnalyses.Select(col => new ColumnInfo
        {
            Name = col.ColumnName,
            DataType = col.DataType,
            IsNullable = col.NullCount > 0,
            IsPrimaryKey = false // CSV files don't have primary keys
        });
    }

    public async Task<CsvDataResult> GetCsvDataAsync(string filePath, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting CSV data: {FilePath}, Skip: {Skip}, Take: {Take}", filePath, skip, take);

        var result = new CsvDataResult
        {
            StartIndex = skip
        };

        try
        {
            // Detect format first
            var (delimiter, encoding, hasHeaders) = await DetectCsvFormatAsync(filePath, cancellationToken);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                HasHeaderRecord = hasHeaders,
                BadDataFound = null,
                MissingFieldFound = null
            };

            using var reader = new StringReader(await File.ReadAllTextAsync(filePath, encoding, cancellationToken));
            using var csv = new CsvReader(reader, config);

            // Read headers
            if (hasHeaders)
            {
                await csv.ReadAsync();
                csv.ReadHeader();
                result.Columns = csv.HeaderRecord?.ToList() ?? new List<string>();
            }
            else
            {
                // For files without headers, read first row to determine column count
                if (await csv.ReadAsync())
                {
                    var recordLength = csv.Parser.Count;
                    result.Columns = Enumerable.Range(1, recordLength).Select(i => $"Column_{i}").ToList();
                    
                    // Read the current record as the first data row
                    var firstRowData = new List<string>();
                    for (int i = 0; i < recordLength; i++)
                    {
                        firstRowData.Add(csv.GetField(i) ?? string.Empty);
                    }
                    result.Rows.Add(firstRowData);
                }
                else
                {
                    result.Columns = new List<string>();
                }
            }

            // Position reader and read data rows
            int rowsToSkip = skip;
            int rowsToRead = take;
            
            // For files without headers, if we already read the first row, account for it
            if (!hasHeaders && result.Rows.Count > 0)
            {
                // We already have the first row, so adjust our skip/take
                if (skip == 0)
                {
                    // We want the first rows and already have the first one
                    rowsToRead = take - 1; // Read one less since we already have one
                }
                else
                {
                    // We need to skip some rows, and we already have one
                    rowsToSkip = skip - 1; // Skip one less since we already consumed one
                    result.Rows.Clear(); // Clear the pre-read row since we're skipping it
                }
            }

            // Skip the required number of data rows
            for (int i = 0; i < rowsToSkip && await csv.ReadAsync(); i++)
            {
                // Just skip these rows
            }

            // Read the requested number of data rows
            int actualRowsRead = hasHeaders ? 0 : result.Rows.Count; // Count any pre-read rows
            while (actualRowsRead < take && await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var rowData = new List<string>();
                for (int i = 0; i < result.Columns.Count; i++)
                {
                    rowData.Add(csv.GetField(i) ?? string.Empty);
                }
                result.Rows.Add(rowData);
                actualRowsRead++;
            }

            result.ReturnedRows = result.Rows.Count;
            
            // Get total row count (this is expensive for large files, consider caching)
            result.TotalRows = await GetTotalRowCountAsync(filePath, hasHeaders, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CSV data: {FilePath}", filePath);
            throw;
        }

        return result;
    }

    private async Task<(string delimiter, Encoding encoding, bool hasHeaders)> DetectCsvFormatAsync(string filePath, CancellationToken cancellationToken)
    {
        // Read first few lines to detect format
        var sampleLines = new List<string>();
        using var reader = new StreamReader(filePath);
        
        for (int i = 0; i < 5 && !reader.EndOfStream; i++)
        {
            var line = await reader.ReadLineAsync();
            if (!string.IsNullOrEmpty(line))
            {
                sampleLines.Add(line);
            }
        }

        if (!sampleLines.Any())
        {
            return (",", Encoding.UTF8, false);
        }

        // Detect delimiter
        var delimiters = new[] { ",", ";", "\t", "|" };
        var delimiter = ",";
        int maxColumns = 0;

        foreach (var testDelimiter in delimiters)
        {
            var columnCount = sampleLines.First().Split(testDelimiter).Length;
            if (columnCount > maxColumns)
            {
                maxColumns = columnCount;
                delimiter = testDelimiter;
            }
        }

        // Simple header detection: check if first row has different characteristics than subsequent rows
        var hasHeaders = false;
        if (sampleLines.Count > 1)
        {
            var firstRowParts = sampleLines[0].Split(delimiter);
            var secondRowParts = sampleLines[1].Split(delimiter);

            // If all values in first row are non-numeric and second row has some numeric values, likely headers
            if (firstRowParts.All(part => !double.TryParse(part, out _)) && 
                secondRowParts.Any(part => double.TryParse(part, out _)))
            {
                hasHeaders = true;
            }
        }

        return (delimiter, Encoding.UTF8, hasHeaders);
    }

    private string DetectDataType(string value)
    {
        if (int.TryParse(value, out _)) return "INTEGER";
        if (double.TryParse(value, out _)) return "REAL";
        if (DateTime.TryParse(value, out _)) return "DATETIME";
        if (bool.TryParse(value, out _)) return "BOOLEAN";
        return "TEXT";
    }

    private async Task CalculateNumericStatisticsAsync(CsvAnalysisResult result, string filePath, CancellationToken cancellationToken)
    {
        // For numeric columns, calculate mean and standard deviation
        var numericColumns = result.ColumnAnalyses
            .Where(col => col.DataType == "INTEGER" || col.DataType == "REAL")
            .ToList();

        if (!numericColumns.Any()) return;

        try
        {
            var (delimiter, encoding, hasHeaders) = await DetectCsvFormatAsync(filePath, cancellationToken);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                HasHeaderRecord = hasHeaders,
                BadDataFound = null,
                MissingFieldFound = null
            };

            using var reader = new StringReader(await File.ReadAllTextAsync(filePath, encoding, cancellationToken));
            using var csv = new CsvReader(reader, config);

            if (hasHeaders)
            {
                await csv.ReadAsync();
                csv.ReadHeader();
            }

            // Collect values for numeric columns
            var columnValues = numericColumns.ToDictionary(col => col.ColumnIndex, _ => new List<double>());

            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var numericCol in numericColumns)
                {
                    var value = csv.GetField(numericCol.ColumnIndex);
                    if (!string.IsNullOrWhiteSpace(value) && double.TryParse(value, out var numValue))
                    {
                        columnValues[numericCol.ColumnIndex].Add(numValue);
                    }
                }
            }

            // Calculate statistics
            foreach (var numericCol in numericColumns)
            {
                var values = columnValues[numericCol.ColumnIndex];
                if (values.Any())
                {
                    numericCol.Mean = values.Average();
                    numericCol.StandardDeviation = Math.Sqrt(values.Select(v => Math.Pow(v - numericCol.Mean.Value, 2)).Average());
                    numericCol.MinValue = values.Min().ToString();
                    numericCol.MaxValue = values.Max().ToString();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating numeric statistics for CSV file: {FilePath}", filePath);
        }
    }

    private async Task<long> GetTotalRowCountAsync(string filePath, bool hasHeaders, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            long count = 0;
            
            while (!reader.EndOfStream)
            {
                await reader.ReadLineAsync();
                count++;
                
                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            return hasHeaders ? count - 1 : count;
        }
        catch
        {
            return 0; // Return 0 if we can't count rows
        }
    }
}
