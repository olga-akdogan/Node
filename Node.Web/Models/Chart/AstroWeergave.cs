using Microsoft.Extensions.Localization;
using Node.Data.Models.Enums;
using Node.Web.Resources;

namespace Node.Web.Models.Chart;

/// <summary>
/// Weergavehulp voor astrologie: Unicode-glyphs (taalonafhankelijk) en
/// meertalige namen voor hemellichamen en dierenriemtekens. De namen komen
/// uit de gedeelde resource-bestanden, aangeleverd door de aanroepende view.
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

    /// <summary>Meertalige naam van een hemellichaam.</summary>
    public static string Naam(CelestialBody lichaam, IStringLocalizer<SharedResource> localizer) => localizer[lichaam switch
    {
        CelestialBody.Sun => "Astro_Sun",
        CelestialBody.Moon => "Astro_Moon",
        CelestialBody.Mercury => "Astro_Mercury",
        CelestialBody.Venus => "Astro_Venus",
        CelestialBody.Mars => "Astro_Mars",
        CelestialBody.Jupiter => "Astro_Jupiter",
        CelestialBody.Saturn => "Astro_Saturn",
        CelestialBody.Uranus => "Astro_Uranus",
        CelestialBody.Neptune => "Astro_Neptune",
        CelestialBody.Pluto => "Astro_Pluto",
        CelestialBody.Ascendant => "Astro_Ascendant",
        _ => lichaam.ToString(),
    }];

    /// <summary>Meertalige naam van een dierenriemteken.</summary>
    public static string Naam(ZodiacSign teken, IStringLocalizer<SharedResource> localizer) => localizer[teken switch
    {
        ZodiacSign.Aries => "Astro_Aries",
        ZodiacSign.Taurus => "Astro_Taurus",
        ZodiacSign.Gemini => "Astro_Gemini",
        ZodiacSign.Cancer => "Astro_Cancer",
        ZodiacSign.Leo => "Astro_Leo",
        ZodiacSign.Virgo => "Astro_Virgo",
        ZodiacSign.Libra => "Astro_Libra",
        ZodiacSign.Scorpio => "Astro_Scorpio",
        ZodiacSign.Sagittarius => "Astro_Sagittarius",
        ZodiacSign.Capricorn => "Astro_Capricorn",
        ZodiacSign.Aquarius => "Astro_Aquarius",
        ZodiacSign.Pisces => "Astro_Pisces",
        _ => teken.ToString(),
    }];

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

    /// <summary>
    /// Rangtelwoord van een huisnummer (1-12) in de huidige UI-taal: "8e" (nl/fr),
    /// "8th" (en). Engelse rangtelwoorden volgen geen vaste regel (1st/2nd/3rd/4th...),
    /// vandaar de expliciete lijst in plaats van een algemene formule.
    /// </summary>
    public static string HuisOrdinaal(int huis) => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "en" => huis switch { 1 => "1st", 2 => "2nd", 3 => "3rd", _ => $"{huis}th" },
        "fr" => huis == 1 ? "1er" : $"{huis}e",
        _ => $"{huis}e", // nl: altijd "e" (1e, 2e, 3e, ...)
    };
}
