using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace DataSpark.Core.Services.Analysis;

/// <summary>
/// Core implementation for bivariate SVG generation.
/// </summary>
public sealed class BivariateSvgService : IBivariateSvgService
{
    private readonly ILogger<BivariateSvgService> _logger;

    public BivariateSvgService(ILogger<BivariateSvgService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<string> GenerateSvgAsync(string filePath, string column1, string column2, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(column1);
        ArgumentException.ThrowIfNullOrWhiteSpace(column2);

        cancellationToken.ThrowIfCancellationRequested();

        var records = await ReadRecordsAsync(filePath, cancellationToken).ConfigureAwait(false);
        if (!records.Any())
        {
            throw new InvalidOperationException("No data found in file.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var headers = records[0].Keys;
        if (!headers.Contains(column1) || !headers.Contains(column2))
        {
            throw new InvalidOperationException("One or both columns not found in file.");
        }

        var col1Data = records.Select(r => r[column1]?.ToString()).ToList();
        var col2Data = records.Select(r => r[column2]?.ToString()).ToList();

        var col1Numeric = col1Data.All(v => double.TryParse(v, out _) || string.IsNullOrEmpty(v));
        var col2Numeric = col2Data.All(v => double.TryParse(v, out _) || string.IsNullOrEmpty(v));

        var plot = new ScottPlot.Plot();

        if (col1Numeric && col2Numeric)
        {
            var pairs = col1Data.Zip(col2Data, (x, y) => new { x, y })
                .Where(p => double.TryParse(p.x, out _) && double.TryParse(p.y, out _))
                .Select(p => new { X = double.Parse(p.x!, CultureInfo.InvariantCulture), Y = double.Parse(p.y!, CultureInfo.InvariantCulture) })
                .ToList();

            if (pairs.Count < 2)
            {
                throw new InvalidOperationException("Insufficient numeric points for scatter plot.");
            }

            var xs = pairs.Select(p => p.X).ToArray();
            var ys = pairs.Select(p => p.Y).ToArray();
            plot.Add.Scatter(xs, ys);

            var meanX = xs.Average();
            var meanY = ys.Average();
            var numerator = xs.Zip(ys, (x, y) => (x - meanX) * (y - meanY)).Sum();
            var denominator = xs.Sum(x => Math.Pow(x - meanX, 2));
            var slope = denominator == 0 ? 0 : numerator / denominator;
            var intercept = meanY - slope * meanX;
            var minX = xs.Min();
            var maxX = xs.Max();
            var trendX = new[] { minX, maxX };
            var trendY = new[] { intercept + slope * minX, intercept + slope * maxX };
            plot.Add.Scatter(trendX, trendY);
            plot.Title($"{column1} vs {column2} (with trend line)");
            plot.XLabel(column1);
            plot.YLabel(column2);
        }
        else if (col1Numeric ^ col2Numeric)
        {
            var numeric = col1Numeric ? col1Data : col2Data;
            var categories = col1Numeric ? col2Data : col1Data;
            var groupedMeans = categories.Zip(numeric, (cat, num) => new { cat, num })
                .Where(p => !string.IsNullOrWhiteSpace(p.cat) && double.TryParse(p.num, out _))
                .GroupBy(p => p.cat!)
                .OrderBy(g => g.Key)
                .ToList();

            var bars = groupedMeans.Select((g, i) => new ScottPlot.Bar
            {
                Position = i,
                Value = g.Average(x => double.Parse(x.num!, CultureInfo.InvariantCulture)),
                Label = g.Key
            }).ToArray();

            plot.Add.Bars(bars);
            plot.Title($"Grouped Mean by {(col1Numeric ? column2 : column1)}");
            plot.YLabel(col1Numeric ? column1 : column2);
        }
        else
        {
            var counts = col1Data.Zip(col2Data, (a, b) => $"{a} | {b}")
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .GroupBy(v => v)
                .OrderByDescending(g => g.Count())
                .Take(20)
                .ToList();

            var bars = counts.Select((g, i) => new ScottPlot.Bar
            {
                Position = i,
                Value = g.Count(),
                Label = g.Key
            }).ToArray();
            plot.Add.Bars(bars);
            plot.Title($"Top Pair Frequencies: {column1} x {column2}");
            plot.YLabel("Count");
        }

        _logger.LogInformation("Generated bivariate SVG for {Column1} and {Column2}", column1, column2);
        return plot.GetSvgXml(900, 500);
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
