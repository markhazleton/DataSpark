// Re-export Core models for Web usage
global using PersistedDatabaseFile = Sql2Csv.Core.Models.PersistedDatabaseFile;

using System.ComponentModel.DataAnnotations;

namespace Sql2Csv.Web.Models;

/// <summary>
/// Model for updating file description - Web specific extension
/// </summary>
public class UpdateFileDescriptionModel
{
    [Required]
    public required string FileId { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }
}
