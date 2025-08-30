using Sql2Csv.Core.Models;

namespace Sql2Csv.Web.Models;

/// <summary>
/// View model for unified analysis results that can handle both database and CSV files
/// </summary>
public class UnifiedAnalysisViewModel
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DataSourceType FileType { get; set; }
    public List<DataSourceInfoViewModel> DataSources { get; set; } = new();
    public DataSourceSummaryViewModel Summary { get; set; } = new();
    public List<UnifiedColumnAnalysisViewModel> ColumnAnalyses { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// View model for individual data source information (table or CSV file)
/// </summary>
public class DataSourceInfoViewModel
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public int ColumnCount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}

/// <summary>
/// View model for summary information about the data source
/// </summary>
public class DataSourceSummaryViewModel
{
    public int TotalDataSources { get; set; }
    public int TotalColumns { get; set; }
    public long TotalRows { get; set; }
    public long FileSize { get; set; }
    public DateTime AnalysisDate { get; set; }
    
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
/// View model for unified column analysis that works for both database and CSV columns
/// </summary>
public class UnifiedColumnAnalysisViewModel
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public long NonNullCount { get; set; }
    public long NullCount { get; set; }
    public long UniqueCount { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public double? Mean { get; set; }
    public double? StandardDeviation { get; set; }
    public List<string> SampleValues { get; set; } = new();
    
    public long TotalCount => NonNullCount + NullCount;
    public double NonNullPercentage => TotalCount > 0 ? (double)NonNullCount / TotalCount * 100 : 0;
    public double UniquePercentage => NonNullCount > 0 ? (double)UniqueCount / NonNullCount * 100 : 0;
}

/// <summary>
/// Information about available data sources in a file
/// </summary>
public class DataSourceInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DataSourceType Type { get; set; }
    public long RowCount { get; set; }
    public int ColumnCount { get; set; }
}

/// <summary>
/// Enhanced file upload view model that supports both database and CSV files
/// </summary>
public class UnifiedFileUploadViewModel
{
    public IFormFile? DataFile { get; set; }
    public string? SelectedFileId { get; set; }
    public bool SaveForFutureUse { get; set; }
    public string? FileDescription { get; set; }
    public string? ErrorMessage { get; set; }
    public List<PersistedDatabaseFile> AvailableFiles { get; set; } = new();
    public List<string> SupportedFileTypes { get; set; } = new() 
    { 
        ".db (SQLite Database)",
        ".sqlite (SQLite Database)", 
        ".sqlite3 (SQLite Database)",
        ".csv (Comma Separated Values)",
        ".tsv (Tab Separated Values)",
        ".txt (Text/Delimited File)"
    };
}

/// <summary>
/// View model for analyzing a specific data source (table or CSV)
/// </summary>
public class DataSourceAnalysisViewModel
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DataSourceType FileType { get; set; }
    public string DataSourceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public int ColumnCount { get; set; }
    public List<UnifiedColumnAnalysisViewModel> ColumnAnalyses { get; set; } = new();
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// View model for data viewing with unified support
/// </summary>
public class UnifiedViewDataViewModel
{
    public string DataSourceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DataSourceType FileType { get; set; }
    public List<ColumnInfoViewModel> Columns { get; set; } = new();
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}

/// <summary>
/// View model for detailed CSV analysis (similar to TableAnalysisViewModel for databases)
/// </summary>
public class CsvDetailedAnalysisViewModel
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DataSourceType FileType { get; set; }
    public DataSourceSummaryViewModel Summary { get; set; } = new();
    public List<UnifiedColumnAnalysisViewModel> ColumnAnalyses { get; set; } = new();
    public List<DataSourceInfoViewModel> DataSources { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public TimeSpan AnalysisDuration { get; set; }
    public bool IsSuccess => !Errors.Any();
}
