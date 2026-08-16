using Microsoft.Extensions.Localization;
using Node.Data.Models.Enums;
using Node.Web.Resources;

namespace Node.Web.Models.Chart;

/// <summary>
/// Display helper for astrology: Unicode glyphs (language-independent) and
/// multilingual names for celestial bodies and zodiac signs. The names come
/// from the shared resource files, supplied by the calling view.
/// </summary>
public static class AstroDisplay
{
    /// <summary>Unicode glyph of a celestial body (as in the UI design).</summary>
    public static string Glyph(CelestialBody body) => body switch
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
    /// Unicode glyph of a zodiac sign. The variation selector U+FE0E forces
    /// text rendering: without it, Windows shows the characters as colored
    /// emoji instead of elegant symbols.
    /// </summary>
    public static string Glyph(ZodiacSign sign) => (sign switch
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

    /// <summary>Multilingual name of a celestial body.</summary>
    public static string Name(CelestialBody body, IStringLocalizer<SharedResource> localizer) => localizer[body switch
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
        _ => body.ToString(),
    }];

    /// <summary>Multilingual name of a zodiac sign.</summary>
    public static string Name(ZodiacSign sign, IStringLocalizer<SharedResource> localizer) => localizer[sign switch
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
        _ => sign.ToString(),
    }];

    /// <summary>
    /// Element index of a sign (0 = fire, 1 = earth, 2 = air, 3 = water).
    /// Same rule as in DemoSynastry: sign index modulo 4.
    /// </summary>
    private static int ElementIndex(ZodiacSign sign) => (int)sign % 4;

    private static readonly string[] ElementNameKeys =
        ["Astro_Element_Fire", "Astro_Element_Earth", "Astro_Element_Air", "Astro_Element_Water"];

    /// <summary>Multilingual name of the element (fire/earth/air/water) of a sign.</summary>
    public static string ElementName(ZodiacSign sign, IStringLocalizer<SharedResource> localizer) =>
        localizer[ElementNameKeys[ElementIndex(sign)]];

    /// <summary>
    /// Poetic signature line on the natal chart page, based on the elements
    /// of sun and moon (same rule as DemoSynastry).
    /// </summary>
    public static string Signature(ZodiacSign sun, ZodiacSign moon, IStringLocalizer<SharedResource> localizer)
    {
        var (sunElement, moonElement) = (ElementIndex(sun), ElementIndex(moon));

        var closingKey = (sunElement, moonElement) switch
        {
            var (a, b) when a == b => "Chart_Signature_SameElement",
            (0, 3) or (3, 0) => "Chart_Signature_FireWater",
            (1, 2) or (2, 1) => "Chart_Signature_EarthAir",
            (0, 2) or (2, 0) => "Chart_Signature_FireAir",
            (1, 3) or (3, 1) => "Chart_Signature_EarthWater",
            _ => "Chart_Signature_Neutral",
        };

        return string.Format(
            localizer["Chart_Signature_Template"],
            ElementName(sun, localizer), ElementName(moon, localizer), localizer[closingKey]);
    }

    /// <summary>
    /// Ordinal of a house number (1-12) in the current UI language: "8e" (nl/fr),
    /// "8th" (en). English ordinals don't follow a fixed rule (1st/2nd/3rd/4th...),
    /// hence the explicit list instead of a general formula.
    /// </summary>
    public static string HouseOrdinal(int house) => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "en" => house switch { 1 => "1st", 2 => "2nd", 3 => "3rd", _ => $"{house}th" },
        "fr" => house == 1 ? "1er" : $"{house}e",
        _ => $"{house}e", // nl: always "e" (1e, 2e, 3e, ...)
    };
}
