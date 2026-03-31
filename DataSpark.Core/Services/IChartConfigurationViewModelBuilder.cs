using DataSpark.Core.Models.Charts;
using DataSpark.Core.Services.Charts;
using Microsoft.Extensions.Logging;
using DataSpark.Core.Interfaces;
using DataSpark.Core.Models;
using DataSpark.Core.Models.Analysis;

namespace DataSpark.Core.Services;

/// <summary>
/// Builds chart configuration view models and supplies default configurations.
/// Presentation-layer abstraction to thin controllers; wraps core data & chart services.
/// </summary>
public interface IChartConfigurationViewModelBuilder
{
    /// <summary>
    /// Create a default in-memory chart configuration scaffold for a given data source.
    /// </summary>
    ChartConfiguration CreateDefaultConfiguration(string dataSource);

    /// <summary>
    /// Build a rich configuration view model including available columns, palettes, chart types, saved configs, etc.
    /// </summary>
    Task<ChartConfigurationViewModel> BuildAsync(ChartConfiguration configuration, string dataSource);
}

/// <summary>
/// Implementation of <see cref="IChartConfigurationViewModelBuilder"/> that orchestrates core services.
/// </summary>
public class ChartConfigurationViewModelBuilder : IChartConfigurationViewModelBuilder
{
    private readonly IChartDataService _dataService;
    private readonly IChartService _chartService;
    private readonly ILogger<ChartConfigurationViewModelBuilder> _logger;

    public ChartConfigurationViewModelBuilder(IChartDataService dataService, IChartService chartService, ILogger<ChartConfigurationViewModelBuilder> logger)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _chartService = chartService ?? throw new ArgumentNullException(nameof(chartService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ChartConfiguration CreateDefaultConfiguration(string dataSource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataSource);

        var config = new ChartConfiguration
        {
            Name = "New Chart",
            CsvFile = dataSource,
            ChartType = "Column",
            ChartStyle = "2D",
            ChartPalette = "BrightPastel",
            Width = 800,
            Height = 400,
            Title = "Chart Title",
            ShowLegend = true,
            IsAnimated = true
        };

        // Provide initial series & axis scaffolding for the UI
        config.Series.Add(new ChartSeries
        {
            Name = "Series 1",
            DataColumn = string.Empty,
            AggregationFunction = "Sum",
            IsVisible = true,
            DisplayOrder = 1
        });

        config.XAxis = new ChartAxis
        {
            AxisType = "X",
            DataColumn = string.Empty,
            Title = "Category"
        };

        return config;
    }

    public async Task<ChartConfigurationViewModel> BuildAsync(ChartConfiguration configuration, string dataSource)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        ArgumentException.ThrowIfNullOrWhiteSpace(dataSource);

        _logger.LogDebug("Building ChartConfigurationViewModel for data source {DataSource} (ConfigId={Id})", dataSource, configuration.Id);

        var availableColumns = await _dataService.GetColumnsAsync(dataSource).ConfigureAwait(false);
        var columnValues = new Dictionary<string, List<string>>();

        // Capture representative values for categorical columns (limited for perf)
        var categoricalColumns = availableColumns.Where(c => c.IsCategory || !c.IsNumeric).Take(10);
        foreach (var column in categoricalColumns)
        {
            try
            {
                var values = await _dataService.GetColumnValuesAsync(dataSource, column.Column, 100).ConfigureAwait(false);
                columnValues[column.Column] = values;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load sample values for column {Column}", column.Column);
            }
        }

        return new ChartConfigurationViewModel
        {
            Configuration = configuration,
            AvailableColumns = availableColumns,
            AvailableChartTypes = ChartTypes.GetNames(),
            ChartTypes = ChartTypes.GetNames(),
            ColorPalettes = ColorPalettes.GetNames(),
            ColumnValues = columnValues,
            DataSource = dataSource,
            IsEditMode = configuration.Id > 0,
            SavedConfigurations = await _chartService.GetConfigurationsAsync(dataSource).ConfigureAwait(false)
        };
    }
}
