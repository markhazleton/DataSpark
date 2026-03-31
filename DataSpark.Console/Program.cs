
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DataSpark.Core.Configuration;
using DataSpark.Core.Interfaces;
using DataSpark.Core.Services;
using DataSpark.Presentation.Commands;
using System.CommandLine;
using System.Reflection;

namespace DataSpark;

/// <summary>
/// The main program class.
/// </summary>
public static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        using var earlyLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = earlyLoggerFactory.CreateLogger("DataSpark.Console");

        try
        {
            if (args.Any(a => string.Equals(a, "--version", StringComparison.OrdinalIgnoreCase)))
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
                logger.LogInformation("DataSpark.Console {Version}", version);
                return 0;
            }

            var host = CreateHostBuilder(args).Build();

            var rootCommand = CommandFactory.CreateRootCommand(host.Services);
            var parseResult = rootCommand.Parse(args);
            return await parseResult.InvokeAsync(parseResult.InvocationConfiguration, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred");
            return 1;
        }
    }

    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The host builder.</returns>
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Configure options
                services.Configure<DataSparkOptions>(
                    context.Configuration.GetSection(DataSparkOptions.SectionName));

                // Register services
                services.AddScoped<IDatabaseDiscoveryService, DatabaseDiscoveryService>();
                services.AddScoped<IDatabaseDiscoverySummaryService, DatabaseDiscoverySummaryService>();
                services.AddScoped<IExportService, ExportService>();
                services.AddScoped<ISchemaService, SchemaService>();
                services.AddScoped<ICodeGenerationService, CodeGenerationService>();
                services.AddScoped<ApplicationService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });
}
