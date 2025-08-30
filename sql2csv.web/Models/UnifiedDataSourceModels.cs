using Sql2Csv.Core.Models;

namespace Sql2Csv.Web.Models;

/// <summary>
/// View model for unified data source analysis that handles both database tables and CSV files
/// </summary>
public class UnifiedDataSourceAnalysisViewModel
{
    public string FileId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DataSourceType FileType { get; set; }
    public string DataSourceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UnifiedAnalysisViewModel Analysis { get; set; } = new();
    public bool CanViewData { get; set; }
    public bool CanExport { get; set; }
    public TimeSpan AnalysisDuration { get; set; }
    public bool IsSuccess => !Analysis.Errors.Any();
    public List<string> Errors => Analysis.Errors;
}

/// <summary>
/// View model for unified data viewing with pagination support
/// </summary>
public class UnifiedDataViewViewModel
{
    public string FileId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DataSourceType FileType { get; set; }
    public string DataSourceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<ColumnInfoViewModel> Columns { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Note: ColumnInfoViewModel is already defined in ViewModels.cs and reused here

/// <summary>
/// Request model for unified DataTables server-side processing
/// </summary>
public class UnifiedDataTableRequest
{
    public string FileId { get; set; } = string.Empty;
    public DataSourceType FileType { get; set; }
    public string? DataSourceName { get; set; }
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public UnifiedDataTableSearch? Search { get; set; }
    public List<UnifiedDataTableColumn>? Columns { get; set; }
    public List<UnifiedDataTableOrder>? Order { get; set; }
}

/// <summary>
/// Column information for unified DataTables request
/// </summary>
public class UnifiedDataTableColumn
{
    public string? Data { get; set; }
    public string? Name { get; set; }
    public bool Searchable { get; set; }
    public bool Orderable { get; set; }
    public UnifiedDataTableSearch? Search { get; set; }
}

/// <summary>
/// Search information for unified DataTables
/// </summary>
public class UnifiedDataTableSearch
{
    public string? Value { get; set; }
    public bool Regex { get; set; }
}

/// <summary>
/// Order information for unified DataTables request
/// </summary>
public class UnifiedDataTableOrder
{
    public int Column { get; set; }
    public string Dir { get; set; } = "asc";
}

/// <summary>
/// View model for export results
/// </summary>
public class ExportResultsViewModel
{
    public string FileId { get; set; } = string.Empty;
    public DataSourceType FileType { get; set; }
    public List<ExportResultViewModel> Results { get; set; } = new();
    public bool HasResults => Results.Any();
    public bool AllSuccessful => Results.All(r => r.Success);
    public int SuccessCount => Results.Count(r => r.Success);
    public int ErrorCount => Results.Count(r => !r.Success);
}

/// <summary>
/// Enhanced file upload view model that supports unified data sources
/// </summary>
public class UnifiedFileUploadResultViewModel
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string FileId { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public DataSourceType FileType { get; set; }
    public List<DataSourceInfo> AvailableDataSources { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Navigation item for unified data source browsing
/// </summary>
public class DataSourceNavigationItem
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DataSourceType Type { get; set; }
    public string Url { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public int ColumnCount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "table";
}

/// <summary>
/// Summary statistics for unified data sources
/// </summary>
public class UnifiedDataSourceSummary
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DataSourceType FileType { get; set; }
    public long FileSize { get; set; }
    public int TotalDataSources { get; set; }
    public long TotalRows { get; set; }
    public int TotalColumns { get; set; }
    public DateTime LastAnalyzed { get; set; }
    public List<DataSourceNavigationItem> DataSources { get; set; } = new();
    
    public string FileSizeFormatted => FormatFileSize(FileSize);
    
    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }
}

/// <summary>
/// Configuration for unified data source access
/// </summary>
public class UnifiedDataSourceConfiguration
{
    public string FileId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DataSourceType FileType { get; set; }
    public string? DataSourceName { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
    
    /// <summary>
    /// Converts to Core DataSourceConfiguration
    /// </summary>
    public DataSourceConfiguration ToDataSourceConfiguration()
    {
        return new DataSourceConfiguration
        {
            Id = FileId,
            Name = DataSourceName ?? Path.GetFileNameWithoutExtension(FilePath),
            FilePath = FilePath,
            Type = FileType,
            TableName = FileType == DataSourceType.Database ? DataSourceName : null,
            ConnectionString = FileType == DataSourceType.Database ? $"Data Source={FilePath};Mode=ReadOnly;Cache=Shared;" : null,
            CsvDelimiter = Parameters.GetValueOrDefault("Delimiter", ","),
            CsvHasHeaders = Parameters.ContainsKey("HasHeaders") ? bool.Parse(Parameters["HasHeaders"]) : true,
            FileSize = File.Exists(FilePath) ? new FileInfo(FilePath).Length : 0,
            CreatedDate = File.Exists(FilePath) ? File.GetCreationTime(FilePath) : DateTime.UtcNow,
            LastModified = File.Exists(FilePath) ? File.GetLastWriteTime(FilePath) : DateTime.UtcNow,
            Metadata = Parameters
        };
    }
}
