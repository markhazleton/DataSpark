using System.CommandLine;
using DataSpark.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataSpark.Presentation.Commands;

internal static class SchemaCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("schema", "Display database schema");

        var pathOption = new Option<string>("--path")
        {
            Description = "SQLite database path or directory"
        };

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: text|json|markdown",
            DefaultValueFactory = _ => "text"
        };
        var tableOption = new Option<string?>("--table", "Specific table name");

        command.Add(pathOption);
        command.Add(formatOption);
        command.Add(tableOption);

        command.SetAction(async parseResult =>
        {
            var path = parseResult.GetValue(pathOption) ?? string.Empty;
            var format = (parseResult.GetValue(formatOption) ?? "text").ToLowerInvariant();
            var table = parseResult.GetValue(tableOption);

            using var scope = services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationService>>();

            if (string.IsNullOrWhiteSpace(path))
            {
                logger.LogError("--path is required");
                Environment.ExitCode = 1;
                return;
            }

            var searchPath = NormalizeToSearchPath(path);
            if (!Directory.Exists(searchPath))
            {
                logger.LogError("Path not found: {Path}", path);
                Environment.ExitCode = 1;
                return;
            }

            var app = scope.ServiceProvider.GetRequiredService<ApplicationService>();
            IReadOnlyList<string>? filter = string.IsNullOrWhiteSpace(table) ? null : [table];
            await app.GenerateSchemaReportsAsync(searchPath, format, filter).ConfigureAwait(false);
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
}
