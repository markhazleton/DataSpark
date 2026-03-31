using System.CommandLine;
using System.Text.Json;
using DataSpark.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

            using var scope = services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IDatabaseDiscoverySummaryService>>();

            if (string.IsNullOrWhiteSpace(path))
            {
                logger.LogError("--path is required");
                Environment.ExitCode = 1;
                return;
            }

            if (!Directory.Exists(path))
            {
                logger.LogError("Path not found: {Path}", path);
                Environment.ExitCode = 1;
                return;
            }

            var summaryService = scope.ServiceProvider.GetRequiredService<IDatabaseDiscoverySummaryService>();
            var result = await summaryService.ScanAsync(path, recursive).ConfigureAwait(false);

            if (result.Databases.Count == 0)
            {
                logger.LogWarning("No SQLite databases found");
                Environment.ExitCode = 2;
                return;
            }

            if (format == "json")
            {
                var jsonObj = new { databases = result.Databases.Select(d => new { d.Path, d.SizeBytes, d.TableCount }) };
                logger.LogInformation("{Output}", JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true }));
            }
            else if (format == "markdown")
            {
                logger.LogInformation("| Path | Size (bytes) | Tables |");
                logger.LogInformation("|------|--------------|--------|");
                foreach (var db in result.Databases)
                {
                    logger.LogInformation("| {Path} | {SizeBytes} | {TableCount} |", db.Path, db.SizeBytes, db.TableCount);
                }
            }
            else
            {
                logger.LogInformation("Found {Count} SQLite database(s):", result.Databases.Count);
                foreach (var db in result.Databases)
                {
                    logger.LogInformation("  {Path}  ({SizeBytes} bytes, {TableCount} tables)", db.Path, db.SizeBytes, db.TableCount);
                }
            }

            Environment.ExitCode = 0;
        });

        return command;
    }
}
