
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sql2Csv.Core.Configuration;
using Sql2Csv.Core.Interfaces;
using Sql2Csv.Infrastructure.Services;
using Sql2Csv.Application.Services;
using Sql2Csv.Presentation.Commands;
using System.CommandLine;

namespace Sql2Csv;

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
        try
        {
            var host = CreateHostBuilder(args).Build();

            var rootCommand = CommandFactory.CreateRootCommand(host.Services);
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
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
                services.Configure<Sql2CsvOptions>(
                    context.Configuration.GetSection(Sql2CsvOptions.SectionName));

                // Register services
                services.AddScoped<IDatabaseDiscoveryService, DatabaseDiscoveryService>();
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
