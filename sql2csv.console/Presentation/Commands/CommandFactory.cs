using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Sql2Csv.Core.Configuration;
using Sql2Csv.Core.Services;
using Sql2Csv.Core.Interfaces;

namespace Sql2Csv.Presentation.Commands;

/// <summary>
/// Factory for creating CLI commands.
/// </summary>
public static class CommandFactory
{
    /// <summary>
    /// Creates the root command for the CLI.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <returns>The root command.</returns>
    public static RootCommand CreateRootCommand(IServiceProvider services)
    {
        var rootCommand = new RootCommand("SQL to CSV Exporter - Exports SQLite databases to CSV format");

        rootCommand.AddCommand(CreateExportCommand(services));
        rootCommand.AddCommand(CreateSchemaCommand(services));
        rootCommand.AddCommand(CreateGenerateCommand(services));
        rootCommand.AddCommand(CreateDiscoverCommand(services));

        return rootCommand;
    }

    private static Command CreateDiscoverCommand(IServiceProvider services)
    {
        var discoverCommand = new Command("discover", "Discover SQLite database files and print a summary");

        var pathOption = new Option<string>(
            "--path",
            description: "Path to directory containing SQLite databases",
            getDefaultValue: () => GetDefaultDataPath(services));

        discoverCommand.AddOption(pathOption);

        discoverCommand.SetHandler(async (path) =>
        {
            using var scope = services.CreateScope();
            var discovery = scope.ServiceProvider.GetRequiredService<IDatabaseDiscoveryService>();
            var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("discover");
            try
            {
                logger?.LogInformation("Discovering databases in {Path}", path);
                var databases = await discovery.DiscoverDatabasesAsync(path, CancellationToken.None);
                Console.WriteLine($"Discovered {databases.Count()} database(s) in '{path}'.");
                foreach (var db in databases.OrderBy(d => d.Name))
                {
                    Console.WriteLine(" - " + db.Name);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during discovery");
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, pathOption);

        return discoverCommand;
    }

    private static Command CreateExportCommand(IServiceProvider services)
    {
        var exportCommand = new Command("export", "Export database tables to CSV files");

        var pathOption = new Option<string>(
            "--path",
            description: "Path to directory containing SQLite databases",
            getDefaultValue: () => GetDefaultDataPath(services));

        var outputOption = new Option<string>(
            "--output",
            description: "Output directory for CSV files",
            getDefaultValue: () => GetDefaultExportPath(services));

        exportCommand.AddOption(pathOption);
        exportCommand.AddOption(outputOption);

        exportCommand.SetHandler(async (path, output) =>
        {
            using var scope = services.CreateScope();
            var app = scope.ServiceProvider.GetRequiredService<ApplicationService>();
            await app.ExportDatabasesAsync(path, output);
        }, pathOption, outputOption);

        return exportCommand;
    }

    private static Command CreateSchemaCommand(IServiceProvider services)
    {
        var schemaCommand = new Command("schema", "Generate schema reports for databases");

        var pathOption = new Option<string>(
            "--path",
            description: "Path to directory containing SQLite databases",
            getDefaultValue: () => GetDefaultDataPath(services));

        schemaCommand.AddOption(pathOption);

        schemaCommand.SetHandler(async (path) =>
        {
            using var scope = services.CreateScope();
            var app = scope.ServiceProvider.GetRequiredService<ApplicationService>();
            await app.GenerateSchemaReportsAsync(path);
        }, pathOption);

        return schemaCommand;
    }

    private static Command CreateGenerateCommand(IServiceProvider services)
    {
        var generateCommand = new Command("generate", "Generate DTO classes from database schema");

        var pathOption = new Option<string>(
            "--path",
            description: "Path to directory containing SQLite databases",
            getDefaultValue: () => GetDefaultDataPath(services));

        var outputOption = new Option<string>(
            "--output",
            description: "Output directory for generated code",
            getDefaultValue: () => GetDefaultGeneratedPath(services));

        var namespaceOption = new Option<string>(
            "--namespace",
            description: "Namespace for generated classes",
            getDefaultValue: () => "Sql2Csv.Generated");

        generateCommand.AddOption(pathOption);
        generateCommand.AddOption(outputOption);
        generateCommand.AddOption(namespaceOption);

        generateCommand.SetHandler(async (path, output, namespaceName) =>
        {
            using var scope = services.CreateScope();
            var app = scope.ServiceProvider.GetRequiredService<ApplicationService>();
            await app.GenerateCodeAsync(path, output, namespaceName);
        }, pathOption, outputOption, namespaceOption);

        return generateCommand;
    }

    private static string GetDefaultDataPath(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<Sql2CsvOptions>>().Value;
        return Path.Combine(options.RootPath, options.Paths.Data);
    }

    private static string GetDefaultExportPath(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<Sql2CsvOptions>>().Value;
        return Path.Combine(options.RootPath, "export");
    }

    private static string GetDefaultGeneratedPath(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<Sql2CsvOptions>>().Value;
        return Path.Combine(options.RootPath, "generated");
    }
}
