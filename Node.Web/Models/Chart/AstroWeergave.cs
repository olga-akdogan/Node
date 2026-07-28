using Node.Data.Models.Enums;

namespace Node.Web.Models.Chart;

/// <summary>
/// Weergavehulp voor astrologie: Unicode-glyphs en (voorlopig Nederlandse)
/// namen voor hemellichamen en dierenriemtekens. De namen verhuizen in de
/// meertaligheidsfase naar resource-bestanden.
/// </summary>
public static class AstroWeergave
{
    /// <summary>Unicode-glyph van een hemellichaam (zoals in het UI-ontwerp).</summary>
    public static string Glyph(CelestialBody lichaam) => lichaam switch
    {
        CelestialBody.Sun => "☉",
        CelestialBody.Moon => "☽",
        CelestialBody.Mercury => "☿",
        CelestialBody.Venus => "♀",
        CelestialBody.Mars => "♂",
        CelestialBody.Jupiter => "♃",
        CelestialBody.Saturn => "♄",
        CelestialBody.Uranus => "♅",
        CelestialBody.Neptune => "♆",
        CelestialBody.Pluto => "♇",
        CelestialBody.Ascendant => "↑",
        _ => "?",
    };

    /// <summary>
    /// Unicode-glyph van een dierenriemteken. De variatieselector U+FE0E
    /// dwingt tekstweergave af: zonder deze toont Windows de tekens als
    /// gekleurde emoji in plaats van als elegante symbolen.
    /// </summary>
    public static string Glyph(ZodiacSign teken) => (teken switch
    {
        ZodiacSign.Aries => "♈",
        ZodiacSign.Taurus => "♉",
        ZodiacSign.Gemini => "♊",
        ZodiacSign.Cancer => "♋",
        ZodiacSign.Leo => "♌",
        ZodiacSign.Virgo => "♍",
        ZodiacSign.Libra => "♎",
        ZodiacSign.Scorpio => "♏",
        ZodiacSign.Sagittarius => "♐",
        ZodiacSign.Capricorn => "♑",
        ZodiacSign.Aquarius => "♒",
        ZodiacSign.Pisces => "♓",
        _ => "?",
    }) + "︎";

    /// <summary>Nederlandse naam van een hemellichaam.</summary>
    public static string Naam(CelestialBody lichaam) => lichaam switch
    {
        CelestialBody.Sun => "Zon",
        CelestialBody.Moon => "Maan",
        CelestialBody.Mercury => "Mercurius",
        CelestialBody.Venus => "Venus",
        CelestialBody.Mars => "Mars",
        CelestialBody.Jupiter => "Jupiter",
        CelestialBody.Saturn => "Saturnus",
        CelestialBody.Uranus => "Uranus",
        CelestialBody.Neptune => "Neptunus",
        CelestialBody.Pluto => "Pluto",
        CelestialBody.Ascendant => "Ascendant",
        _ => lichaam.ToString(),
    };

    /// <summary>Nederlandse naam van een dierenriemteken.</summary>
    public static string Naam(ZodiacSign teken) => teken switch
    {
        ZodiacSign.Aries => "Ram",
        ZodiacSign.Taurus => "Stier",
        ZodiacSign.Gemini => "Tweelingen",
        ZodiacSign.Cancer => "Kreeft",
        ZodiacSign.Leo => "Leeuw",
        ZodiacSign.Virgo => "Maagd",
        ZodiacSign.Libra => "Weegschaal",
        ZodiacSign.Scorpio => "Schorpioen",
        ZodiacSign.Sagittarius => "Boogschutter",
        ZodiacSign.Capricorn => "Steenbok",
        ZodiacSign.Aquarius => "Waterman",
        ZodiacSign.Pisces => "Vissen",
        _ => teken.ToString(),
    };

    /// <summary>
    /// Nederlandse naam van het element van een teken (vuur/aarde/lucht/water).
    /// Zelfde rekenregel als in DemoSynastrie: tekenindex modulo 4.
    /// </summary>
    public static string Element(ZodiacSign teken) => ((int)teken % 4) switch
    {
        0 => "vuur",
        1 => "aarde",
        2 => "lucht",
        _ => "water",
    };
}
