using Sql2Csv.Core.Interfaces;
using Sql2Csv.Core.Services;
using Sql2Csv.Web.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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

// Register Core services
builder.Services.AddScoped<IDatabaseDiscoveryService, DatabaseDiscoveryService>();
builder.Services.AddScoped<ISchemaService, SchemaService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<ICodeGenerationService, CodeGenerationService>();

// Register Web services
builder.Services.AddScoped<IWebDatabaseService, WebDatabaseService>();
builder.Services.AddScoped<IPersistedFileService, PersistedFileService>();
builder.Services.AddSingleton<IPerformanceMetricsService, PerformanceMetricsService>();

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
