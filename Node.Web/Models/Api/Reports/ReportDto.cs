namespace Node.Web.Models.Api.Reports;

/// <summary>One row in the moderation queue (Moderator/Admin only).</summary>
public class ReportDto
{
    public int Id { get; set; }

    public string ReporterDisplayName { get; set; } = string.Empty;

    public string ReportedUserId { get; set; } = string.Empty;

    public string ReportedDisplayName { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public bool IsResolved { get; set; }

    public DateTime CreatedAt { get; set; }
}
