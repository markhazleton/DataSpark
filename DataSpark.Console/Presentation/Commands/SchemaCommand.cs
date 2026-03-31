using System.CommandLine;
using DataSpark.Core.Services;
using Microsoft.Extensions.DependencyInjection;

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

            if (string.IsNullOrWhiteSpace(path))
            {
                Console.Error.WriteLine("--path is required.");
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
