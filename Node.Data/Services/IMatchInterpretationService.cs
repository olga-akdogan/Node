using Node.Data.Models;

namespace Node.Data.Services;

/// <summary>
/// Schrijft de leesbare uitleg bij een match met Claude (Anthropic), op basis
/// van de volledige horoscopen van beide gebruikers. De compatibiliteitsscore
/// zelf blijft deterministisch (<see cref="DemoSynastrie"/> in Node.Data.Data);
/// dit levert enkel de bijhorende tekst.
/// </summary>
public interface IMatchInterpretationService
{
    /// <summary>
    /// Genereert een korte interpretatietekst voor de match tussen twee
    /// gebruikers. Wordt één keer per match opgeroepen (bij het ontstaan van
    /// de match) en het resultaat wordt bewaard op Match.CompatibilityExplanation
    /// zodat de tekst niet telkens opnieuw opgevraagd moet worden.
    /// </summary>
    Task<string> SchrijfInterpretatieAsync(
        ApplicationUser gebruikerA, NatalChart chartA,
        ApplicationUser gebruikerB, NatalChart chartB,
        int compatibiliteitsScore);
}
