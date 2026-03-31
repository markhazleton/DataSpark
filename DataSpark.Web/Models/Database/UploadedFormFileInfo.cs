using DataSpark.Core.Interfaces;

namespace DataSpark.Web.Models.Database;

/// <summary>
/// Adapter from ASP.NET IFormFile to core file abstraction.
/// </summary>
public sealed class UploadedFormFileInfo : IUploadedFileInfo
{
    private readonly IFormFile _formFile;

    public UploadedFormFileInfo(IFormFile formFile)
    {
        _formFile = formFile ?? throw new ArgumentNullException(nameof(formFile));
    }

    public string FileName => _formFile.FileName;

    public long Length => _formFile.Length;

    public Stream OpenReadStream() => _formFile.OpenReadStream();

    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        return _formFile.CopyToAsync(target, cancellationToken);
    }
}
