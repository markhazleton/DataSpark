using System.CommandLine;
using DataSpark.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DataSpark.Presentation.Commands;

internal static class ExportCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("export", "Export database tables to CSV files");

        var pathOption = new Option<string>("--path")
        {
            Description = "SQLite database path or directory"
        };

        var outputOption = new Option<string>("--output")
        {
            Description = "Output directory"
        };

        var tablesOption = new Option<string?>("--tables")
        {
            Description = "Comma-separated tables to export"
        };

        var delimiterOption = new Option<string?>("--delimiter")
        {
            Description = "CSV delimiter"
        };

        var noHeadersOption = new Option<bool>("--no-headers")
        {
            Description = "Omit column headers"
        };

        command.Add(pathOption);
        command.Add(outputOption);
        command.Add(tablesOption);
        command.Add(delimiterOption);
        command.Add(noHeadersOption);

        command.SetAction(async parseResult =>
        {
            var path = parseResult.GetValue(pathOption) ?? string.Empty;
            var output = parseResult.GetValue(outputOption) ?? string.Empty;
            var tables = parseResult.GetValue(tablesOption);
            var delimiter = parseResult.GetValue(delimiterOption);
            var noHeaders = parseResult.GetValue(noHeadersOption);

            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(output))
            {
                Console.Error.WriteLine("--path and --output are required.");
                Environment.ExitCode = 1;
                return;
            }

            var searchPath = NormalizeToSearchPath(path);
            if (!Directory.Exists(searchPath))
            {
                Console.Error.WriteLine($"Path not found: {path}");
                Environment.ExitCode = 1;
                return;
            }

            using var scope = services.CreateScope();
            var app = scope.ServiceProvider.GetRequiredService<ApplicationService>();
            var tableList = ParseTables(tables);
            bool? includeHeaders = noHeaders ? false : null;

            await app.ExportDatabasesAsync(searchPath, output, tableList, delimiter, includeHeaders).ConfigureAwait(false);
            Environment.ExitCode = 0;
        });

        return command;
    }

    private static string NormalizeToSearchPath(string inputPath)
    {
        if (File.Exists(inputPath))
        {
            return Path.GetDirectoryName(inputPath) ?? inputPath;
        }

        return inputPath;
    }

    private static IReadOnlyList<string> ParseTables(string? tables)
    {
        if (string.IsNullOrWhiteSpace(tables))
        {
            return [];
        }

        return tables
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
