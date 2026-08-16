using Node.Data.Models;
using Node.Data.Models.Enums;

namespace Node.Data.Data;

/// <summary>
/// Deterministic synastry score based on the elements of sun, moon and
/// ascendant: same element scores high, complementary elements (fire+air,
/// earth+water) score well, other combinations score average. The score
/// stays deliberately simple and explainable (no aspect theory); the richer
/// interpretation text for a match comes from
/// <see cref="Node.Web.Services.Interfaces.IMatchInterpretationService"/>
/// and the swipe-card teaser from
/// <see cref="Node.Web.Services.Interfaces.ISwipeTeaserService"/>, both of
/// which receive this score as a fixed input.
/// </summary>
public static class DemoSynastry
{
    public static (int Score, SynastryConclusion Conclusion) Calculate(NatalChart a, NatalChart b)
    {
        // Signs are in classical order, so sign modulo 4 gives the element:
        // 0 = fire, 1 = earth, 2 = air, 3 = water.
        static int Element(ZodiacSign sign) => (int)sign % 4;

        static int ScorePair(ZodiacSign x, ZodiacSign y)
        {
            var (ex, ey) = (Element(x), Element(y));
            return ex == ey
                ? 88
                : (ex, ey) is (0, 2) or (2, 0) or (1, 3) or (3, 1)
                    ? 76
                    : 58;
        }

        // The "big three" (sun, moon, ascendant) each weigh equally.
        var sunScore = ScorePair(a.SunSign, b.SunSign);
        var moonScore = ScorePair(a.MoonSign, b.MoonSign);
        var ascendantScore = ScorePair(a.AscendantSign, b.AscendantSign);
        var score = (int)Math.Round((sunScore + moonScore + ascendantScore) / 3.0);

        var conclusion = score >= 85 ? SynastryConclusion.HighAffinity
            : score >= 70 ? SynastryConclusion.Complementary
            : SynastryConclusion.Different;

        return (score, conclusion);
    }
}
