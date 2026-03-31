using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using DataSpark.Benchmarks.Util;
using DataSpark.Core.Configuration;
using DataSpark.Core.Interfaces;
using DataSpark.Core.Services;

namespace DataSpark.Benchmarks;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class SchemaBenchmarks
{
    private ISchemaService _schema = default!;
    private string _connectionString = string.Empty;

    [Params(5)] public int Tables;
    [Params(100)] public int RowsPerTable;

    [GlobalSetup]
    public void Setup()
    {
        var options = Options.Create(new DataSparkOptions
        {
            Export = new ExportOptions { Delimiter = ",", IncludeHeaders = true, Encoding = "UTF-8" },
            Database = new DatabaseOptions { Timeout = 600 }
        });
        _schema = new SchemaService(new NullLogger<SchemaService>(), options);
        var db = BenchmarkHelpers.CreateSchemaDatabase(Tables, RowsPerTable);
        _connectionString = db.ConnectionString;
    }

    [Benchmark]
    public async Task<int> GetTableNames()
    {
        var names = await _schema.GetTableNamesAsync(_connectionString);
        return names.Count();
    }

    [Benchmark]
    public async Task<int> GetTables()
    {
        var tables = await _schema.GetTablesAsync(_connectionString);
        return tables.Count();
    }

    [Benchmark]
    public Task<string> GenerateTextReport() => _schema.GenerateSchemaReportAsync(_connectionString, "text");

    [Benchmark]
    public Task<string> GenerateMarkdownReport() => _schema.GenerateSchemaReportAsync(_connectionString, "markdown");

    [Benchmark]
    public Task<string> GenerateJsonReport() => _schema.GenerateSchemaReportAsync(_connectionString, "json");
}
