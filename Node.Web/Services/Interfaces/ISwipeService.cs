using Node.Web.Models.Swiping;

namespace Node.Web.Services.Interfaces;

/// <summary>
/// Discovering and rating candidates (the swipe stack).
/// </summary>
public interface ISwipeService
{
    /// <summary>
    /// Finds the next candidate for the logged-in user: a member who hasn't
    /// been rated yet. Null when the stack is empty.
    /// </summary>
    Task<SwipeCardViewModel?> GetNextCandidateAsync(string userId);

    /// <summary>
    /// Registers a like or pass. A mutual like results in a match; the
    /// result indicates whether that happened.
    /// </summary>
    Task<(bool IsMatch, int? MatchId)> RateAsync(string swiperUserId, string targetUserId, bool isLike);
}
