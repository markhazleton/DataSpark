using System.ComponentModel.DataAnnotations;

namespace Sql2Csv.Web.Models;

/// <summary>
/// View model for file upload
/// </summary>
public class FileUploadViewModel
{
    [Required(ErrorMessage = "Please select a database file")]
    [Display(Name = "Database File")]
    public IFormFile? DatabaseFile { get; set; }

    public string? ErrorMessage { get; set; }
    public bool IsValid => string.IsNullOrEmpty(ErrorMessage);

    [Display(Name = "Save file for future use")]
    public bool SaveForFutureUse { get; set; }

    [Display(Name = "File Description")]
    public string? FileDescription { get; set; }

    public List<PersistedDatabaseFile> AvailableFiles { get; set; } = [];

    [Display(Name = "Use existing file")]
    public string? SelectedFileId { get; set; }
}

/// <summary>
/// View model for database analysis results
/// </summary>
public class DatabaseAnalysisViewModel
{
    public required string DatabaseName { get; init; }
    public required string FilePath { get; init; }
    public List<TableInfoViewModel> Tables { get; init; } = [];
    public string? SchemaReport { get; init; }
    public TimeSpan AnalysisDuration { get; init; }
}

/// <summary>
/// View model for table information
/// </summary>
public class TableInfoViewModel
{
    public required string Name { get; init; }
    public string Schema { get; init; } = "main";
    public long RowCount { get; init; }
    public List<ColumnInfoViewModel> Columns { get; init; } = [];
    public bool HasPrimaryKey => Columns.Any(c => c.IsPrimaryKey);
}

/// <summary>
/// View model for column information
/// </summary>
public class ColumnInfoViewModel
{
    public required string Name { get; init; }
    public required string DataType { get; init; }
    public bool IsNullable { get; init; }
    public bool IsPrimaryKey { get; init; }
    public string? DefaultValue { get; init; }
}

/// <summary>
/// View model for export operations
/// </summary>
public class ExportViewModel
{
    public required string DatabaseName { get; init; }
    public required string TableName { get; init; }
    public List<string> SelectedTables { get; init; } = [];
    public ExportFormat Format { get; init; } = ExportFormat.CSV;
    public bool IncludeHeaders { get; init; } = true;
    public string Delimiter { get; init; } = ",";
}

/// <summary>
/// View model for export results
/// </summary>
public class ExportResultViewModel
{
    public required string TableName { get; init; }
    public required string FileName { get; init; }
    public required string FileContent { get; init; }
    public int RowCount { get; init; }
    public TimeSpan Duration { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// View model for code generation
/// </summary>
public class CodeGenerationViewModel
{
    public required string DatabaseName { get; init; }
    public List<string> SelectedTables { get; init; } = [];
    public string NamespaceName { get; init; } = "Generated.Models";
    public CodeLanguage Language { get; init; } = CodeLanguage.CSharp;
}

/// <summary>
/// View model for generated code results
/// </summary>
public class GeneratedCodeViewModel
{
    public required string TableName { get; init; }
    public required string ClassName { get; init; }
    public required string Code { get; init; }
    public CodeLanguage Language { get; init; }
}

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
/// View model for managing persisted files
/// </summary>
public class FileManagementViewModel
{
    public List<PersistedDatabaseFile> Files { get; set; } = [];
    public long TotalStorageSize { get; set; }
    public string FormattedStorageSize => FormatFileSize(TotalStorageSize);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// View model for viewing table data
/// </summary>
public class ViewDataViewModel
{
    public required string TableName { get; init; }
    public required string DatabaseName { get; init; }
    public required string FilePath { get; init; }
    public List<ColumnInfoViewModel> Columns { get; init; } = [];
}
