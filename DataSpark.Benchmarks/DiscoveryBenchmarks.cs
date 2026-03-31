using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using DataSpark.Core.Interfaces;
using DataSpark.Core.Services;
using DataSpark.Benchmarks.Util;

namespace DataSpark.Benchmarks;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class DiscoveryBenchmarks
{
    private IDatabaseDiscoveryService _service = default!;
    private string _directory = string.Empty;

    [Params(5, 25, 50)]
    public int DatabaseCount;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _service = new DatabaseDiscoveryService(new NullLogger<DatabaseDiscoveryService>());
        _directory = BenchmarkHelpers.CreateTempDirectory("discover");
        // create dummy databases
        for (int i = 0; i < DatabaseCount; i++)
        {
            BenchmarkHelpers.CreateDatabaseAt(_directory, $"db_{i}.db", 1);
        }
    }

    [Benchmark]
    public async Task<int> DiscoverDatabasesAsync()
    {
        var configs = await _service.DiscoverDatabasesAsync(_directory);
        return configs.Count();
    }
}
