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

        var delimiterOption = new Option<string?>(
            "--delimiter",
            description: "Optional CSV delimiter override (defaults to configured option)");

        var headersOption = new Option<bool?>(
            "--headers",
            description: "Override to include (true) or exclude (false) headers; omit to use configured option");

        var tablesOption = new Option<string?>(
            name: "--tables",
            description: "Optional comma or semicolon separated list of tables to export (case-insensitive). Example: --tables Users,Orders;Products");

        exportCommand.AddOption(pathOption);
        exportCommand.AddOption(outputOption);
        exportCommand.AddOption(delimiterOption);
        exportCommand.AddOption(headersOption);
        exportCommand.AddOption(tablesOption);

        exportCommand.SetHandler(async (path, output, delimiter, headers, tables) =>
        {
            using var scope = services.CreateScope();
            var app = scope.ServiceProvider.GetRequiredService<ApplicationService>();
            var tableList = ParseTables(tables);
            if (tableList.Any())
            {
                // Capture console output count heuristic by temp directory snapshot
                var before = Directory.Exists(output) ? Directory.GetFiles(output, "*_extract.csv", SearchOption.AllDirectories).Length : 0;
                await app.ExportDatabasesAsync(path, output, tableList, delimiter, headers);
                var after = Directory.Exists(output) ? Directory.GetFiles(output, "*_extract.csv", SearchOption.AllDirectories).Length : 0;
                if (after == before)
                {
                    Console.Error.WriteLine("No matching tables found for supplied filter.");
                    Environment.ExitCode = 2; // failure per requirements when no tables matched
                }
            }
            else
            {
                await app.ExportDatabasesAsync(path, output, delimiter, headers);
            }
        }, pathOption, outputOption, delimiterOption, headersOption, tablesOption);

        return exportCommand;
    }

    private static Command CreateSchemaCommand(IServiceProvider services)
    {
        var schemaCommand = new Command("schema", "Generate schema reports for databases");

        var pathOption = new Option<string>(
            "--path",
            description: "Path to directory containing SQLite databases",
            getDefaultValue: () => GetDefaultDataPath(services));

        var formatOption = new Option<string>(
            name: "--format",
            description: "Output format: text (default), json, markdown",
            getDefaultValue: () => "text");

        var tablesOption = new Option<string?>(
            name: "--tables",
            description: "Optional comma or semicolon separated list of tables to include in schema report (currently informational only).");

        schemaCommand.AddOption(pathOption);
        schemaCommand.AddOption(formatOption);
        schemaCommand.AddOption(tablesOption);

        schemaCommand.SetHandler(async (path, format, tables) =>
        {
            using var scope = services.CreateScope();
            var app = scope.ServiceProvider.GetRequiredService<ApplicationService>();
            var tableList = ParseTables(tables);
            await app.GenerateSchemaReportsAsync(path, format, tableList.Any() ? tableList : null);
        }, pathOption, formatOption, tablesOption);

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

        var tablesOption = new Option<string?>(
            name: "--tables",
            description: "Optional comma or semicolon separated list of tables to generate DTO classes for (currently ignored).");

        generateCommand.AddOption(pathOption);
        generateCommand.AddOption(outputOption);
        generateCommand.AddOption(namespaceOption);
        generateCommand.AddOption(tablesOption);

        generateCommand.SetHandler(async (path, output, namespaceName, tables) =>
        {
            using var scope = services.CreateScope();
            var app = scope.ServiceProvider.GetRequiredService<ApplicationService>();
            var tableList = ParseTables(tables);
            await app.GenerateCodeAsync(path, output, namespaceName, tableList.Any() ? tableList : null);
        }, pathOption, outputOption, namespaceOption, tablesOption);

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

    private static IReadOnlyList<string> ParseTables(string? tables)
    {
        if (string.IsNullOrWhiteSpace(tables)) return Array.Empty<string>();
        var split = tables.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(t => t.Trim())
                          .Where(t => t.Length > 0)
                          .Distinct(StringComparer.OrdinalIgnoreCase)
                          .ToList();
        return split;
    }
}
