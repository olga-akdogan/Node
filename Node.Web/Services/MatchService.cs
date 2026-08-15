using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Node.Data.Data;
using Node.Data.Models;
using Node.Data.Models.Enums;
using Node.Data.Services;
using Node.Web.Models.Matches;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

/// <summary>
/// Matches and chat conversations. Every method first checks whether the
/// user is actually a participant in the match (authorization at the data
/// level). The chat messages themselves live in GetStream Chat, not in our database.
/// </summary>
public class MatchService : IMatchService
{
    private readonly ApplicationDbContext _context;
    private readonly IStreamChatService _streamChatService;
    private readonly IMatchInterpretationService _matchInterpretationService;
    private readonly ILogger<MatchService> _logger;

    public MatchService(
        ApplicationDbContext context,
        IStreamChatService streamChatService,
        IMatchInterpretationService matchInterpretationService,
        ILogger<MatchService> logger)
    {
        _context = context;
        _streamChatService = streamChatService;
        _matchInterpretationService = matchInterpretationService;
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

        // GetStream needs to already know the logged-in user before we can ask
        // for their channels; reuses the match data already loaded above.
        var ikzelf = (matches[0].User1Id == userId ? matches[0].User1 : matches[0].User2)!;
        await _streamChatService.ZorgVoorGebruikerAsync(ikzelf);

        // Last message + unread count per conversation come from GetStream,
        // looked up via the other user's id.
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
            // Most recent conversation first; matches without one sink to the bottom.
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

        // Both users must exist as GetStream users before they can join the
        // channel (the JS client creates the channel itself via the members
        // list, the first time either of them opens the conversation).
        await _streamChatService.ZorgVoorGebruikerAsync(ikzelf);
        await _streamChatService.ZorgVoorGebruikerAsync(ander);

        await EnsureInterpretationInCurrentLanguageAsync(match);

        _logger.LogInformation("Chat opened for match {MatchId} by {UserId}.", matchId, userId);

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
    /// If the viewing user's current language differs from the language
    /// CompatibilityExplanation was written in, the text is regenerated via
    /// Claude and the match is updated.
    /// </summary>
    private async Task EnsureInterpretationInCurrentLanguageAsync(Match match)
    {
        var currentLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (match.CompatibilityExplanation is not null && match.CompatibilityExplanationLanguage == currentLanguage)
        {
            return;
        }

        var chart1 = await _context.NatalCharts.Include(n => n.Placements).FirstOrDefaultAsync(n => n.UserId == match.User1Id);
        var chart2 = await _context.NatalCharts.Include(n => n.Placements).FirstOrDefaultAsync(n => n.UserId == match.User2Id);
        if (chart1 is null || chart2 is null || match.User1 is null || match.User2 is null)
        {
            return; // Charts not calculated yet: nothing to write the text from.
        }

        match.CompatibilityExplanation = await _matchInterpretationService.SchrijfInterpretatieAsync(
            match.User1, chart1, match.User2, chart2, match.CompatibilityScore, currentLanguage);
        match.CompatibilityExplanationLanguage = currentLanguage;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Looks up the match, but only when the user is a participant in it.
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
