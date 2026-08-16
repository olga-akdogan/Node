using Node.Data.Models;

namespace Node.Data.Services;

/// <summary>Calculates a user's real birth chart via Swiss Ephemeris.</summary>
public interface INatalChartCalculator
{
    /// <summary>
    /// Calculates a full <see cref="NatalChart"/> (with all <see cref="Placement"/>s)
    /// for the given user. The user must already have BirthDate, BirthTime and
    /// the birth coordinates (BirthLatitude/BirthLongitude) filled in.
    /// Doesn't save anything: the caller adds the result to the DbContext.
    /// </summary>
    NatalChart Calculate(ApplicationUser user);
}
