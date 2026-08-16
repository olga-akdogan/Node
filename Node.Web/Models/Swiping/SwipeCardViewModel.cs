namespace Node.Web.Models.Swiping;

/// <summary>
/// One profile card in the discover stack (in the style of Tinder/Bumble:
/// one candidate at a time, with a pass and like button below it).
/// </summary>
public class SwipeCardViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Profile picture of the candidate; null = no photo uploaded yet (initial-letter avatar as fallback).</summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>Age, calculated from the birth date.</summary>
    public int Age { get; set; }

    public string? Bio { get; set; }

    public string BirthPlace { get; set; } = string.Empty;

    /// <summary>Compatibility score with the logged-in user (null = not calculable yet).</summary>
    public int? CompatibilityScore { get; set; }

    /// <summary>Claude-written playful "compatibility test" blurb for this candidate (see ISwipeTeaserService).</summary>
    public string? CompatibilityExplanation { get; set; }

    /// <summary>Claude-written first-date suggestion that ties back to the compatibility test above.</summary>
    public string? DateIdea { get; set; }
}
