using CsvHelper;
using CsvHelper.Configuration;
using DataSpark.Core.Models.Analysis;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace DataSpark.Core.Services.Analysis;

/// <summary>
/// Core implementation for bivariate analysis computations.
/// </summary>
public sealed class BivariateAnalysisService : IBivariateAnalysisService
{
    private readonly ILogger<BivariateAnalysisService> _logger;

    public BivariateAnalysisService(ILogger<BivariateAnalysisService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<BivariateAnalysisResult> AnalyzeAsync(string filePath, string column1, string column2, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(column1);
        ArgumentException.ThrowIfNullOrWhiteSpace(column2);

        cancellationToken.ThrowIfCancellationRequested();

        var records = await ReadRecordsAsync(filePath, cancellationToken).ConfigureAwait(false);

        if (!records.Any())
        {
            throw new InvalidOperationException("File is empty.");
        }

        var headers = records[0].Keys.ToList();
        if (!headers.Contains(column1) || !headers.Contains(column2))
        {
            throw new InvalidOperationException("One or both columns not found in file.");
        }

        var col1Data = records.Select(r => r[column1]?.ToString()).ToList();
        var col2Data = records.Select(r => r[column2]?.ToString()).ToList();

        var col1Numeric = col1Data.All(v => double.TryParse(v, out _) || string.IsNullOrEmpty(v));
        var col2Numeric = col2Data.All(v => double.TryParse(v, out _) || string.IsNullOrEmpty(v));
        var col1Date = col1Data.All(v => DateTime.TryParse(v, out _) || string.IsNullOrEmpty(v));
        var col2Date = col2Data.All(v => DateTime.TryParse(v, out _) || string.IsNullOrEmpty(v));

        var result = new BivariateAnalysisResult
        {
            FileName = Path.GetFileName(filePath),
            Column1 = column1,
            Column2 = column2,
            Col1Type = col1Numeric ? "Numeric" : (col1Date ? "Date" : "Categorical"),
            Col2Type = col2Numeric ? "Numeric" : (col2Date ? "Date" : "Categorical")
        };

        if ((col1Numeric || col1Date) && (col2Numeric || col2Date))
        {
            var x = col1Data.Where(v => !string.IsNullOrEmpty(v) && double.TryParse(v, out _)).Select(v => double.Parse(v!, CultureInfo.InvariantCulture)).ToList();
            var y = col2Data.Where(v => !string.IsNullOrEmpty(v) && double.TryParse(v, out _)).Select(v => double.Parse(v!, CultureInfo.InvariantCulture)).ToList();

            var n = Math.Min(x.Count, y.Count);
            if (n > 1)
            {
                x = x.Take(n).ToList();
                y = y.Take(n).ToList();

                var meanX = x.Average();
                var meanY = y.Average();
                var sumXY = x.Zip(y, (a, b) => (a - meanX) * (b - meanY)).Sum();
                var sumX2 = x.Sum(a => Math.Pow(a - meanX, 2));
                var sumY2 = y.Sum(b => Math.Pow(b - meanY, 2));
                var corr = (sumX2 == 0 || sumY2 == 0) ? double.NaN : sumXY / Math.Sqrt(sumX2 * sumY2);

                var slope = sumX2 == 0 ? 0 : sumXY / sumX2;
                var intercept = meanY - slope * meanX;

                result.Correlation = corr;
                result.Regression = new RegressionResult { Intercept = intercept, Slope = slope };
                result.Scatter = x.Zip(y, (a1, b1) => new[] { a1, b1 }).ToList();
            }
        }
        else if (!col1Numeric && !col2Numeric)
        {
            var table = col1Data.Zip(col2Data, (a, b) => new { a, b })
                .GroupBy(x => x.a)
                .ToDictionary(
                    g => g.Key ?? string.Empty,
                    g => g.GroupBy(x => x.b)
                        .ToDictionary(gg => gg.Key ?? string.Empty, gg => gg.Count()));

            result.ContingencyTable = table;
        }
        else
        {
            var numeric = col1Numeric ? col1Data : col2Data;
            var categorical = col1Numeric ? col2Data : col1Data;

            var groups = categorical.Zip(numeric, (cat, num) => new { cat, num })
                .Where(x => !string.IsNullOrEmpty(x.num) && double.TryParse(x.num, out _))
                .GroupBy(x => x.cat ?? string.Empty)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var nums = g.Select(x => double.Parse(x.num!, CultureInfo.InvariantCulture)).ToList();
                        return new GroupStatsResult
                        {
                            Count = nums.Count,
                            Min = nums.Any() ? nums.Min() : null,
                            Max = nums.Any() ? nums.Max() : null,
                            Mean = nums.Any() ? nums.Average() : null,
                            Std = nums.Count > 1 ? Math.Sqrt(nums.Select(x => Math.Pow(x - nums.Average(), 2)).Average()) : null,
                            Values = nums
                        };
                    });

            result.GroupStats = groups;
        }

        _logger.LogInformation("Completed bivariate analysis for {Column1} and {Column2}", column1, column2);
        return result;
    }

    private static async Task<List<IDictionary<string, object?>>> ReadRecordsAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        var records = new List<IDictionary<string, object?>>();
        if (!await csv.ReadAsync().ConfigureAwait(false))
        {
            return records;
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();

        while (await csv.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                row[header] = csv.GetField(header);
            }

            records.Add(row);
        }

        return records;
    }
}
