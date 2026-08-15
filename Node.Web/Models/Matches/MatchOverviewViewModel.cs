namespace Node.Web.Models.Matches;

/// <summary>
/// Eén rij in het matchoverzicht (naar het voorbeeld van Bumble: naam, score,
/// laatste bericht en het aantal ongelezen berichten).
/// </summary>
public class MatchOverviewViewModel
{
    public int MatchId { get; set; }

    public string OtherDisplayName { get; set; } = string.Empty;

    /// <summary>Profielfoto van de ander; null = nog geen foto geüpload (letter-avatar als terugval).</summary>
    public string? OtherProfilePictureUrl { get; set; }

    public int CompatibilityScore { get; set; }

    /// <summary>Voorbeeld van het laatste bericht in het gesprek (null = nog geen berichten).</summary>
    public string? LastMessagePreview { get; set; }

    public DateTime? LastMessageAt { get; set; }

    /// <summary>Aantal berichten van de ander die ik nog niet gelezen heb.</summary>
    public int UnreadCount { get; set; }
}
