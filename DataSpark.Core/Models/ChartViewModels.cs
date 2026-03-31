using DataSpark.Core.Models.Charts; // Core domain
using DataSpark.Core.Models.Analysis; // ColumnInfo

namespace DataSpark.Core.Models;

// Rich view models aligned with existing controllers & Razor views. Domain & summary now reside in Core.

public class ChartIndexViewModel
{
    public List<ChartConfigurationSummary> SavedConfigurations { get; set; } = new();
    public List<string> AvailableDataSources { get; set; } = new();
    public string? ActiveDataSource { get; set; }
    public int TotalConfigurations => SavedConfigurations.Count;
    public int FavoriteCount => SavedConfigurations.Count(c => c.IsFavorite);
    public int TotalFilters => SavedConfigurations.Sum(c => c.FilterCount);
    public Dictionary<string, int> ConfigurationCounts { get; set; } = new();
    public ChartConfiguration? CurrentConfiguration { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public class ChartConfigurationViewModel
{
    public ChartConfiguration Configuration { get; set; } = new();
    public bool IsEditMode { get; set; }
    public string DataSource { get; set; } = string.Empty;
    public List<string> AvailableDataSources { get; set; } = new();
    public List<string> AvailableChartTypes { get; set; } = new();
    public List<string> ChartTypes { get; set; } = new(); // Legacy alias used in some views
    public List<Analysis.ColumnInfo> AvailableColumns { get; set; } = new();
    public List<string> NumericColumns { get; set; } = new();
    public List<string> DimensionColumns { get; set; } = new();
    public List<string> DateColumns { get; set; } = new();
    public Dictionary<string, List<string>> ColumnValues { get; set; } = new();
    public List<ExportOption> ExportOptions { get; set; } = new();
    public ProcessedChartData? PreviewData { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public List<ChartConfigurationSummary> SavedConfigurations { get; set; } = new();
    public List<string> ColorPalettes { get; set; } = new();
}

public class ChartDisplayViewModel
{
    public ChartConfiguration Configuration { get; set; } = new();
    public ProcessedChartData? Data { get; set; }
    public DataSummary? Summary { get; set; }
    public string? ChartJson { get; set; }
    public string? ChartHtml { get; set; }
    public ChartRenderResult? RenderResult { get; set; }
    public bool IsEditable { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ExportOption> ExportOptions { get; set; } = new();
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public static ApiResponse<T> Ok(T data, string? message = null) => new() { Success = true, Data = data, Message = message };
    public static ApiResponse<T> Fail(string message, List<string>? errors = null) => new() { Success = false, Message = message, Errors = errors };
    // Legacy method names used in controllers
    public static ApiResponse<T> SuccessResult(T data, string? message = null) => Ok(data, message);
    public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null) => Fail(message, errors);
}

public class ChartDataRequest
{
    public int ConfigurationId { get; set; }
    public List<FilterRequest> Filters { get; set; } = new();
    // Extended properties expected by controllers
    public List<string> YColumns { get; set; } = new();
    public string AggregationFunction { get; set; } = "Sum";
    public string? XColumn { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
    public int? MaxRows { get; set; }
}

public class FilterRequest
{
    public string Column { get; set; } = string.Empty;
    public string FilterType { get; set; } = string.Empty;
    public List<string> IncludedValues { get; set; } = new();
    public List<string> ExcludedValues { get; set; } = new();
    // Legacy single list alias (mapped to IncludedValues)
    public List<string> Values
    {
        get => IncludedValues;
        set => IncludedValues = value;
    }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public string? Pattern { get; set; }
    public bool IsCaseSensitive { get; set; }
    public bool IsRegex { get; set; }
}

public class ValidationRequest
{
    public ChartConfiguration Configuration { get; set; } = new();
    public string DataSource { get; set; } = string.Empty;
    public bool IncludeDataCheck { get; set; } = true;
}

public class ExportOption
{
    // Extended properties for UI/export support
    public string Key { get; set; } = string.Empty; // e.g., png, csv
    public string Name { get; set; } = string.Empty; // Display name
    public string Description { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public bool IncludeHeaders { get; set; } = true; // For data exports
    public bool IsEnabled { get; set; } = true;
}

public class ChartPreviewRequest
{
    public ChartConfiguration Configuration { get; set; } = new();
    public List<FilterRequest> Filters { get; set; } = new();
    public string DataSource { get; set; } = string.Empty;
    public int? MaxRows { get; set; }
    public int? MaxDataPoints { get; set; }
    public bool IncludeData { get; set; } = true;
}

public class ChartSharingConfig
{
    public bool IsPublic { get; set; }
    public string? ShareToken { get; set; }
    public DateTime? ExpiresUtc { get; set; }
}

public class ChartTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ChartType { get; set; } = string.Empty;
    public ChartConfiguration? BaseConfiguration { get; set; }
}

public class ChartTemplates
{
    public List<ChartTemplate> Templates { get; set; } = new();
    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
}

public class BulkOperationRequest
{
    public List<int> ConfigurationIds { get; set; } = new();
    public string Operation { get; set; } = string.Empty; // delete, favorite, export
    public Dictionary<string, string>? Parameters { get; set; }
}
