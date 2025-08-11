using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace Sql2Csv.Benchmarks;

/// <summary>
/// Shared benchmark configuration.
/// </summary>
public sealed class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default
            .WithId("Default")
            .WithWarmupCount(1)
            .WithIterationCount(5));
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(MarkdownExporter.GitHub, MarkdownExporter.Default);
        Options |= ConfigOptions.JoinSummary;
    }
}
