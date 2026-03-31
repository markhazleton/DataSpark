using DataSpark.Web.Services;
using DataSpark.Web.Services.Chart;
using Serilog;
using DataSpark.Core.Configuration;
using DataSpark.Core.Interfaces;
using DataSpark.Core.Services;
using DataSpark.Core.Services.Analysis;
using DataSpark.Core.Services.Charts;
using WebSpark.Bootswatch;
using DataSpark.Web.Middleware;
using WebCsvFileService = DataSpark.Web.Services.CsvFileService;
using CoreCsvProcessingService = DataSpark.Core.Services.Analysis.CsvProcessingService;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog as the logging provider
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register DataSpark services
// NOTE: Two CsvFileService classes exist (web + core). Web version handles uploads & web-root paths.
// Core version (DataSpark.Core.Services.CsvFileService) is required by ChartDataService in Core.
// Register both explicitly (different CLR types) so DI can resolve the core dependency.
builder.Services.AddScoped<WebCsvFileService>(); // Web layer CsvFileService (DataSpark.Web.Services)
builder.Services.AddScoped<DataSpark.Core.Services.CsvFileService>(); // Core CsvFileService
// Register web CsvProcessingService explicitly (core interface-based service already registered below)
builder.Services.AddScoped<DataSpark.Web.Services.CsvProcessingService>();
// Core CSV processing & export services
builder.Services.AddScoped<ICsvFileReader, WebCsvFileReaderAdapter>();
builder.Services.AddScoped<ICsvProcessingService, CoreCsvProcessingService>();
builder.Services.AddScoped<IBivariateSvgService, BivariateSvgService>();
builder.Services.AddScoped<ISchemaService, SchemaService>();
builder.Services.AddScoped<ICodeGenerationService, CodeGenerationService>();
builder.Services.AddScoped<IExportService, DataSpark.Core.Services.ExportService>();
builder.Services.AddScoped<IDataExportService, DataExportService>();
builder.Services.AddScoped<IDatabaseAnalysisService, DatabaseAnalysisService>();
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection(FileStorageOptions.SectionName));
builder.Services.AddScoped<IFileStorageOptions, FileStorageOptionsWrapper>();
builder.Services.AddScoped<IPersistedFileService, PersistedFileService>();

// Register chart storage provider & repository (core implementation)
builder.Services.AddScoped<IChartStoragePathProvider, WebChartStoragePathProvider>();
builder.Services.AddScoped<IChartConfigurationRepository, FileSystemChartConfigurationRepository>();
builder.Services.AddScoped<IChartService, DataSpark.Core.Services.Charts.ChartService>();
// Core domain services
builder.Services.AddScoped<IChartDataService, DataSpark.Core.Services.Charts.ChartDataService>();
builder.Services.AddScoped<IChartValidationService, DataSpark.Core.Services.Charts.ChartValidationService>();
// Web rendering service (still presentation layer)
builder.Services.AddScoped<IChartRenderingService, DataSpark.Core.Services.Charts.ChartRenderingService>();
// ViewModel builder to thin controllers
builder.Services.AddScoped<IChartConfigurationViewModelBuilder, DataSpark.Core.Services.ChartConfigurationViewModelBuilder>();

// Add memory cache services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<WebSpark.HttpClientUtility.MemoryCache.IMemoryCacheManager, WebSpark.HttpClientUtility.MemoryCache.MemoryCacheManager>();

// Add Bootswatch theme switcher services (includes StyleCache)
builder.Services.AddBootswatchThemeSwitcher();

// Register IHttpContextAccessor as required by Bootswatch for theme switching tag helper
builder.Services.AddHttpContextAccessor();

// Register WebSpark.HttpClientUtility services required by Bootswatch
builder.Services.AddScoped<WebSpark.HttpClientUtility.RequestResult.IHttpRequestResultService, WebSpark.HttpClientUtility.RequestResult.HttpRequestResultService>();

// Configure OpenAI options
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<SampleDataOptions>(builder.Configuration.GetSection(SampleDataOptions.SectionName));
builder.Services.AddScoped<ISampleDataService, SampleDataService>();

// Validate OpenAI configuration in development
if (builder.Environment.IsDevelopment())
{
    var openAiConfig = builder.Configuration.GetSection("OpenAI");
    if (string.IsNullOrEmpty(openAiConfig["ApiKey"]) || string.IsNullOrEmpty(openAiConfig["AssistantId"]))
    {
        Log.Warning(
            "OpenAI configuration is missing; AI features will be disabled until configured. Configure with: dotnet user-secrets set \"OpenAI:ApiKey\" \"your-api-key\" and dotnet user-secrets set \"OpenAI:AssistantId\" \"your-assistant-id\"");
    }
}

// Register HttpClient and OpenAIFileAnalysisService
builder.Services.AddHttpClient<OpenAIFileAnalysisService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Use all Bootswatch features (includes StyleCache and static files)
app.UseBootswatchAll();

app.UseRouting();

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseMiddleware<ApiKeyAuthMiddleware>());

// Add session middleware
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
