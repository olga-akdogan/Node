namespace Node.Data.Models.Enums;

/// <summary>
/// De hemellichamen en punten die in een geboortehoroscoop (natal chart) berekend worden.
/// De Ascendant is geen hemellichaam maar wel een essentieel punt in de horoscoop,
/// daarom is hij hier mee opgenomen.
/// </summary>
public enum CelestialBody
{
    Sun,       // Zon
    Moon,      // Maan
    Mercury,   // Mercurius
    Venus,     // Venus
    Mars,      // Mars
    Jupiter,   // Jupiter
    Saturn,    // Saturnus
    Uranus,    // Uranus
    Neptune,   // Neptunus
    Pluto,     // Pluto
    Ascendant  // Ascendant (rijzend teken)
}
