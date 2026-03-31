namespace DataSpark.Core.Models;

// Pivot table specific DTOs (remain web-layer concerns)
public class PivotTableViewModel
{
    public string CurrentFile { get; set; } = string.Empty;
    public List<string> AvailableFiles { get; set; } = new();
    public List<string> ColumnHeaders { get; set; } = new();
    public int RecordCount { get; set; }
    public List<Dictionary<string, object>> Data { get; set; } = new();
}

public class LoadCsvDataRequest
{
    public string FileName { get; set; } = string.Empty;
    public List<string>? SelectedColumns { get; set; }
}

public class LoadCsvDataResponse
{
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public List<string> Columns { get; set; } = new();
    public int RecordCount { get; set; }
}

public class SaveConfigurationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CsvFile { get; set; } = string.Empty;
    public string AggregatorName { get; set; } = string.Empty;
    public string RendererName { get; set; } = string.Empty;
    public List<string> Cols { get; set; } = new();
    public List<string> Rows { get; set; } = new();
    public List<string> Vals { get; set; } = new();
    public Dictionary<string, object> IncludeValues { get; set; } = new();
    public Dictionary<string, object> ExcludeValues { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class PivotTableConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CsvFile { get; set; } = string.Empty;
    public string AggregatorName { get; set; } = string.Empty;
    public string RendererName { get; set; } = string.Empty;
    public List<string> Cols { get; set; } = new();
    public List<string> Rows { get; set; } = new();
    public List<string> Vals { get; set; } = new();
    public Dictionary<string, object> IncludeValues { get; set; } = new();
    public Dictionary<string, object> ExcludeValues { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class StandardResponse
{
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
