using Node.Web.Models.Chart;

namespace Node.Web.Services.Interfaces;

/// <summary>Assembles a user's natal chart page.</summary>
public interface IChartService
{
    /// <summary>
    /// Builds the user's full natal chart overview.
    /// Null when no chart has been calculated (yet).
    /// </summary>
    Task<HoroscopeViewModel?> GetHoroscopeAsync(string userId);
}
