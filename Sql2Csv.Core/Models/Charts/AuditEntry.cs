namespace Sql2Csv.Core.Models.Charts;

/// <summary>
/// Audit entry for tracking chart configuration & related actions.
/// </summary>
public class AuditEntry
{
    public int Id { get; set; }
    public int ConfigurationId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
