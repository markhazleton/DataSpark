using Microsoft.Data.Analysis;
using DataSpark.Core.Interfaces;
using DataSpark.Core.Models;

namespace DataSpark.Web.Services;

/// <summary>
/// Adapts existing CsvFileService to Core ICsvFileReader abstraction.
/// </summary>
public class WebCsvFileReaderAdapter : ICsvFileReader
{
    private readonly CsvFileService _csvFileService;

    public WebCsvFileReaderAdapter(CsvFileService csvFileService)
    {
        _csvFileService = csvFileService;
    }

    public async Task<CsvFileReadResult<DataFrame>> ReadCsvAsDataFrameAsync(string fileName, char delimiter, bool allString)
    {
        var op = await _csvFileService.ReadCsvAsDataFrameAsync(fileName, delimiter, null, allString).ConfigureAwait(false);
        return new CsvFileReadResult<DataFrame>
        {
            Success = op.Success,
            Data = op.Data,
            ErrorMessage = op.ErrorMessage
        };
    }
}