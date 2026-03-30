using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Services;
using Sql2Csv.Core.Configuration;
using Sql2Csv.Web.Services;
using System.Text.Json;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Add services to the container.

// Required for session state
builder.Services.AddDistributedMemoryCache();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Ensure camelCase for API/Json responses consumed by JS table component
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Enable session for retaining current database file path across AJAX requests
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // required for non‑EU cookie consent simplicity
});

// Configure Core services
builder.Services.Configure<FileStorageOptions>(options =>
{
    var config = builder.Configuration;
    options.PersistedDirectory = config["FileUpload:PersistedDirectory"] 
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Sql2Csv.Web", "PersistedDatabases");
    
    var maxAgeDays = config.GetValue("FileUpload:MaxAgeDays", 30);
    options.MaxFileAge = TimeSpan.FromDays(maxAgeDays);
    
    var maxSizeMB = config.GetValue("FileUpload:MaxStorageSizeMB", 1024);
    options.MaxStorageSizeBytes = maxSizeMB * 1024L * 1024L;
});

// Register Core services
builder.Services.AddScoped<IDatabaseDiscoveryService, DatabaseDiscoveryService>();
builder.Services.AddScoped<ISchemaService, SchemaService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<ICodeGenerationService, CodeGenerationService>();

// Register new Core services
builder.Services.AddScoped<Sql2Csv.Core.Interfaces.IDatabaseAnalysisService, Sql2Csv.Core.Services.DatabaseAnalysisService>();
builder.Services.AddScoped<Sql2Csv.Core.Interfaces.IPersistedFileService, Sql2Csv.Core.Services.PersistedFileService>();

// Register new unified data services (keeping existing services for compatibility)
builder.Services.AddScoped<IDataFileDiscoveryService, DataFileDiscoveryService>();
builder.Services.AddScoped<IUnifiedAnalysisService, UnifiedAnalysisService>();

// Register Web services
builder.Services.AddScoped<IWebDatabaseService, WebDatabaseService>();
builder.Services.AddScoped<IUnifiedWebDataService, UnifiedWebDataService>();
builder.Services.AddSingleton<IPerformanceMetricsService, PerformanceMetricsService>();

// Register Web-specific file storage options
builder.Services.AddScoped<IFileStorageOptions, WebFileStorageOptions>();

// Configure file upload limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 52428800; // 50MB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Cleanup temp files on shutdown
app.Lifetime.ApplicationStopping.Register(() =>
{
    using var scope = app.Services.CreateScope();
    var databaseService = scope.ServiceProvider.GetRequiredService<IWebDatabaseService>();
    databaseService.CleanupTempFiles();
});

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
