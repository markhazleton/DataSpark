using System.ComponentModel.DataAnnotations;
using Sql2Csv.Core.Models;

namespace Sql2Csv.Web.Models;

/// <summary>
/// View model for file upload - now supports both database and CSV files
/// </summary>
public class FileUploadViewModel
{
    [Required(ErrorMessage = "Please select a data file")]
    [Display(Name = "Data File (Database or CSV)")]
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
/// Web-specific view model for database analysis results
/// </summary>
public class DatabaseAnalysisViewModel
{
    public required string DatabaseName { get; init; }
    public required string FilePath { get; init; }
    public List<TableInfoViewModel> Tables { get; init; } = [];
    public string? SchemaReport { get; init; }
    public TimeSpan AnalysisDuration { get; init; }

    /// <summary>
    /// Creates from core model
    /// </summary>
    public static DatabaseAnalysisViewModel FromCore(DatabaseAnalysisResult coreResult)
    {
        return new DatabaseAnalysisViewModel
        {
            DatabaseName = coreResult.DatabaseName,
            FilePath = coreResult.FilePath,
            Tables = coreResult.Tables.Select(TableInfoViewModel.FromCore).ToList(),
            SchemaReport = coreResult.SchemaReport,
            AnalysisDuration = coreResult.AnalysisDuration
        };
    }
}

/// <summary>
/// Web-specific view model for table information
/// </summary>
public class TableInfoViewModel
{
    public required string Name { get; init; }
    public string TableName => Name; // Alias for compatibility
    public string Schema { get; init; } = "main";
    public long RowCount { get; init; }
    public List<ColumnInfoViewModel> Columns { get; init; } = [];
    public int ColumnCount => Columns.Count; // Calculated property
    public bool HasPrimaryKey => Columns.Any(c => c.IsPrimaryKey);

    /// <summary>
    /// Creates from core model
    /// </summary>
    public static TableInfoViewModel FromCore(TableInfo coreModel)
    {
        return new TableInfoViewModel
        {
            Name = coreModel.Name,
            Schema = coreModel.Schema,
            RowCount = coreModel.RowCount,
            Columns = coreModel.Columns.Select(ColumnInfoViewModel.FromCore).ToList()
        };
    }
}

/// <summary>
/// Web-specific view model for column information
/// </summary>
public class ColumnInfoViewModel
{
    public required string Name { get; init; }
    public required string DataType { get; init; }
    public bool IsNullable { get; init; }
    public bool IsPrimaryKey { get; init; }
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Creates from core model
    /// </summary>
    public static ColumnInfoViewModel FromCore(ColumnInfo coreModel)
    {
        return new ColumnInfoViewModel
        {
            Name = coreModel.Name,
            DataType = coreModel.DataType,
            IsNullable = coreModel.IsNullable,
            IsPrimaryKey = coreModel.IsPrimaryKey,
            DefaultValue = coreModel.DefaultValue
        };
    }
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
/// Web-specific view model for export results
/// </summary>
public class ExportResultViewModel
{
    public required string TableName { get; init; }
    public required string FileName { get; init; }
    public required string FileContent { get; init; }
    public string? FilePath { get; init; }
    public int RowCount { get; init; }
    public TimeSpan Duration { get; init; }
    public bool IsSuccess { get; init; }
    public bool Success => IsSuccess; // Alias for compatibility
    public string? ErrorMessage { get; init; }
    public string? Message => ErrorMessage; // Alias for compatibility

    /// <summary>
    /// Creates from core model
    /// </summary>
    public static ExportResultViewModel FromCore(ExportResult coreResult)
    {
        return new ExportResultViewModel
        {
            TableName = coreResult.TableName,
            FileName = coreResult.FileName,
            FileContent = coreResult.FileContent,
            FilePath = coreResult.FilePath,
            RowCount = coreResult.RowCount,
            Duration = coreResult.Duration,
            IsSuccess = coreResult.IsSuccess,
            ErrorMessage = coreResult.ErrorMessage
        };
    }
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
/// Web-specific view model for generated code results
/// </summary>
public class GeneratedCodeViewModel
{
    public required string TableName { get; init; }
    public required string ClassName { get; init; }
    public required string Code { get; init; }
    public CodeLanguage Language { get; init; }

    /// <summary>
    /// Creates from core model
    /// </summary>
    public static GeneratedCodeViewModel FromCore(GeneratedCodeResult coreResult)
    {
        return new GeneratedCodeViewModel
        {
            TableName = coreResult.TableName,
            ClassName = coreResult.ClassName,
            Code = coreResult.Code,
            Language = coreResult.Language
        };
    }
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

/// <summary>
/// View model for file selection page
/// </summary>
public class FileSelectionViewModel
{
    public List<PersistedDatabaseFile> PersistedFiles { get; init; } = [];
    public FileUploadViewModel UploadModel { get; init; } = new();
    public string? SelectedFileId { get; set; }
    public bool ShowUploadForm { get; set; } = true;
}
