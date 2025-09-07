using Microsoft.Data.Analysis;
using Microsoft.Extensions.Logging;
using Sql2Csv.Core.Models.Analysis;
using System.Data;
using System.Text.Json;

namespace Sql2Csv.Core.Services.Analysis;

/// <summary>
/// Core implementation for CSV analytical processing. Rendering-specific concerns remain in web.
/// </summary>
public class CsvProcessingService : ICsvProcessingService
{
    private readonly ICsvFileReader _csvFileReader;
    private readonly ILogger<CsvProcessingService> _logger;

    public CsvProcessingService(ICsvFileReader csvFileReader, ILogger<CsvProcessingService> logger)
    {
        _csvFileReader = csvFileReader ?? throw new ArgumentNullException(nameof(csvFileReader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CsvViewModel> ProcessCsvWithFallbackAsync(string fileName, char delimiter = ',')
    {
        try
        {
            return await ProcessCsvInternalAsync(fileName, useSafeMethod: false, delimiter).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary CSV processing failed for {File}. Falling back to safe mode.", fileName);
            return await ProcessCsvInternalAsync(fileName, useSafeMethod: true, delimiter).ConfigureAwait(false);
        }
    }

    public async Task<string> ProcessCsvToJsonAsync(string fileName, bool useSafeMethod, char delimiter = ',')
    {
        var dfResult = await _csvFileReader.ReadCsvAsDataFrameAsync(fileName, delimiter, useSafeMethod).ConfigureAwait(false);
        if (!dfResult.Success || dfResult.Data.Count == 0 || dfResult.Data[0] == null)
            return "[]";
        return DataFrameToJson(dfResult.Data[0]);
    }

    public string ProcessCsvToJson(string filePath, bool useSafeMethod, char delimiter = ',') =>
        ProcessCsvToJsonAsync(filePath, useSafeMethod, delimiter).GetAwaiter().GetResult();

    public string GetScottPlotSvg(string columnName, DataTable dataTable)
    {
        var columnData = dataTable.AsEnumerable().Select(r => r[columnName]?.ToString()).ToList();
        var isNumeric = columnData.All(v => double.TryParse(v, out _));
        var plt = new ScottPlot.Plot();

        if (isNumeric)
        {
            var data = dataTable.AsEnumerable().Select(r => Convert.ToDouble(r[columnName])).ToArray();
            double min = data.Min();
            double max = data.Max();
            double median = Median(data);
            double q1 = Quantile(data, 0.25);
            double q3 = Quantile(data, 0.75);

            ScottPlot.Box box = new()
            {
                Position = 1,
                BoxMin = q1,
                BoxMax = q3,
                WhiskerMin = min,
                WhiskerMax = max,
                BoxMiddle = median
            };
            plt.Add.Box(box);
            plt.Title($"Box Plot of {columnName}");
            plt.XLabel(columnName);
            plt.YLabel("Values");
        }
        else
        {
            var counts = columnData.GroupBy(x => x).ToDictionary(g => g.Key ?? string.Empty, g => g.Count())
                                    .OrderByDescending(k => k.Value).ToList();
            var bars = counts.Select((kvp, idx) => new ScottPlot.Bar { Position = idx, Value = kvp.Value, Label = kvp.Key }).ToArray();
            plt.Add.Bars(bars);
            plt.Title($"Frequency Count of {columnName}");
            plt.XLabel("Category");
            plt.YLabel("Count");
        }
        return plt.GetSvgXml(600, 400);
    }

    public string GetScottBarPlotSvg(string columnName, DataTable dataTable)
    {
        var columnData = dataTable.AsEnumerable().Select(r => r[columnName]?.ToString()).ToList();
        var isNumeric = columnData.All(v => double.TryParse(v, out _));
        var plt = new ScottPlot.Plot();
        if (isNumeric)
        {
            var data = dataTable.AsEnumerable().Select(r => Convert.ToDouble(r[columnName])).ToArray();
            plt.Add.Bars(data);
            plt.Title($"Univariate Analysis of {columnName}");
            plt.XLabel(columnName);
            plt.YLabel("Frequency");
        }
        else
        {
            var counts = columnData.GroupBy(x => x).ToDictionary(g => g.Key ?? string.Empty, g => g.Count())
                                    .OrderByDescending(k => k.Value).ToList();
            var bars = counts.Select((kvp, idx) => new ScottPlot.Bar { Position = idx, Value = kvp.Value, Label = kvp.Key }).ToArray();
            plt.Add.Bars(bars);
            plt.Title($"Frequency Count of {columnName}");
            plt.XLabel("Category");
            plt.YLabel("Count");
        }
        return plt.GetSvgXml(600, 400);
    }

    private async Task<CsvViewModel> ProcessCsvInternalAsync(string fileName, bool useSafeMethod, char delimiter)
    {
        var model = new CsvViewModel { FileName = fileName };
        try
        {
            var dfResult = await _csvFileReader.ReadCsvAsDataFrameAsync(fileName, delimiter, useSafeMethod).ConfigureAwait(false);
            if (!dfResult.Success || dfResult.Data.Count == 0 || dfResult.Data[0] == null)
            {
                model.Message = dfResult.ErrorMessage ?? "Failed to load CSV data";
                return model;
            }
            PopulateCsvViewModel(model, dfResult.Data[0]);
        }
        catch (Exception ex)
        {
            model.Message = $"Error processing file: {ex.Message}";
            if (!useSafeMethod) throw;
        }
        return model;
    }

    private static void PopulateCsvViewModel(CsvViewModel model, DataFrame df)
    {
        model.RowCount = df.Rows.Count;
        model.ColumnCount = df.Columns.Count;
        model.ColumnDetails = df.GetUnivariateAnalysis();
        model.BivariateAnalyses = df.GetBivariateAnalysis();
        model.Info = df.Info();
        model.Description = df.Description();
        model.Head = df.Head(5);
    }

    private static double Median(double[] data)
    {
        Array.Sort(data);
        int n = data.Length;
        return n % 2 == 0 ? (data[n / 2 - 1] + data[n / 2]) / 2d : data[n / 2];
    }
    private static double Quantile(double[] data, double q)
    {
        Array.Sort(data);
        if (q < 0 || q > 1) throw new ArgumentOutOfRangeException(nameof(q));
        double pos = (data.Length - 1) * q;
        int li = (int)pos;
        double frac = pos - li;
        return li + 1 < data.Length ? data[li] + frac * (data[li + 1] - data[li]) : data[li];
    }

    private static string DataFrameToJson(DataFrame df)
    {
        var rows = new List<Dictionary<string, object?>>();
        foreach (var row in df.Rows)
        {
            var dict = new Dictionary<string, object?>();
            for (int i = 0; i < df.Columns.Count; i++)
                dict[df.Columns[i].Name] = row[i];
            rows.Add(dict);
        }
        return JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
    }
}

/// <summary>
/// Minimal abstraction allowing Core service to obtain DataFrames and raw records without referencing web implementation.
/// </summary>
public interface ICsvFileReader
{
    Task<CsvFileReadResult<DataFrame>> ReadCsvAsDataFrameAsync(string fileName, char delimiter, bool allString);
}

public class CsvFileReadResult<T>
{
    public bool Success { get; set; }
    public List<T> Data { get; set; } = new();
    public string? ErrorMessage { get; set; }
}