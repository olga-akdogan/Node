using System.ComponentModel.DataAnnotations;

namespace Node.Data.Models;

/// <summary>
/// Een melding van ongepast gedrag: een gebruiker rapporteert een andere
/// gebruiker. Moderatoren behandelen deze meldingen in het beheerscherm.
/// </summary>
public class Report
{
    public int Id { get; set; }

    /// <summary>De gebruiker die de melding indient.</summary>
    [Required]
    public string ReporterUserId { get; set; } = string.Empty;

    public ApplicationUser? ReporterUser { get; set; }

    /// <summary>De gebruiker over wie de melding gaat.</summary>
    [Required]
    public string ReportedUserId { get; set; } = string.Empty;

    public ApplicationUser? ReportedUser { get; set; }

    /// <summary>Reden van de melding, opgegeven door de melder.</summary>
    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>True zodra een moderator de melding heeft afgehandeld.</summary>
    public bool IsResolved { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
