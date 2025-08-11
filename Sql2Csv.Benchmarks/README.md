# Sql2Csv Benchmarks

BenchmarkDotNet benchmarks for the core Sql2Csv services (discovery, export, schema generation).

## Projects Covered

* Database discovery (`DatabaseDiscoveryService`)
* Table export (`ExportService`)
* Schema introspection & report generation (`SchemaService`)

## Running

```bash
dotnet run -c Release --project Sql2Csv.Benchmarks
```

Add a filter (optional):

```bash
dotnet run -c Release --project Sql2Csv.Benchmarks -- --filter *Export*
```

## Baseline Results

Initial baseline results are stored in `baseline/BenchmarkBaseline.md` (generated with .NET 9, BenchmarkDotNet 0.14.0). Re-run locally to compare evolutions.

## Notes

Databases are generated in temp folders during `GlobalSetup` so benchmark measurements exclude data generation and focus on the service methods themselves. Export benchmarks parameterize row counts (10, 1k, 10k). Schema benchmarks currently fix table/row counts for stability; extend via additional `[Params]` as needed.
