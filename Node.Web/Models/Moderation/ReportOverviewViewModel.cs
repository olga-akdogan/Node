namespace Node.Web.Models.Moderation;

/// <summary>One row in the moderation queue (Moderator/Admin only). Shared by the web page and the REST API.</summary>
public class ReportOverviewViewModel
{
    public int Id { get; set; }

    public string ReporterDisplayName { get; set; } = string.Empty;

    public string ReportedUserId { get; set; } = string.Empty;

    public string ReportedDisplayName { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public bool IsResolved { get; set; }

    public DateTime CreatedAt { get; set; }
}
