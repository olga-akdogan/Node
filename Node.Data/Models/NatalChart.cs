using System.ComponentModel.DataAnnotations;
using Node.Data.Models.Enums;

namespace Node.Data.Models;

/// <summary>
/// One user's natal chart (1-to-1 relation).
/// The individual planet positions live in <see cref="Placement"/>;
/// the Sun, Moon and Ascendant signs are denormalized here so index
/// pages can easily filter and sort on them.
/// </summary>
public class NatalChart
{
    public int Id { get; set; }

    /// <summary>The user this natal chart belongs to.</summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    /// <summary>
    /// The birth moment converted to UT (universal time).
    /// Important: the conversion uses the birthplace's historical time zone,
    /// not a fixed offset.
    /// </summary>
    [Required]
    public DateTime BirthMomentUtc { get; set; }

    /// <summary>Sun sign (denormalized for filtering).</summary>
    [Required]
    public ZodiacSign SunSign { get; set; }

    /// <summary>Moon sign (denormalized for filtering).</summary>
    [Required]
    public ZodiacSign MoonSign { get; set; }

    /// <summary>Ascendant / rising sign (denormalized for filtering).</summary>
    [Required]
    public ZodiacSign AscendantSign { get; set; }

    /// <summary>
    /// True when the user didn't provide an exact birth time. The Ascendant and
    /// every placement's house are then calculated on a conventional time
    /// (12:00) and therefore unreliable; the Sun, Moon and planet signs
    /// themselves remain correct.
    /// </summary>
    public bool AscendantIsApproximate { get; set; }

    /// <summary>Timestamp when the natal chart was (re)calculated (UTC).</summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>All calculated positions (planet per sign/house/degree).</summary>
    public ICollection<Placement> Placements { get; set; } = new List<Placement>();

    /// <summary>Claude-written interpretation of the full natal chart.</summary>
    [MaxLength(3000)]
    public string? InterpretationText { get; set; }

    /// <summary>Claude-written text about what the user looks for in a partner/relationship, based on the chart.</summary>
    [MaxLength(2000)]
    public string? PartnerLookingForText { get; set; }

    /// <summary>
    /// Language (ISO 639-1, e.g. "nl") the texts above are written in. When
    /// the user's current language selection differs, the texts are
    /// regenerated via Claude and this column is updated.
    /// </summary>
    [MaxLength(5)]
    public string? InterpretationLanguage { get; set; }
}
