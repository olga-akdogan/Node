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

    /// <summary>Tijdstip waarop de horoscoop (her)berekend werd (UTC).</summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Alle berekende posities (planeet per teken/huis/graad).</summary>
    public ICollection<Placement> Placements { get; set; } = new List<Placement>();
}
