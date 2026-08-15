using System.ComponentModel.DataAnnotations;
using Node.Data.Models.Enums;

namespace Node.Data.Models;

/// <summary>
/// De geboortehoroscoop van één gebruiker (1-op-1 relatie).
/// De afzonderlijke planeetposities staan in <see cref="Placement"/>;
/// de zon-, maan- en ascendanttekens worden hier gedenormaliseerd bewaard
/// zodat overzichtspagina's er eenvoudig op kunnen filteren en sorteren.
/// </summary>
public class NatalChart
{
    public int Id { get; set; }

    /// <summary>De gebruiker bij wie deze horoscoop hoort.</summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    /// <summary>
    /// Het geboortemoment omgerekend naar UT (wereldtijd).
    /// Belangrijk: de omrekening gebeurt met de historische tijdzone van de
    /// geboorteplaats, niet met een vaste offset.
    /// </summary>
    [Required]
    public DateTime BirthMomentUtc { get; set; }

    /// <summary>Zonneteken (gedenormaliseerd voor filteren).</summary>
    [Required]
    public ZodiacSign SunSign { get; set; }

    /// <summary>Maanteken (gedenormaliseerd voor filteren).</summary>
    [Required]
    public ZodiacSign MoonSign { get; set; }

    /// <summary>Ascendant / rijzend teken (gedenormaliseerd voor filteren).</summary>
    [Required]
    public ZodiacSign AscendantSign { get; set; }

    /// <summary>
    /// True wanneer de gebruiker geen exacte geboortetijd opgaf. De Ascendant en
    /// de huizen van alle plaatsingen zijn dan berekend op een conventionele
    /// tijd (bv. 12:00) en dus niet betrouwbaar; de zon-, maan- en planeettekens
    /// zelf blijven wel correct.
    /// </summary>
    public bool AscendantIsApproximate { get; set; }

    /// <summary>Tijdstip waarop de horoscoop (her)berekend werd (UTC).</summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Alle berekende posities (planeet per teken/huis/graad).</summary>
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
