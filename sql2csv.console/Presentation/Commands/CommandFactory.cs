using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Sql2Csv.Application.Services;

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

        return rootCommand;
    }

    private static Command CreateExportCommand(IServiceProvider services)
    {
        var exportCommand = new Command("export", "Export database tables to CSV files");

        var pathOption = new Option<string>(
            "--path",
            description: "Path to directory containing SQLite databases",
            getDefaultValue: () => GetDefaultDataPath());

        var outputOption = new Option<string>(
            "--output",
            description: "Output directory for CSV files",
            getDefaultValue: () => GetDefaultExportPath());

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
            getDefaultValue: () => GetDefaultDataPath());

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
            getDefaultValue: () => GetDefaultDataPath());

        var outputOption = new Option<string>(
            "--output",
            description: "Output directory for generated code",
            getDefaultValue: () => GetDefaultGeneratedPath());

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

    private static string GetDefaultDataPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SQL2CSV", "data");

    private static string GetDefaultExportPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SQL2CSV", "export");

    private static string GetDefaultGeneratedPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SQL2CSV", "generated");
}
