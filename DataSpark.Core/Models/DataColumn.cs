namespace DataSpark.Core.Models;

/// <summary>
/// Inferred semantic type for a data column.
/// </summary>
public enum InferredDataType
{
    Unknown,
    Numeric,
    Categorical,
    DateTime,
    Boolean,
    Text
}

/// <summary>
/// Column profile used by exploratory analysis.
/// </summary>
public sealed class DataColumn
{
    public string Name { get; set; } = string.Empty;

    public InferredDataType InferredDataType { get; set; } = InferredDataType.Unknown;

    public long NullCount { get; set; }

    public long UniqueCount { get; set; }

    public List<string> SampleValues { get; set; } = [];

    public ColumnStatistics Statistics { get; set; } = new();
}
