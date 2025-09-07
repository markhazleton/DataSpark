using Sql2Csv.Core.Interfaces;

namespace Sql2Csv.Web.Models;

/// <summary>
/// Web implementation of IUploadedFileInfo that wraps IFormFile
/// </summary>
public class FormFileWrapper : IUploadedFileInfo
{
    private readonly IFormFile _formFile;

    public FormFileWrapper(IFormFile formFile)
    {
        _formFile = formFile ?? throw new ArgumentNullException(nameof(formFile));
    }

    public string FileName => _formFile.FileName;

    public long Length => _formFile.Length;

    public Stream OpenReadStream()
    {
        return _formFile.OpenReadStream();
    }

    public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        await _formFile.CopyToAsync(target, cancellationToken);
    }
}
