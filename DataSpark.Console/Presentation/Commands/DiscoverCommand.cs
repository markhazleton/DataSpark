using System.CommandLine;
using System.Text.Json;
using DataSpark.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DataSpark.Presentation.Commands;

internal static class DiscoverCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("discover", "Discover SQLite databases in a directory");

        var pathOption = new Option<string>("--path")
        {
            Description = "Directory to scan"
        };

        var recursiveOption = new Option<bool>("--recursive")
        {
            Description = "Include subdirectories"
        };

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: text|json|markdown",
            DefaultValueFactory = _ => "text"
        };

        command.Add(pathOption);
        command.Add(recursiveOption);
        command.Add(formatOption);

        command.SetAction(async parseResult =>
        {
            var path = parseResult.GetValue(pathOption) ?? string.Empty;
            var recursive = parseResult.GetValue(recursiveOption);
            var format = (parseResult.GetValue(formatOption) ?? "text").ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(path))
            {
                Console.Error.WriteLine("--path is required.");
                Environment.ExitCode = 1;
                return;
            }

            if (!Directory.Exists(path))
            {
                Console.Error.WriteLine($"Path not found: {path}");
                Environment.ExitCode = 1;
                return;
            }

            using var scope = services.CreateScope();
            var discovery = scope.ServiceProvider.GetRequiredService<IDatabaseDiscoveryService>();
            var schemaService = scope.ServiceProvider.GetRequiredService<ISchemaService>();

            var scanPaths = recursive
                ? new[] { path }.Concat(Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories))
                : new[] { path };

            var databases = new List<DataSpark.Core.Models.DatabaseConfiguration>();
            foreach (var scanPath in scanPaths)
            {
                var found = await discovery.DiscoverDatabasesAsync(scanPath).ConfigureAwait(false);
                databases.AddRange(found);
            }

            var unique = databases
                .GroupBy(d => d.ConnectionString, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            if (unique.Count == 0)
            {
                Console.Error.WriteLine("No SQLite databases found.");
                Environment.ExitCode = 2;
                return;
            }

            var items = new List<object>();
            foreach (var db in unique)
            {
                var dbPath = GetDatabasePath(db.ConnectionString);
                long sizeBytes = 0;
                if (!string.IsNullOrWhiteSpace(dbPath) && File.Exists(dbPath))
                {
                    sizeBytes = new FileInfo(dbPath).Length;
                }

                var tables = await schemaService.GetTableNamesAsync(db.ConnectionString).ConfigureAwait(false);
                items.Add(new
                {
                    path = dbPath,
                    sizeBytes,
                    tableCount = tables.Count()
                });
            }

            if (format == "json")
            {
                Console.WriteLine(JsonSerializer.Serialize(new { databases = items }, new JsonSerializerOptions { WriteIndented = true }));
            }
            else if (format == "markdown")
            {
                Console.WriteLine("| Path | Size (bytes) | Tables |");
                Console.WriteLine("|------|--------------|--------|");
                foreach (dynamic item in items)
                {
                    Console.WriteLine($"| {item.path} | {item.sizeBytes} | {item.tableCount} |");
                }
            }
            else
            {
                Console.WriteLine($"Found {items.Count} SQLite database(s):");
                foreach (dynamic item in items)
                {
                    Console.WriteLine($"  {item.path}  ({item.sizeBytes} bytes, {item.tableCount} tables)");
                }
            }

            Environment.ExitCode = 0;
        });

        return command;
    }

    private static string GetDatabasePath(string connectionString)
    {
        const string prefix = "Data Source=";
        if (!connectionString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        return connectionString.Substring(prefix.Length).Trim();
    }
}
