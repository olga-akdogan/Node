using Node.Web.Models.Swiping;

namespace Node.Web.Services.Interfaces;

/// <summary>
/// Het ontdekken en beoordelen van kandidaten (de swipe-stapel).
/// </summary>
public interface ISwipeService
{
    /// <summary>
    /// Zoekt de volgende kandidaat voor de ingelogde gebruiker: een lid dat
    /// nog niet beoordeeld werd. Null wanneer de stapel leeg is.
    /// </summary>
    Task<SwipeCardViewModel?> GetVolgendeKandidaatAsync(string userId);

    /// <summary>
    /// Registreert een like of pass. Bij een wederzijdse like ontstaat een
    /// match; het resultaat geeft aan of dat gebeurd is.
    /// </summary>
    Task<(bool IsMatch, int? MatchId)> BeoordeelAsync(string swiperUserId, string targetUserId, bool isLike);
}
