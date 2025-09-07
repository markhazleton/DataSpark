namespace Sql2Csv.Core.Models;

/// <summary>
/// Available export formats
/// </summary>
public enum ExportFormat
{
    CSV,
    JSON,
    XML
}

/// <summary>
/// Available code generation languages
/// </summary>
public enum CodeLanguage
{
    CSharp,
    TypeScript,
    Python
}

/// <summary>
/// View model for database analysis results
/// </summary>
public class DatabaseAnalysisResult
{
    public required string DatabaseName { get; init; }
    public required string FilePath { get; init; }
    public List<TableInfo> Tables { get; init; } = [];
    public string? SchemaReport { get; init; }
    public TimeSpan AnalysisDuration { get; init; }
}

/// <summary>
/// View model for generated code results
/// </summary>
public class GeneratedCodeResult
{
    public required string TableName { get; init; }
    public required string ClassName { get; init; }
    public required string Code { get; init; }
    public CodeLanguage Language { get; init; }
}

/// <summary>
/// Table analysis result
/// </summary>
public class TableAnalysisResult
{
    public required string DatabaseName { get; init; }
    public required string TableName { get; init; }
    public required string FilePath { get; init; }
    public TableStatistics? Statistics { get; init; }
    public List<ColumnAnalysis> ColumnAnalyses { get; init; } = [];
    public TimeSpan AnalysisDuration { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Table statistics
/// </summary>
public class TableStatistics
{
    public long TotalRows { get; init; }
    public int TotalColumns { get; init; }
    public int NumericColumns { get; init; }
    public int TextColumns { get; init; }
    public int DateTimeColumns { get; init; }
    public int NullableColumns { get; init; }
    public int PrimaryKeyColumns { get; init; }
    public double DataQualityScore { get; init; }
    public long EstimatedSizeBytes { get; init; }
}

/// <summary>
/// Value frequency information
/// </summary>
public class ValueFrequency
{
    public required string Value { get; init; }
    public long Count { get; init; }
    public double Percentage { get; init; }
}

/// <summary>
/// DataTables request model for server-side processing
/// </summary>
public class DataTablesRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public string? SearchValue { get; set; }
    public List<DataTablesOrder> Order { get; set; } = [];
    public List<DataTablesColumn> Columns { get; set; } = [];
}

/// <summary>
/// DataTables ordering information
/// </summary>
public class DataTablesOrder
{
    public int Column { get; set; }
    public string Dir { get; set; } = "asc";
}

/// <summary>
/// DataTables column information
/// </summary>
public class DataTablesColumn
{
    public string Data { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Searchable { get; set; }
    public bool Orderable { get; set; }
}

/// <summary>
/// DataTables result
/// </summary>
public class TableDataResult
{
    public int Draw { get; set; }
    public long RecordsTotal { get; set; }
    public long RecordsFiltered { get; set; }
    public object?[][] Data { get; set; } = [];
    public List<string> Columns { get; set; } = [];
    public string? Error { get; set; }
}
