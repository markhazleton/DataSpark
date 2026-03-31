using DataSpark.Core.Services.Charts;

namespace DataSpark.Web.Services.Chart;

/// <summary>
/// Resolves the charts storage folder based on the web host environment root.
/// </summary>
public class WebChartStoragePathProvider : IChartStoragePathProvider
{
    private readonly IWebHostEnvironment _env;
    private readonly string _chartsFolder;

    public WebChartStoragePathProvider(IWebHostEnvironment env)
    {
        _env = env;
        var dataFolder = Path.Combine(env.ContentRootPath, "data");
        _chartsFolder = Path.Combine(dataFolder, "charts");
        Directory.CreateDirectory(_chartsFolder);
    }

    public string GetChartsFolderPath() => _chartsFolder;
}
