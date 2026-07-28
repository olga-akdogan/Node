using Node.Data.Models;
using Node.Data.Models.Enums;

namespace Node.Data.Data;

/// <summary>
/// Eenvoudige demo-synastrie op basis van de elementen van de zontekens:
/// zelfde element scoort hoog, complementaire elementen (vuur+lucht,
/// aarde+water) scoren goed, andere combinaties gemiddeld.
/// Wordt gebruikt door de seeding én door het swipen, en wordt in de
/// astrologie-fase vervangen door een echte berekening (SwissEphNet).
/// </summary>
public static class DemoSynastrie
{
    public static (int Score, string Uitleg) Bereken(NatalChart a, NatalChart b)
    {
        // De tekens staan in klassieke volgorde, dus teken modulo 4 geeft het
        // element: 0 = vuur, 1 = aarde, 2 = lucht, 3 = water.
        static int Element(ZodiacSign teken) => (int)teken % 4;

        var elementA = Element(a.SunSign);
        var elementB = Element(b.SunSign);

        var score = elementA == elementB
            ? 88
            : (elementA, elementB) is (0, 2) or (2, 0) or (1, 3) or (3, 1)
                ? 76
                : 58;

        var uitleg = $"Zon in {a.SunSign} naast zon in {b.SunSign}: " +
                     (score >= 85 ? "hetzelfde element, jullie spreken dezelfde taal."
                      : score >= 70 ? "complementaire elementen die elkaar versterken."
                      : "verschillende elementen, dus genoeg om van elkaar te leren.");

        return (score, uitleg);
    }
}
