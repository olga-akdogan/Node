using Node.Data.Models.Enums;

namespace Node.Web.Models.Chart;

/// <summary>
/// The full natal chart page of the logged-in user (design 04): wheel, big
/// three, placements table and signature line.
/// </summary>
public class HoroscopeViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>E.g. "3 Jan 1999 · 07:10 · Turnhout, Belgium".</summary>
    public string BirthInfo { get; set; } = string.Empty;

    public ZodiacSign SunSign { get; set; }

    public ZodiacSign MoonSign { get; set; }

    public ZodiacSign AscendantSign { get; set; }

    /// <summary>
    /// True when the exact birth time is unknown: the Ascendant and the
    /// houses of all placements are then calculated on a conventional time
    /// and therefore not reliable.
    /// </summary>
    public bool AscendantIsApproximate { get; set; }

    public IList<PlacementRow> Placements { get; set; } = new List<PlacementRow>();

    /// <summary>Signature line based on the elements of sun and moon.</summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>Claude-written interpretation of the full natal chart.</summary>
    public string? Interpretation { get; set; }

    /// <summary>Claude-written text about what the user looks for in a partner/relationship.</summary>
    public string? PartnerPreferenceText { get; set; }
}

/// <summary>One row in the placements table, ready for display.</summary>
public class PlacementRow
{
    public CelestialBody Body { get; set; }

    public ZodiacSign Sign { get; set; }

    public int House { get; set; }

    /// <summary>Degree within the sign, e.g. "14°30′".</summary>
    public string DegreeText { get; set; } = string.Empty;

    /// <summary>
    /// Absolute ecliptic longitude in degrees (sign index × 30 + degree),
    /// used to place the glyph on the wheel.
    /// </summary>
    public double AbsoluteDegrees { get; set; }
}
