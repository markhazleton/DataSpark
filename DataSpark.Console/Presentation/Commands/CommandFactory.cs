using System.CommandLine;

namespace DataSpark.Presentation.Commands;

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
        var rootCommand = new RootCommand("DataSpark CLI - Discover, export, inspect schema, and generate DTOs from SQLite databases");

        var versionOption = new Option<bool>("--version")
        {
            Description = "Display version"
        };
        var verbosityOption = new Option<string>("--verbosity")
        {
            Description = "Logging level: quiet|normal|detailed",
            DefaultValueFactory = _ => "normal"
        };
        rootCommand.Add(versionOption);
        rootCommand.Add(verbosityOption);

        rootCommand.Add(DiscoverCommand.Create(services));
        rootCommand.Add(ExportCommand.Create(services));
        rootCommand.Add(SchemaCommand.Create(services));
        rootCommand.Add(GenerateCommand.Create(services));

        return rootCommand;
    }
}
