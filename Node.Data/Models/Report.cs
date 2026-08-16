using System.ComponentModel.DataAnnotations;

namespace Node.Data.Models;

/// <summary>
/// A report of inappropriate behavior: one user reporting another.
/// Moderators handle these reports in the moderation queue.
/// </summary>
public class Report
{
    public int Id { get; set; }

    /// <summary>The user filing the report.</summary>
    [Required]
    public string ReporterUserId { get; set; } = string.Empty;

    public ApplicationUser? ReporterUser { get; set; }

    /// <summary>The user the report is about.</summary>
    [Required]
    public string ReportedUserId { get; set; } = string.Empty;

    public ApplicationUser? ReportedUser { get; set; }

    /// <summary>Reason for the report, provided by the reporter.</summary>
    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>True once a moderator has resolved the report.</summary>
    public bool IsResolved { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
