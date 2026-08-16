namespace Node.Web.Services.Interfaces;

/// <summary>
/// Integration with GetStream Chat: the external service that handles match
/// chat conversations in real time (Node.Data no longer stores messages itself).
/// [AI-generated: Claude (Sonnet 5), prompt "add GetStream Chat to my .NET
/// app, replace the custom chat with it" — adapted and documented for this
/// exam project.]
/// </summary>
public interface IStreamChatService
{
    /// <summary>GetStream API key: public, the browser needs it to connect.</summary>
    string ApiKey { get; }

    /// <summary>
    /// Creates a short-lived GetStream user token that lets the logged-in
    /// user's browser connect directly to GetStream (independent of our server).
    /// </summary>
    string CreateUserToken(string userId);

    /// <summary>
    /// Ensures the user exists as a GetStream user (creating or updating their
    /// name/photo). Required before that user can join a channel.
    /// </summary>
    Task EnsureUserExistsAsync(Node.Data.Models.ApplicationUser user);

    /// <summary>
    /// Fetches the latest chat status from GetStream for each of the user's
    /// active matches (last message + unread count), keyed by the other
    /// user's id. Matches without a conversation are absent from the result.
    /// </summary>
    Task<IReadOnlyDictionary<string, StreamChannelStatus>> GetChannelStatusesAsync(string userId);
}

/// <summary>Chat status of one GetStream channel, seen from one user's perspective.</summary>
public record StreamChannelStatus(string? LastMessageText, DateTime? LastMessageAt, int UnreadCount);
