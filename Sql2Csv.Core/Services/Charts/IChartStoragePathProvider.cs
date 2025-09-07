namespace Sql2Csv.Core.Services.Charts;

/// <summary>
/// Abstraction for resolving the base folder path used to persist chart configurations.
/// This allows the core repository implementation to remain UI/framework agnostic.
/// </summary>
public interface IChartStoragePathProvider
{
    /// <summary>Returns the absolute path to the folder containing chart JSON files.</summary>
    string GetChartsFolderPath();
}
