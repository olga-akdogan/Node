using Node.Data.Models;

namespace Node.Data.Services;

/// <summary>
/// Writes the readable explanation for a match with Claude (Anthropic),
/// based on both users' full natal charts. The compatibility score itself
/// stays deterministic (<see cref="Node.Data.Data.DemoSynastry"/>); this only
/// supplies the accompanying text.
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
    Task<string> WriteMatchInterpretationAsync(
        ApplicationUser userA, NatalChart chartA,
        ApplicationUser userB, NatalChart chartB,
        int compatibilityScore,
        string language);
}
