using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sql2Csv.Benchmarks.Util;
using Sql2Csv.Core.Configuration;
using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Services;

namespace Sql2Csv.Benchmarks;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class ExportBenchmarks
{
    private IExportService _exportService = default!;
    private ISchemaService _schemaService = default!;
    private Core.Models.DatabaseConfiguration _db = default!;
    private string _outputDir = string.Empty;

    [Params(10, 1000, 10000)]
    public int RowCount;

    [GlobalSetup]
    public void Setup()
    {
        var options = Options.Create(new Sql2CsvOptions
        {
            Export = new ExportOptions { Delimiter = ",", IncludeHeaders = true, Encoding = "UTF-8" },
            Database = new DatabaseOptions { Timeout = 600 }
        });
        _schemaService = new SchemaService(new NullLogger<SchemaService>(), options);
        _exportService = new ExportService(new NullLogger<ExportService>(), _schemaService, options);
        _db = BenchmarkHelpers.CreateDatabase(RowCount);
        _outputDir = BenchmarkHelpers.CreateTempDirectory("export");
    }

    [IterationSetup]
    public void IterationSetup()
    {
        if (Directory.Exists(_outputDir))
        {
            foreach (var f in Directory.EnumerateFiles(_outputDir))
                File.Delete(f);
        }
    }

    [Benchmark]
    public async Task<long> ExportTable()
    {
        var result = await _exportService.ExportTableToCsvAsync(_db, "Data", Path.Combine(_outputDir, $"data_{RowCount}.csv"));
        return result.RowCount;
    }
}
