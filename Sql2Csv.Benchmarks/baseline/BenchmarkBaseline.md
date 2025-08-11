```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.5742)
AMD Ryzen 9 7900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.304
  [Host]        : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  WarmupCount=1 : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=WarmupCount=1  IterationCount=5  WarmupCount=1  

```
| Type                | Method                 | InvocationCount | UnrollFactor | DatabaseCount | Tables | RowsPerTable | RowCount | Mean         | Error        | StdDev     | Gen0   | Gen1   | Allocated  |
|-------------------- |----------------------- |---------------- |------------- |-------------- |------- |------------- |--------- |-------------:|-------------:|-----------:|-------:|-------:|-----------:|
| **DiscoveryBenchmarks** | **DiscoverDatabasesAsync** | **Default**         | **16**           | **5**             | **?**      | **?**            | **?**        |     **59.04 μs** |     **3.701 μs** |   **0.573 μs** | **0.2441** |      **-** |    **4.04 KB** |
| **SchemaBenchmarks**    | **GetTableNames**          | **Default**         | **16**           | **?**             | **5**      | **100**          | **?**        |     **33.72 μs** |     **2.760 μs** |   **0.717 μs** | **0.0610** |      **-** |    **1.52 KB** |
| SchemaBenchmarks    | GetTables              | Default         | 16           | ?             | 5      | 100          | ?        |    337.71 μs |    22.400 μs |   5.817 μs | 0.9766 |      - |   20.91 KB |
| SchemaBenchmarks    | GenerateTextReport     | Default         | 16           | ?             | 5      | 100          | ?        |    343.01 μs |    20.969 μs |   3.245 μs | 1.4648 |      - |   27.23 KB |
| SchemaBenchmarks    | GenerateMarkdownReport | Default         | 16           | ?             | 5      | 100          | ?        |    349.10 μs |    21.813 μs |   5.665 μs | 1.4648 |      - |   29.16 KB |
| SchemaBenchmarks    | GenerateJsonReport     | Default         | 16           | ?             | 5      | 100          | ?        |    350.58 μs |    12.942 μs |   2.003 μs | 1.9531 |      - |    34.2 KB |
| **ExportBenchmarks**    | **ExportTable**            | **1**               | **1**            | **?**             | **?**      | **?**            | **10**       |    **325.48 μs** |    **48.269 μs** |  **12.535 μs** |      **-** |      **-** |   **28.05 KB** |
| **DiscoveryBenchmarks** | **DiscoverDatabasesAsync** | **Default**         | **16**           | **25**            | **?**      | **?**            | **?**        |     **63.80 μs** |     **5.028 μs** |   **0.778 μs** | **0.9766** |      **-** |   **16.37 KB** |
| **DiscoveryBenchmarks** | **DiscoverDatabasesAsync** | **Default**         | **16**           | **50**            | **?**      | **?**            | **?**        |     **80.22 μs** |     **3.605 μs** |   **0.936 μs** | **1.8311** | **0.1221** |   **31.87 KB** |
| **ExportBenchmarks**    | **ExportTable**            | **1**               | **1**            | **?**             | **?**      | **?**            | **1000**     |  **2,462.70 μs** |   **823.267 μs** | **127.401 μs** |      **-** |      **-** |  **266.16 KB** |
| **ExportBenchmarks**    | **ExportTable**            | **1**               | **1**            | **?**             | **?**      | **?**            | **10000**    | **20,384.10 μs** | **2,518.062 μs** | **653.933 μs** |      **-** |      **-** | **2798.78 KB** |
