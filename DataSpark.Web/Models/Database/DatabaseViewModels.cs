using DataSpark.Core.Models;

namespace DataSpark.Web.Models.Database;

public sealed class DatabaseIndexViewModel
{
    public List<PersistedDatabaseFile> Files { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public sealed class DatabaseAnalyzeViewModel
{
    public PersistedDatabaseFile? File { get; set; }
    public DatabaseAnalysisResult? Analysis { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class DatabaseTableViewModel
{
    public PersistedDatabaseFile? File { get; set; }
    public TableAnalysisResult? Analysis { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class DatabaseExportResultsViewModel
{
    public PersistedDatabaseFile? File { get; set; }
    public List<ExportResult> Results { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public sealed class DatabaseCodeResultsViewModel
{
    public PersistedDatabaseFile? File { get; set; }
    public string NamespaceName { get; set; } = "DataSpark.Generated";
    public List<GeneratedCodeResult> Results { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
