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
    /// Generates a short interpretation text for the match between two users,
    /// in the requested language. Called when the match is created, and again
    /// whenever a user views the match in a language that differs from
    /// Match.CompatibilityExplanationLanguage.
    /// </summary>
    /// <param name="language">ISO 639-1 code of the requested language (e.g. "nl", "en", "fr").</param>
    Task<string> SchrijfInterpretatieAsync(
        ApplicationUser gebruikerA, NatalChart chartA,
        ApplicationUser gebruikerB, NatalChart chartB,
        int compatibiliteitsScore,
        string language);
}
