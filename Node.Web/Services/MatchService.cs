using Microsoft.EntityFrameworkCore;
using Node.Data.Data;
using Node.Data.Models;
using Node.Data.Models.Enums;
using Node.Web.Models.Matches;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

/// <summary>
/// Matches en chatgesprekken. Elke methode controleert eerst of de gebruiker
/// wel deelnemer is van de match (autorisatie op gegevensniveau).
/// </summary>
public class MatchService : IMatchService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MatchService> _logger;

    public MatchService(ApplicationDbContext context, ILogger<MatchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<MatchOverviewViewModel>> GetMatchesVoorGebruikerAsync(string userId)
    {
        var matches = await _context.Matches
            .Include(m => m.User1)
            .Include(m => m.User2)
            .Include(m => m.ChatMessages)
            .Where(m => m.Status == MatchStatus.Active
                        && (m.User1Id == userId || m.User2Id == userId))
            .ToListAsync();

        return matches
            .Select(m =>
            {
                var ander = m.User1Id == userId ? m.User2 : m.User1;
                var laatste = m.ChatMessages.OrderByDescending(c => c.SentAt).FirstOrDefault();

                return new MatchOverviewViewModel
                {
                    MatchId = m.Id,
                    OtherDisplayName = ander?.DisplayName ?? "Onbekend",
                    CompatibilityScore = m.CompatibilityScore,
                    LastMessagePreview = laatste?.Content,
                    LastMessageAt = laatste?.SentAt,
                    UnreadCount = m.ChatMessages.Count(c => c.SenderUserId != userId && !c.IsRead),
                };
            })
            // Recentste gesprek bovenaan; matches zonder gesprek daaronder.
            .OrderByDescending(v => v.LastMessageAt ?? DateTime.MinValue)
            .ToList();
    }

    public async Task<ChatViewModel?> GetChatAsync(int matchId, string userId)
    {
        var match = await ZoekMatchVanDeelnemerAsync(matchId, userId);
        if (match is null)
        {
            return null;
        }

        var berichten = await _context.ChatMessages
            .Where(c => c.MatchId == matchId)
            .OrderBy(c => c.SentAt)
            .ToListAsync();

        // Berichten van de ander markeren als gelezen nu ik het gesprek open.
        foreach (var bericht in berichten.Where(b => b.SenderUserId != userId && !b.IsRead))
        {
            bericht.IsRead = true;
        }

        await _context.SaveChangesAsync();

        var ander = match.User1Id == userId ? match.User2 : match.User1;

        return new ChatViewModel
        {
            MatchId = match.Id,
            OtherDisplayName = ander?.DisplayName ?? "Onbekend",
            CompatibilityScore = match.CompatibilityScore,
            CompatibilityExplanation = match.CompatibilityExplanation,
            Messages = berichten
                .Select(b => new ChatBerichtViewModel
                {
                    Content = b.Content,
                    SentAt = b.SentAt,
                    IsMine = b.SenderUserId == userId,
                })
                .ToList(),
        };
    }

    public async Task<ChatBerichtViewModel?> StuurBerichtAsync(int matchId, string userId, string content)
    {
        var match = await ZoekMatchVanDeelnemerAsync(matchId, userId);
        if (match is null || match.Status != MatchStatus.Active)
        {
            return null;
        }

        var bericht = new ChatMessage
        {
            MatchId = matchId,
            SenderUserId = userId,
            Content = content,
        };

        _context.ChatMessages.Add(bericht);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Bericht verstuurd in match {MatchId} door {UserId}.", matchId, userId);

        return new ChatBerichtViewModel
        {
            Content = bericht.Content,
            SentAt = bericht.SentAt,
            IsMine = true,
        };
    }

    /// <summary>
    /// Zoekt de match, maar alleen wanneer de gebruiker er deelnemer van is.
    /// </summary>
    private async Task<Match?> ZoekMatchVanDeelnemerAsync(int matchId, string userId)
    {
        return await _context.Matches
            .Include(m => m.User1)
            .Include(m => m.User2)
            .FirstOrDefaultAsync(m => m.Id == matchId
                                      && (m.User1Id == userId || m.User2Id == userId));
    }
}
