using Microsoft.EntityFrameworkCore;
using Node.Data.Data;
using Node.Data.Models;
using Node.Data.Models.Enums;
using Node.Web.Models.Matches;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

/// <summary>
/// Matches en chatgesprekken. Elke methode controleert eerst of de gebruiker
/// wel deelnemer is van de match (autorisatie op gegevensniveau). De
/// chatberichten zelf staan bij GetStream Chat, niet in onze databank.
/// </summary>
public class MatchService : IMatchService
{
    private readonly ApplicationDbContext _context;
    private readonly IStreamChatService _streamChatService;
    private readonly ILogger<MatchService> _logger;

    public MatchService(ApplicationDbContext context, IStreamChatService streamChatService, ILogger<MatchService> logger)
    {
        _context = context;
        _streamChatService = streamChatService;
        _logger = logger;
    }

    public async Task<List<MatchOverviewViewModel>> GetMatchesVoorGebruikerAsync(string userId)
    {
        var matches = await _context.Matches
            .Include(m => m.User1)
            .Include(m => m.User2)
            .Where(m => m.Status == MatchStatus.Active
                        && (m.User1Id == userId || m.User2Id == userId))
            .ToListAsync();

        if (matches.Count == 0)
        {
            return [];
        }

        // GetStream moet de ingelogde gebruiker al kennen vóór er namens haar/hem
        // gevraagd kan worden naar kanalen; hergebruikt de al geladen matchdata.
        var ikzelf = (matches[0].User1Id == userId ? matches[0].User1 : matches[0].User2)!;
        await _streamChatService.ZorgVoorGebruikerAsync(ikzelf);

        // Laatste bericht + ongelezen aantal per gesprek komen bij GetStream vandaan,
        // opgezocht via de id van de andere gebruiker.
        var kanaalStatussen = await _streamChatService.GetKanaalStatussenAsync(userId);

        return matches
            .Select(m =>
            {
                var ander = m.User1Id == userId ? m.User2 : m.User1;
                kanaalStatussen.TryGetValue(ander?.Id ?? string.Empty, out var status);

                return new MatchOverviewViewModel
                {
                    MatchId = m.Id,
                    OtherDisplayName = ander?.DisplayName ?? "Onbekend",
                    OtherProfilePictureUrl = ander?.ProfilePictureUrl,
                    CompatibilityScore = m.CompatibilityScore,
                    LastMessagePreview = status?.LaatsteBerichtTekst,
                    LastMessageAt = status?.LaatsteBerichtOp,
                    UnreadCount = status?.OngelezenAantal ?? 0,
                };
            })
            // Recentste gesprek bovenaan; matches zonder gesprek daaronder.
            .OrderByDescending(v => v.LastMessageAt ?? DateTime.MinValue)
            .ToList();
    }

    public async Task<ChatViewModel?> GetChatAsync(int matchId, string userId)
    {
        var match = await ZoekMatchVanDeelnemerAsync(matchId, userId);
        if (match is null || match.User1 is null || match.User2 is null)
        {
            return null;
        }

        var ikzelf = match.User1Id == userId ? match.User1 : match.User2;
        var ander = match.User1Id == userId ? match.User2 : match.User1;

        // Beide gebruikers moeten als GetStream-gebruiker bestaan vóór ze aan
        // het kanaal kunnen deelnemen (het kanaal zelf maakt de JS-client aan
        // via de members-lijst, de eerste keer dat iemand het gesprek opent).
        await _streamChatService.ZorgVoorGebruikerAsync(ikzelf);
        await _streamChatService.ZorgVoorGebruikerAsync(ander);

        _logger.LogInformation("Chat geopend voor match {MatchId} door {UserId}.", matchId, userId);

        return new ChatViewModel
        {
            MatchId = match.Id,
            OtherDisplayName = ander.DisplayName,
            CompatibilityScore = match.CompatibilityScore,
            CompatibilityExplanation = match.CompatibilityExplanation,
            StreamApiKey = _streamChatService.ApiKey,
            StreamUserToken = _streamChatService.MaakGebruikersToken(userId),
            CurrentUserId = userId,
            OtherUserId = ander.Id,
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
