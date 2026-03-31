namespace DataSpark.Core.Models;

public class CsvFileReadResult<T>
{
    public bool Success { get; set; }
    public List<T> Data { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
