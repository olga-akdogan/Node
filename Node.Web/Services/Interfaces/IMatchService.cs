using Node.Data.Models;

namespace Node.Web.Services.Interfaces;

public interface IMatchService
{
    Task<Match?> CreateMatchIfMutualLikeAsync(string userId, string targetUserId);

    Task<List<Match>> GetMatchesForUserAsync(string userId);
}