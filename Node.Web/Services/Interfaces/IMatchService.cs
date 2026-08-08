using Node.Web.Models.Matches;

namespace Node.Web.Services.Interfaces;

/// <summary>
/// Matches en het bijbehorende chatgesprek van de ingelogde gebruiker.
/// Alle methoden controleren dat de gebruiker deelnemer van de match is.
/// De chatberichten zelf komen van GetStream Chat, niet uit onze databank.
/// </summary>
public interface IMatchService
{
    /// <summary>Alle actieve matches van de gebruiker, recentste gesprek eerst.</summary>
    Task<List<MatchOverviewViewModel>> GetMatchesVoorGebruikerAsync(string userId);

    /// <summary>
    /// Het chatscherm van één match: matchgegevens plus een GetStream-token
    /// waarmee de browser rechtstreeks met het gesprek verbindt. Null wanneer
    /// de match niet bestaat of de gebruiker geen deelnemer is.
    /// </summary>
    Task<ChatViewModel?> GetChatAsync(int matchId, string userId);
}
