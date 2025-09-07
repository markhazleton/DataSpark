using Microsoft.Data.Analysis;
using Sql2Csv.Core.Models;

namespace Sql2Csv.Core.Interfaces;

/// <summary>
/// Minimal abstraction allowing Core service to obtain DataFrames and raw records without referencing web implementation.
/// </summary>
public interface ICsvFileReader
{
    Task<CsvFileReadResult<DataFrame>> ReadCsvAsDataFrameAsync(string fileName, char delimiter, bool allString);
}
