using Node.Data.Models;

namespace Node.Data.Services;

/// <summary>Berekent de echte geboortehoroscoop van een gebruiker via Swiss Ephemeris.</summary>
public interface INatalChartCalculator
{
    /// <summary>
    /// Berekent een volledige <see cref="NatalChart"/> (met alle <see cref="Placement"/>s)
    /// voor de gegeven gebruiker. De gebruiker moet BirthDate, BirthTime en de
    /// geboortecoördinaten (BirthLatitude/BirthLongitude) al ingevuld hebben.
    /// Slaat niets op: de aanroeper voegt het resultaat toe aan de DbContext.
    /// </summary>
    NatalChart Calculate(ApplicationUser user);
}
