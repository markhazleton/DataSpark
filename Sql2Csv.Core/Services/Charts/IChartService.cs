using Sql2Csv.Core.Models.Charts;

namespace Sql2Csv.Core.Services.Charts;

/// <summary>
/// Application-level chart configuration service abstraction.
/// </summary>
public interface IChartService
{
    Task<ChartConfiguration?> GetConfigurationAsync(int id);
    Task<ChartConfiguration> SaveConfigurationAsync(ChartConfiguration config);
    Task<bool> DeleteConfigurationAsync(int id);
    Task<List<ChartConfigurationSummary>> GetConfigurationsAsync(string? dataSource = null);
    Task<ChartConfiguration?> GetConfigurationByNameAsync(string name, string dataSource);
    Task<bool> ConfigurationExistsAsync(string name, string dataSource, int? excludeId = null);
    Task<ChartConfiguration> DuplicateConfigurationAsync(int id, string newName);
    Task<List<ChartConfiguration>> GetConfigurationsByIdsAsync(List<int> ids);
    Task<int> DeleteConfigurationsAsync(List<int> ids);
}
