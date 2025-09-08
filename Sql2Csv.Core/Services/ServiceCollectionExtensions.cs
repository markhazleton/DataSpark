using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sql2Csv.Core.Configuration;
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
        services.AddScoped<IUnifiedAnalysisService, UnifiedAnalysisService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<ISchemaService, SchemaService>();
        services.AddScoped<ICodeGenerationService, CodeGenerationService>();
        services.AddScoped<ISchemaReportSink, LoggingSchemaReportSink>();
        services.AddScoped<ApplicationService>();

        // Add new core services
        services.AddScoped<IDatabaseAnalysisService, DatabaseAnalysisService>();
        services.AddScoped<IPersistedFileService, PersistedFileService>();
        
        return services;
    }

    /// <summary>
    /// Adds file storage services with configuration
    /// </summary>
    public static IServiceCollection AddFileStorage(this IServiceCollection services, Action<FileStorageOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddScoped<IFileStorageOptions, FileStorageOptionsWrapper>();
        services.AddScoped<IPersistedFileService, PersistedFileService>();
        
        return services;
    }
}
