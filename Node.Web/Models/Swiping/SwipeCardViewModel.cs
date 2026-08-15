namespace Node.Web.Models.Swiping;

/// <summary>
/// Eén profielkaart in de ontdek-stapel (naar het voorbeeld van Tinder/Bumble:
/// één kandidaat tegelijk, met daaronder een pas- en een like-knop).
/// </summary>
public class SwipeCardViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Profile picture of the candidate; null = no photo uploaded yet (initial-letter avatar as fallback).</summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>Leeftijd, berekend uit de geboortedatum.</summary>
    public int Age { get; set; }

    public string? Bio { get; set; }

    public string BirthPlace { get; set; } = string.Empty;

    /// <summary>Compatibiliteitsscore met de ingelogde gebruiker (null = nog niet berekenbaar).</summary>
    public int? CompatibilityScore { get; set; }

    /// <summary>The most essential synastry conclusion, already localized (see DemoSynastrie/SwipeService).</summary>
    public string? CompatibilityExplanation { get; set; }
}
