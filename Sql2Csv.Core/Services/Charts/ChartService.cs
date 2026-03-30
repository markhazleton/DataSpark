using Sql2Csv.Core.Models.Charts;
using Microsoft.Extensions.Logging;

namespace Sql2Csv.Core.Services.Charts;

/// <summary>
/// Default implementation of <see cref="IChartService"/> providing CRUD and duplication logic
/// for <see cref="ChartConfiguration"/> instances using the configured repository & validation service.
/// </summary>
public class ChartService : IChartService
{
    private readonly IChartConfigurationRepository _repository;
    private readonly IChartValidationService _validationService;
    private readonly ILogger<ChartService> _logger;

    public ChartService(
        IChartConfigurationRepository repository,
        IChartValidationService validationService,
        ILogger<ChartService> logger)
    {
        _repository = repository;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<ChartConfiguration?> GetConfigurationAsync(int id)
    {
        try
        {
            return await _repository.GetByIdAsync(id).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chart configuration {Id}", id);
            return null;
        }
    }

    public async Task<ChartConfiguration> SaveConfigurationAsync(ChartConfiguration config)
    {
        try
        {
            _logger.LogInformation("Saving chart configuration: {Name} (DataSource: {DataSource})", config.Name, config.CsvFile);

            var validationResult = await _validationService.ValidateConfigurationAsync(config).ConfigureAwait(false);
            if (!validationResult.IsValid)
            {
                var errorMessage = $"Configuration validation failed: {string.Join(", ", validationResult.Errors)}";
                _logger.LogWarning("Chart validation failed for '{Name}': {Errors}", config.Name, string.Join("; ", validationResult.Errors));
                throw new InvalidOperationException(errorMessage);
            }

            var existingConfig = await _repository.GetByNameAsync(config.Name, config.CsvFile).ConfigureAwait(false);
            if (existingConfig != null && existingConfig.Id != config.Id)
            {
                var errorMessage = $"A chart named '{config.Name}' already exists for this data source.";
                _logger.LogWarning("Duplicate chart name detected: {Name} for data source: {DataSource}", config.Name, config.CsvFile);
                throw new InvalidOperationException(errorMessage);
            }

            ChartConfiguration savedConfig = config.Id == 0
                ? await _repository.CreateAsync(config)
                : await _repository.UpdateAsync(config).ConfigureAwait(false);

            _logger.LogInformation("Chart configuration {Name} saved with ID {Id}", savedConfig.Name, savedConfig.Id);
            return savedConfig;
        }
        catch (InvalidOperationException)
        {
            throw; // propagate user-friendly validation errors
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error saving chart configuration: {Name}", config.Name);
            throw new InvalidOperationException($"Failed to save chart configuration '{config.Name}'.", ex);
        }
    }

    public async Task<bool> DeleteConfigurationAsync(int id)
    {
        try
        {
            var result = await _repository.DeleteAsync(id).ConfigureAwait(false);
            if (result) _logger.LogInformation("Deleted chart configuration {Id}", id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chart configuration {Id}", id);
            return false;
        }
    }

    public async Task<List<ChartConfigurationSummary>> GetConfigurationsAsync(string? dataSource = null)
    {
        try
        {
            return await _repository.GetSummariesAsync(dataSource).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chart configurations (DataSource: {DataSource})", dataSource);
            return new List<ChartConfigurationSummary>();
        }
    }

    public async Task<ChartConfiguration?> GetConfigurationByNameAsync(string name, string dataSource)
    {
        try
        {
            return await _repository.GetByNameAsync(name, dataSource).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chart configuration by name: {Name}", name);
            return null;
        }
    }

    public async Task<bool> ConfigurationExistsAsync(string name, string dataSource, int? excludeId = null)
    {
        try
        {
            return await _repository.ExistsByNameAsync(name, dataSource, excludeId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking configuration existence: {Name}", name);
            return false;
        }
    }

    public async Task<ChartConfiguration> DuplicateConfigurationAsync(int id, string newName)
    {
        try
        {
            var originalConfig = await _repository.GetByIdAsync(id).ConfigureAwait(false) ?? throw new ArgumentException($"Configuration with ID {id} not found");

            var duplicatedConfig = originalConfig.Clone();
            duplicatedConfig.Name = newName;

            if (await _repository.ExistsByNameAsync(newName, duplicatedConfig.CsvFile))
                throw new InvalidOperationException($"A configuration with the name '{newName}' already exists");

            var savedConfig = await _repository.CreateAsync(duplicatedConfig).ConfigureAwait(false);
            _logger.LogInformation("Duplicated chart configuration {OriginalId} as {NewName}", id, newName);
            return savedConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating chart configuration {Id}", id);
            throw;
        }
    }

    public async Task<List<ChartConfiguration>> GetConfigurationsByIdsAsync(List<int> ids)
    {
        try
        {
            return await _repository.GetByIdsAsync(ids).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving multiple chart configurations");
            return new List<ChartConfiguration>();
        }
    }

    public async Task<int> DeleteConfigurationsAsync(List<int> ids)
    {
        try
        {
            var deleted = await _repository.DeleteByIdsAsync(ids).ConfigureAwait(false);
            _logger.LogInformation("Deleted {Count} chart configurations", deleted);
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting multiple chart configurations");
            return 0;
        }
    }
}
