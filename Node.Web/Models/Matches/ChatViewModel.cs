namespace Node.Web.Models.Matches;

/// <summary>Het volledige chatscherm van één match.</summary>
public class ChatViewModel
{
    public int MatchId { get; set; }

    public string OtherDisplayName { get; set; } = string.Empty;

    public int CompatibilityScore { get; set; }

    public string? CompatibilityExplanation { get; set; }

    public IList<ChatBerichtViewModel> Messages { get; set; } = new List<ChatBerichtViewModel>();
}

/// <summary>Eén chatbericht, klaar voor weergave als tekstballon.</summary>
public class ChatBerichtViewModel
{
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }

    /// <summary>True als de ingelogde gebruiker de afzender is (ballon rechts).</summary>
    public bool IsMine { get; set; }
}
