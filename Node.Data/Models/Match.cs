using System.ComponentModel.DataAnnotations;
using Node.Data.Models.Enums;

namespace Node.Data.Models;

/// <summary>
/// Een match tussen twee gebruikers die elkaar geliket hebben.
/// De compatibiliteitsscore wordt berekend uit beide geboortehoroscopen
/// (synastrie) en bewaard op het moment dat de match ontstaat.
/// Afspraak: User1Id &lt; User2Id (alfabetisch) zodat een paar maar één keer
/// kan voorkomen (unieke index in de DbContext).
/// </summary>
public class Match
{
    public int Id { get; set; }

    [Required]
    public string User1Id { get; set; } = string.Empty;

    public ApplicationUser? User1 { get; set; }

    [Required]
    public string User2Id { get; set; } = string.Empty;

    public ApplicationUser? User2 { get; set; }

    /// <summary>Astrologische compatibiliteitsscore (0 t.e.m. 100).</summary>
    [Range(0, 100)]
    public int CompatibilityScore { get; set; }

    /// <summary>Korte uitleg bij de score, getoond aan beide gebruikers.</summary>
    [MaxLength(2000)]
    public string? CompatibilityExplanation { get; set; }

    /// <summary>Toestand van de match (actief of beëindigd).</summary>
    [Required]
    public MatchStatus Status { get; set; } = MatchStatus.Active;

    public DateTime MatchedAt { get; set; } = DateTime.UtcNow;
}
