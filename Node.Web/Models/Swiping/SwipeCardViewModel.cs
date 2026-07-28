namespace Node.Web.Models.Swiping;

/// <summary>
/// Eén profielkaart in de ontdek-stapel (naar het voorbeeld van Tinder/Bumble:
/// één kandidaat tegelijk, met daaronder een pas- en een like-knop).
/// </summary>
public class SwipeCardViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Leeftijd, berekend uit de geboortedatum.</summary>
    public int Age { get; set; }

    public string? Bio { get; set; }

    public string BirthPlace { get; set; } = string.Empty;

    /// <summary>Zon-, maan- en ascendantteken; null zolang er geen horoscoop is.</summary>
    public string? SunSign { get; set; }

    public string? MoonSign { get; set; }

    public string? AscendantSign { get; set; }

    /// <summary>Compatibiliteitsscore met de ingelogde gebruiker (null = nog niet berekenbaar).</summary>
    public int? CompatibilityScore { get; set; }

    public string? CompatibilityExplanation { get; set; }
}
