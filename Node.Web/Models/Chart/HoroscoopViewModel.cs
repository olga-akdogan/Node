using Node.Data.Models.Enums;

namespace Node.Web.Models.Chart;

/// <summary>
/// De volledige horoscooppagina van de ingelogde gebruiker (ontwerp 04):
/// wiel, grote drie, plaatsingentabel en signatuurregel.
/// </summary>
public class HoroscoopViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Bijvoorbeeld "3 jan 1999 · 07:10 · Turnhout, België".</summary>
    public string GeboorteInfo { get; set; } = string.Empty;

    public ZodiacSign SunSign { get; set; }

    public ZodiacSign MoonSign { get; set; }

    public ZodiacSign AscendantSign { get; set; }

    public IList<PlaatsingRegel> Placements { get; set; } = new List<PlaatsingRegel>();

    /// <summary>Signatuurregel op basis van de elementen van zon en maan.</summary>
    public string Signatuur { get; set; } = string.Empty;
}

/// <summary>Eén rij in de plaatsingentabel, klaar voor weergave.</summary>
public class PlaatsingRegel
{
    public CelestialBody Body { get; set; }

    public ZodiacSign Sign { get; set; }

    public int Huis { get; set; }

    /// <summary>Graad binnen het teken, bv. "14°30′".</summary>
    public string Graad { get; set; } = string.Empty;

    /// <summary>
    /// Absolute eclipticale lengte in graden (tekenindex × 30 + graad),
    /// gebruikt om de glyph op het wiel te plaatsen.
    /// </summary>
    public double LengteGraden { get; set; }
}
