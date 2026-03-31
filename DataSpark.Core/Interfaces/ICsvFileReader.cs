using Microsoft.Data.Analysis;
using DataSpark.Core.Models;

namespace DataSpark.Core.Interfaces;

/// <summary>
/// Minimal abstraction allowing Core service to obtain DataFrames and raw records without referencing web implementation.
/// </summary>
public interface ICsvFileReader
{
    Task<CsvFileReadResult<DataFrame>> ReadCsvAsDataFrameAsync(string fileName, char delimiter, bool allString);
}
