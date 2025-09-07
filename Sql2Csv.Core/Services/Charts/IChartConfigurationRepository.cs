using Sql2Csv.Core.Models.Charts;

namespace Sql2Csv.Core.Services.Charts;

/// <summary>
/// Repository abstraction for persisting and retrieving <see cref="ChartConfiguration"/> instances.
/// </summary>
public interface IChartConfigurationRepository
{
    Task<ChartConfiguration?> GetByIdAsync(int id);
    Task<ChartConfiguration?> GetByNameAsync(string name, string dataSource);
    Task<List<ChartConfiguration>> GetByDataSourceAsync(string dataSource);
    Task<List<ChartConfiguration>> GetAllAsync();
    Task<ChartConfiguration> CreateAsync(ChartConfiguration config);
    Task<ChartConfiguration> UpdateAsync(ChartConfiguration config);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> ExistsByNameAsync(string name, string dataSource, int? excludeId = null);
    Task<List<ChartConfiguration>> GetByIdsAsync(List<int> ids);
    Task<int> DeleteByIdsAsync(List<int> ids);
    Task<List<ChartConfigurationSummary>> GetSummariesAsync(string? dataSource = null);
}
