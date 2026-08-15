using Node.Data.Models;
using Node.Data.Models.Enums;

namespace Node.Data.Data;

/// <summary>
/// Deterministische synastriescore op basis van de elementen van zon, maan
/// en ascendant: zelfde element scoort hoog, complementaire elementen
/// (vuur+lucht, aarde+water) scoren goed, andere combinaties gemiddeld.
/// De score blijft bewust eenvoudig en uitlegbaar (geen aspectenleer); de
/// rijkere interpretatietekst bij een match komt van
/// <see cref="Node.Web.Services.Interfaces.IMatchInterpretationService"/>,
/// dat deze score als vast gegeven meekrijgt.
/// </summary>
public static class DemoSynastrie
{
    public static (int Score, SynastryConclusion Conclusion) Bereken(NatalChart a, NatalChart b)
    {
        // De tekens staan in klassieke volgorde, dus teken modulo 4 geeft het
        // element: 0 = vuur, 1 = aarde, 2 = lucht, 3 = water.
        static int Element(ZodiacSign teken) => (int)teken % 4;

        static int ScoorPaar(ZodiacSign x, ZodiacSign y)
        {
            var (ex, ey) = (Element(x), Element(y));
            return ex == ey
                ? 88
                : (ex, ey) is (0, 2) or (2, 0) or (1, 3) or (3, 1)
                    ? 76
                    : 58;
        }

        // De "grote drie" (zon, maan, ascendant) wegen elk even zwaar mee.
        var zonScore = ScoorPaar(a.SunSign, b.SunSign);
        var maanScore = ScoorPaar(a.MoonSign, b.MoonSign);
        var ascendantScore = ScoorPaar(a.AscendantSign, b.AscendantSign);
        var score = (int)Math.Round((zonScore + maanScore + ascendantScore) / 3.0);

        // De ruwe tekens worden bewust niet in de conclusie opgenomen: die
        // wordt in de weblaag vertaald naar de meertalige, essentiële tekst
        // die de gebruiker te zien krijgt (zie de Synastry_* resx-sleutels).
        var conclusion = score >= 85 ? SynastryConclusion.HighAffinity
            : score >= 70 ? SynastryConclusion.Complementary
            : SynastryConclusion.Different;

        return (score, conclusion);
    }
}
