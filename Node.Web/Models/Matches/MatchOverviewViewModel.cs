namespace Node.Web.Models.Matches;

/// <summary>
/// One row in the match overview (in the style of Bumble: name, score,
/// last message and the number of unread messages).
/// </summary>
public class MatchOverviewViewModel
{
    public int MatchId { get; set; }

    public string OtherDisplayName { get; set; } = string.Empty;

    /// <summary>Profile picture of the other person; null = no photo uploaded yet (initial-letter avatar as fallback).</summary>
    public string? OtherProfilePictureUrl { get; set; }

    public int CompatibilityScore { get; set; }

    /// <summary>Preview of the last message in the conversation (null = no messages yet).</summary>
    public string? LastMessagePreview { get; set; }

    public DateTime? LastMessageAt { get; set; }

    /// <summary>Number of messages from the other person I haven't read yet.</summary>
    public int UnreadCount { get; set; }
}
