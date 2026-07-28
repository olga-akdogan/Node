using Node.Web.Models.Matches;

namespace Node.Web.Services.Interfaces;

/// <summary>
/// Matches en het bijbehorende chatgesprek van de ingelogde gebruiker.
/// Alle methoden controleren dat de gebruiker deelnemer van de match is.
/// </summary>
public interface IMatchService
{
    /// <summary>Alle actieve matches van de gebruiker, recentste gesprek eerst.</summary>
    Task<List<MatchOverviewViewModel>> GetMatchesVoorGebruikerAsync(string userId);

    /// <summary>
    /// Het chatscherm van één match. Markeert ontvangen berichten meteen als
    /// gelezen. Null wanneer de match niet bestaat of de gebruiker geen
    /// deelnemer is.
    /// </summary>
    Task<ChatViewModel?> GetChatAsync(int matchId, string userId);

    /// <summary>
    /// Verstuurt een bericht in een match. Null wanneer de gebruiker geen
    /// deelnemer is of de match niet meer actief is.
    /// </summary>
    Task<ChatBerichtViewModel?> StuurBerichtAsync(int matchId, string userId, string content);
}
