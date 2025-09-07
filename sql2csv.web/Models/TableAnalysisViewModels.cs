using System.ComponentModel.DataAnnotations;
using Sql2Csv.Core.Models;

namespace Sql2Csv.Web.Models;

/// <summary>
/// Web-specific view model for table analysis results
/// </summary>
public class TableAnalysisViewModel
{
    public required string DatabaseName { get; init; }
    public required string TableName { get; init; }
    public required string FilePath { get; init; }
    public TableStatisticsViewModel? Statistics { get; init; }
    public List<ColumnAnalysisViewModel> ColumnAnalyses { get; init; } = [];
    public TimeSpan AnalysisDuration { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// Creates from core model
    /// </summary>
    public static TableAnalysisViewModel FromCore(TableAnalysisResult coreResult)
    {
        return new TableAnalysisViewModel
        {
            DatabaseName = coreResult.DatabaseName,
            TableName = coreResult.TableName,
            FilePath = coreResult.FilePath,
            Statistics = coreResult.Statistics != null ? TableStatisticsViewModel.FromCore(coreResult.Statistics) : null,
            ColumnAnalyses = coreResult.ColumnAnalyses.Select(ColumnAnalysisViewModel.FromCore).ToList(),
            AnalysisDuration = coreResult.AnalysisDuration,
            ErrorMessage = coreResult.ErrorMessage,
            IsSuccess = coreResult.IsSuccess
        };
    }
}

/// <summary>
/// Web-specific view model for table-level statistics
/// </summary>
public class TableStatisticsViewModel
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

    public string FormattedSize => FormatFileSize(EstimatedSizeBytes);

    public double CompletionPercentage => TotalRows > 0
        ? Math.Round((1.0 - (double)NullableColumns / TotalColumns) * 100, 1)
        : 0;

    /// <summary>
    /// Creates from core model
    /// </summary>
    public static TableStatisticsViewModel FromCore(TableStatistics coreModel)
    {
        return new TableStatisticsViewModel
        {
            TotalRows = coreModel.TotalRows,
            TotalColumns = coreModel.TotalColumns,
            NumericColumns = coreModel.NumericColumns,
            TextColumns = coreModel.TextColumns,
            DateTimeColumns = coreModel.DateTimeColumns,
            NullableColumns = coreModel.NullableColumns,
            PrimaryKeyColumns = coreModel.PrimaryKeyColumns,
            DataQualityScore = coreModel.DataQualityScore,
            EstimatedSizeBytes = coreModel.EstimatedSizeBytes
        };
    }

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
/// Web-specific view model for column-level analysis
/// </summary>
public record ColumnAnalysisViewModel
{
    public required string ColumnName { get; init; }
    public required string DataType { get; init; }
    public bool IsNullable { get; init; }
    public bool IsPrimaryKey { get; init; }
    public string? DefaultValue { get; init; }

    // Basic statistics
    public long TotalCount { get; init; }
    public long NullCount { get; init; }
    public long UniqueCount { get; init; }
    public long DuplicateCount => TotalCount - UniqueCount;

    // Calculated percentages
    public double NullPercentage => TotalCount > 0 ? Math.Round((double)NullCount / TotalCount * 100, 2) : 0;
    public double UniquenessPercentage => TotalCount > 0 ? Math.Round((double)UniqueCount / TotalCount * 100, 2) : 0;
    public double CompletenessPercentage => Math.Round(100 - NullPercentage, 2);

    // Numeric statistics (nullable for non-numeric columns)
    public double? MinValue { get; init; }
    public double? MaxValue { get; init; }
    public double? MeanValue { get; init; }
    public double? MedianValue { get; init; }
    public double? StandardDeviation { get; init; }
    public double? Range => MaxValue.HasValue && MinValue.HasValue ? MaxValue - MinValue : null;

    // String statistics (for text columns)
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public double? AverageLength { get; init; }

    // Date statistics (for datetime columns)
    public DateTime? MinDate { get; init; }
    public DateTime? MaxDate { get; init; }
    public TimeSpan? DateRange => MaxDate.HasValue && MinDate.HasValue ? MaxDate - MinDate : null;

    // Value frequencies (top 10 most common values)
    public List<ValueFrequencyViewModel> TopValues { get; init; } = [];

    // Data quality indicators
    public bool HasOutliers { get; init; }
    public bool HasInconsistentFormat { get; init; }
    public double DataQualityScore { get; init; }

    // Column type classification
    public bool IsNumeric => DataType.ToUpperInvariant() switch
    {
        "INTEGER" or "REAL" or "NUMERIC" or "DECIMAL" or "FLOAT" or "DOUBLE" => true,
        _ => false
    };

    public bool IsText => DataType.ToUpperInvariant() switch
    {
        "TEXT" or "VARCHAR" or "CHAR" or "STRING" => true,
        _ => false
    };

    public bool IsDateTime => DataType.ToUpperInvariant() switch
    {
        "DATETIME" or "DATE" or "TIME" or "TIMESTAMP" => true,
        _ => false
    };

    public string QualityColor => DataQualityScore switch
    {
        >= 0.9 => "text-green-600 bg-green-100",
        >= 0.7 => "text-yellow-600 bg-yellow-100",
        >= 0.5 => "text-orange-600 bg-orange-100",
        _ => "text-red-600 bg-red-100"
    };

    /// <summary>
    /// Creates from core model
    /// </summary>
    public static ColumnAnalysisViewModel FromCore(ColumnAnalysis coreModel)
    {
        return new ColumnAnalysisViewModel
        {
            ColumnName = coreModel.ColumnName,
            DataType = coreModel.DataType,
            IsNullable = coreModel.IsNullable,
            IsPrimaryKey = coreModel.IsPrimaryKey,
            DefaultValue = coreModel.DefaultValue,
            TotalCount = coreModel.TotalCount,
            NullCount = coreModel.NullCount,
            UniqueCount = coreModel.UniqueCount,
            MinValue = double.TryParse(coreModel.MinValue, out var min) ? min : null,
            MaxValue = double.TryParse(coreModel.MaxValue, out var max) ? max : null,
            MeanValue = coreModel.MeanValue,
            MedianValue = coreModel.MedianValue,
            MinLength = coreModel.MinLength,
            MaxLength = coreModel.MaxLength,
            AverageLength = coreModel.AverageLength,
            MinDate = coreModel.MinDate,
            MaxDate = coreModel.MaxDate,
            TopValues = coreModel.TopValues.Select(ValueFrequencyViewModel.FromCore).ToList(),
            DataQualityScore = coreModel.DataQualityScore
        };
    }
}

/// <summary>
/// Web-specific value frequency data for displaying common values
/// </summary>
public class ValueFrequencyViewModel
{
    public required string Value { get; init; }
    public long Count { get; init; }
    public double Percentage { get; init; }

    public string DisplayValue => Value?.Length > 50 ? $"{Value[..47]}..." : Value ?? "(null)";

    /// <summary>
    /// Creates from core model
    /// </summary>
    public static ValueFrequencyViewModel FromCore(ValueFrequency coreModel)
    {
        return new ValueFrequencyViewModel
        {
            Value = coreModel.Value,
            Count = coreModel.Count,
            Percentage = coreModel.Percentage
        };
    }
}
