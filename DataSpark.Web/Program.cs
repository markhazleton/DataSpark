using DataSpark.Web.Services;
using DataSpark.Web.Services.Chart;
using Sql2Csv.Core.Services.Charts;
using WebSpark.Bootswatch;
using static DataSpark.Web.Services.OpenAIFileAnalysisService;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<CsvFileService>();
// Core CSV processing & export services
builder.Services.AddScoped<Sql2Csv.Core.Services.Analysis.ICsvFileReader, WebCsvFileReaderAdapter>();
builder.Services.AddScoped<Sql2Csv.Core.Services.Analysis.ICsvProcessingService, Sql2Csv.Core.Services.Analysis.CsvProcessingService>();
builder.Services.AddScoped<Sql2Csv.Core.Services.Export.IExportService, Sql2Csv.Core.Services.Export.ExportService>();

// Register chart storage provider & repository (core implementation)
builder.Services.AddScoped<IChartStoragePathProvider, WebChartStoragePathProvider>();
builder.Services.AddScoped<IChartConfigurationRepository, FileSystemChartConfigurationRepository>();
builder.Services.AddScoped<IChartService, Sql2Csv.Core.Services.Charts.ChartService>();
// Core domain services
builder.Services.AddScoped<IChartDataService, Sql2Csv.Core.Services.Charts.ChartDataService>();
builder.Services.AddScoped<IChartValidationService, Sql2Csv.Core.Services.Charts.ChartValidationService>();
// Web rendering service (still presentation layer)
builder.Services.AddScoped<IChartRenderingService, ChartRenderingService>();
// ViewModel builder to thin controllers
builder.Services.AddScoped<IChartConfigurationViewModelBuilder, ChartConfigurationViewModelBuilder>();

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

// Validate OpenAI configuration in development
if (builder.Environment.IsDevelopment())
{
    var openAiConfig = builder.Configuration.GetSection("OpenAI");
    if (string.IsNullOrEmpty(openAiConfig["ApiKey"]) || string.IsNullOrEmpty(openAiConfig["AssistantId"]))
    {
        throw new InvalidOperationException(
            "OpenAI configuration is missing. Please set the configuration using User Secrets:\n" +
            "dotnet user-secrets set \"OpenAI:ApiKey\" \"your-api-key\"\n" +
            "dotnet user-secrets set \"OpenAI:AssistantId\" \"your-assistant-id\"");
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

// Add session middleware
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
