namespace Node.Web.Models.Matches;

/// <summary>
/// The chat screen for one match. The messages themselves no longer live in
/// our database: the view connects directly to GetStream Chat in the browser
/// using this data (the API key is public, the token is short-lived and only
/// usable by the logged-in user).
/// </summary>
public class ChatViewModel
{
    public int MatchId { get; set; }

    public string OtherDisplayName { get; set; } = string.Empty;

    public int CompatibilityScore { get; set; }

    public string? CompatibilityExplanation { get; set; }

    /// <summary>GetStream API key (public, not a secret).</summary>
    public string StreamApiKey { get; set; } = string.Empty;

    /// <summary>Short-lived GetStream token for the logged-in user.</summary>
    public string StreamUserToken { get; set; } = string.Empty;

    public string CurrentUserId { get; set; } = string.Empty;

    public string OtherUserId { get; set; } = string.Empty;
}
