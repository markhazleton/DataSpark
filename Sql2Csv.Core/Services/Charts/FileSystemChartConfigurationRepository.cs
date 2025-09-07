using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sql2Csv.Core.Models.Charts;

namespace Sql2Csv.Core.Services.Charts;

/// <summary>
/// File-system based implementation of <see cref="IChartConfigurationRepository"/> storing
/// chart configurations as JSON files (chart_{id}.json) in a folder supplied by <see cref="IChartStoragePathProvider"/>.
/// </summary>
public class FileSystemChartConfigurationRepository : IChartConfigurationRepository
{
    private readonly IChartStoragePathProvider _pathProvider;
    private readonly ILogger<FileSystemChartConfigurationRepository> _logger;
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<int, string> _fileCache = new();
    private int _nextId = 1;

    private string ChartsFolder => _pathProvider.GetChartsFolderPath();

    public FileSystemChartConfigurationRepository(IChartStoragePathProvider pathProvider, ILogger<FileSystemChartConfigurationRepository> logger)
    {
        _pathProvider = pathProvider;
        _logger = logger;
        Directory.CreateDirectory(ChartsFolder);
        InitializeNextId();
    }

    private void InitializeNextId()
    {
        try
        {
            var files = Directory.GetFiles(ChartsFolder, "chart_*.json");
            var maxId = 0;
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (name.StartsWith("chart_", StringComparison.OrdinalIgnoreCase) && int.TryParse(name[6..], out var id))
                {
                    if (id > maxId) maxId = id;
                    _fileCache[id] = file;
                }
            }
            _nextId = maxId + 1;
            _logger.LogInformation("Initialized file repo with {Count} existing charts. Next ID: {NextId}", files.Length, _nextId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed initializing next chart id");
            _nextId = 1;
        }
    }

    public async Task<ChartConfiguration?> GetByIdAsync(int id)
    {
        try
        {
            var path = GetFilePath(id);
            if (!File.Exists(path)) return null;
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<ChartConfiguration>(json, JsonOptions());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading chart {Id}", id);
            return null;
        }
    }

    public async Task<ChartConfiguration?> GetByNameAsync(string name, string dataSource)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && c.CsvFile.Equals(dataSource, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<ChartConfiguration>> GetByDataSourceAsync(string dataSource)
    {
        var all = await GetAllAsync();
        return all.Where(c => c.CsvFile.Equals(dataSource, StringComparison.OrdinalIgnoreCase)).OrderBy(c => c.Name).ToList();
    }

    public async Task<List<ChartConfiguration>> GetAllAsync()
    {
        var result = new List<ChartConfiguration>();
        try
        {
            var files = Directory.GetFiles(ChartsFolder, "chart_*.json");
            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var cfg = JsonSerializer.Deserialize<ChartConfiguration>(json, JsonOptions());
                    if (cfg != null) result.Add(cfg);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load chart file {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating chart configurations");
        }
        return result.OrderBy(c => c.Name).ToList();
    }

    public async Task<ChartConfiguration> CreateAsync(ChartConfiguration config)
    {
        lock (_lock)
        {
            config.Id = _nextId++;
            config.CreatedDate = DateTime.UtcNow;
            config.ModifiedDate = DateTime.UtcNow;
            foreach (var s in config.Series)
            {
                s.Id = _nextId++;
                s.ChartConfigurationId = config.Id;
            }
            if (config.XAxis != null) { config.XAxis.Id = _nextId++; config.XAxis.ChartConfigurationId = config.Id; }
            if (config.YAxis != null) { config.YAxis.Id = _nextId++; config.YAxis.ChartConfigurationId = config.Id; }
            if (config.Y2Axis != null) { config.Y2Axis.Id = _nextId++; config.Y2Axis.ChartConfigurationId = config.Id; }
            foreach (var f in config.Filters) { f.Id = _nextId++; f.ChartConfigurationId = config.Id; }
        }
        var path = GetFilePath(config.Id);
        var json = JsonSerializer.Serialize(config, JsonOptions());
        await File.WriteAllTextAsync(path, json);
        _fileCache[config.Id] = path;
        _logger.LogInformation("Created chart {Id} '{Name}'", config.Id, config.Name);
        return config;
    }

    public async Task<ChartConfiguration> UpdateAsync(ChartConfiguration config)
    {
        if (config.Id <= 0) throw new ArgumentException("Configuration ID must be set for update");
        config.ModifiedDate = DateTime.UtcNow;
        var path = GetFilePath(config.Id);
        var json = JsonSerializer.Serialize(config, JsonOptions());
        await File.WriteAllTextAsync(path, json);
        _logger.LogInformation("Updated chart {Id} '{Name}'", config.Id, config.Name);
        return config;
    }

    public Task<bool> DeleteAsync(int id)
    {
        try
        {
            var path = GetFilePath(id);
            if (!File.Exists(path)) return Task.FromResult(false);
            File.Delete(path);
            _fileCache.TryRemove(id, out _);
            _logger.LogInformation("Deleted chart {Id}", id);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chart {Id}", id);
            return Task.FromResult(false);
        }
    }

    public Task<bool> ExistsAsync(int id) => Task.FromResult(File.Exists(GetFilePath(id)));

    public async Task<bool> ExistsByNameAsync(string name, string dataSource, int? excludeId = null)
    {
        var all = await GetAllAsync();
        return all.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && c.CsvFile.Equals(dataSource, StringComparison.OrdinalIgnoreCase) && (excludeId == null || c.Id != excludeId));
    }

    public async Task<List<ChartConfiguration>> GetByIdsAsync(List<int> ids)
    {
        var result = new List<ChartConfiguration>();
        foreach (var id in ids)
        {
            var cfg = await GetByIdAsync(id);
            if (cfg != null) result.Add(cfg);
        }
        return result;
    }

    public async Task<int> DeleteByIdsAsync(List<int> ids)
    {
        var count = 0;
        foreach (var id in ids)
        {
            if (await DeleteAsync(id)) count++;
        }
        return count;
    }

    public async Task<List<ChartConfigurationSummary>> GetSummariesAsync(string? dataSource = null)
    {
        var all = await GetAllAsync();
        var query = all.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(dataSource))
            query = query.Where(c => c.CsvFile.Equals(dataSource, StringComparison.OrdinalIgnoreCase));
        return query.Select(c => new ChartConfigurationSummary
        {
            Id = c.Id,
            Name = c.Name,
            CsvFile = c.CsvFile,
            DataSource = c.CsvFile ?? string.Empty,
            ChartType = c.ChartType,
            CreatedDate = c.CreatedDate,
            ModifiedDate = c.ModifiedDate,
            CreatedBy = c.CreatedBy,
            SeriesCount = c.Series.Count,
            FilterCount = c.Filters.Count,
            Description = $"{c.ChartType} chart with {c.Series.Count} series"
        }).OrderBy(s => s.Name).ToList();
    }

    private string GetFilePath(int id)
    {
        if (_fileCache.TryGetValue(id, out var cached) && File.Exists(cached)) return cached;
        var path = Path.Combine(ChartsFolder, $"chart_{id}.json");
        _fileCache[id] = path;
        return path;
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}
