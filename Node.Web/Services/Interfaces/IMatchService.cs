using Node.Web.Models.Matches;

namespace Node.Web.Services.Interfaces;

/// <summary>
/// Matches and the logged-in user's associated chat conversation.
/// Every method checks that the user is a participant in the match.
/// The chat messages themselves come from GetStream Chat, not our database.
/// </summary>
public interface IMatchService
{
    /// <summary>All active matches of the user, most recent conversation first.</summary>
    Task<List<MatchOverviewViewModel>> GetMatchesVoorGebruikerAsync(string userId);

    /// <summary>
    /// The chat screen for one match: match data plus a GetStream token the
    /// browser uses to connect directly to the conversation. Null when the
    /// match doesn't exist, isn't active anymore, or the user isn't a participant.
    /// </summary>
    Task<ChatViewModel?> GetChatAsync(int matchId, string userId);

    /// <summary>
    /// Ends the active match (if any) between these two users, so neither can
    /// open its chat anymore. No-op if they aren't currently matched. Used
    /// when one of them reports the other, so the reporter isn't stuck
    /// chatting with someone they just reported while it's reviewed.
    /// </summary>
    Task EindigMatchTussenAsync(string userAId, string userBId);
}
