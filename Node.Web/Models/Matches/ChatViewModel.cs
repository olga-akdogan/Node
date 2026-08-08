namespace Node.Web.Models.Matches;

/// <summary>
/// Het chatscherm van één match. De berichten zelf staan niet meer in onze
/// databank: de view verbindt in de browser rechtstreeks met GetStream Chat
/// via deze gegevens (API-sleutel is publiek, het token is kort geldig en
/// enkel bruikbaar door de ingelogde gebruiker).
/// </summary>
public class ChatViewModel
{
    public int MatchId { get; set; }

    public string OtherDisplayName { get; set; } = string.Empty;

    public int CompatibilityScore { get; set; }

    public string? CompatibilityExplanation { get; set; }

    /// <summary>GetStream API-sleutel (publiek, geen geheim).</summary>
    public string StreamApiKey { get; set; } = string.Empty;

    /// <summary>Kort geldig GetStream-token voor de ingelogde gebruiker.</summary>
    public string StreamUserToken { get; set; } = string.Empty;

    public string CurrentUserId { get; set; } = string.Empty;

    public string OtherUserId { get; set; } = string.Empty;
}
