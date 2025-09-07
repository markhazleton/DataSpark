using Microsoft.Extensions.DependencyInjection;
using Sql2Csv.Core.Interfaces;

namespace Sql2Csv.Core.Services;

/// <summary>
/// DI registration helpers.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSql2CsvCore(this IServiceCollection services, Action<Configuration.Sql2CsvOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddScoped<IDatabaseDiscoveryService, DatabaseDiscoveryService>();
        services.AddScoped<IDataFileDiscoveryService, DataFileDiscoveryService>();
        services.AddScoped<ICsvAnalysisService, CsvAnalysisService>();
        services.AddScoped<IUnifiedAnalysisService, UnifiedAnalysisService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<ISchemaService, SchemaService>();
        services.AddScoped<ICodeGenerationService, CodeGenerationService>();
        services.AddScoped<ISchemaReportSink, LoggingSchemaReportSink>();
        services.AddScoped<ApplicationService>();
        return services;
    }
}
