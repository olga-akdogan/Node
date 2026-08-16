using System.ComponentModel.DataAnnotations;
using Node.Data.Models.Enums;

namespace Node.Data.Models;

/// <summary>
/// A match between two users who liked each other.
/// The compatibility score is calculated from both natal charts (synastry)
/// and stored at the moment the match is created.
/// Convention: User1Id &lt; User2Id (alphabetically) so a pair can only occur
/// once (unique index in the DbContext).
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

    /// <summary>Astrological compatibility score (0 through 100).</summary>
    [Range(0, 100)]
    public int CompatibilityScore { get; set; }

    /// <summary>Short explanation of the score, shown to both users.</summary>
    [MaxLength(2000)]
    public string? CompatibilityExplanation { get; set; }

    /// <summary>
    /// Language (ISO 639-1, e.g. "nl") CompatibilityExplanation is written in.
    /// When a user views the match in a different language, the explanation
    /// is regenerated via Claude and this column is updated.
    /// </summary>
    [MaxLength(5)]
    public string? CompatibilityExplanationLanguage { get; set; }

    /// <summary>State of the match (active or ended).</summary>
    [Required]
    public MatchStatus Status { get; set; } = MatchStatus.Active;

    public DateTime MatchedAt { get; set; } = DateTime.UtcNow;
}
