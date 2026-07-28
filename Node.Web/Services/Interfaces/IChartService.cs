using Node.Web.Models.Chart;

namespace Node.Web.Services.Interfaces;

/// <summary>De horoscooppagina van een gebruiker samenstellen.</summary>
public interface IChartService
{
    /// <summary>
    /// Bouwt het volledige horoscoopoverzicht van de gebruiker.
    /// Null wanneer er (nog) geen berekende horoscoop bestaat.
    /// </summary>
    Task<HoroscoopViewModel?> GetHoroscoopAsync(string userId);
}
