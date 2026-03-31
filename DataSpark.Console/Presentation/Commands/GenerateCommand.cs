using System.CommandLine;
using DataSpark.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataSpark.Presentation.Commands;

internal static class GenerateCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("generate", "Generate C# DTO classes from database schema");

        var pathOption = new Option<string>("--path")
        {
            Description = "SQLite database path or directory"
        };

        var outputOption = new Option<string>("--output")
        {
            Description = "Output directory"
        };

        var namespaceOption = new Option<string>("--namespace")
        {
            Description = "Namespace for generated classes",
            DefaultValueFactory = _ => "DataSpark.Models"
        };
        var tableOption = new Option<string?>("--table", "Specific table name");

        command.Add(pathOption);
        command.Add(outputOption);
        command.Add(namespaceOption);
        command.Add(tableOption);

        command.SetAction(async parseResult =>
        {
            var path = parseResult.GetValue(pathOption) ?? string.Empty;
            var output = parseResult.GetValue(outputOption) ?? string.Empty;
            var namespaceName = parseResult.GetValue(namespaceOption) ?? "DataSpark.Models";
            var table = parseResult.GetValue(tableOption);

            using var scope = services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationService>>();

            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(output))
            {
                logger.LogError("--path and --output are required");
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
            await app.GenerateCodeAsync(searchPath, output, namespaceName, filter).ConfigureAwait(false);
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
