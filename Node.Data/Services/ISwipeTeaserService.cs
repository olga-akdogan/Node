using Node.Data.Models;

namespace Node.Data.Services;

/// <summary>
/// Writes the playful "compatibility test" blurb and first-date suggestion
/// shown on a swipe card, with Claude (Anthropic), based on both users' full
/// natal charts. The compatibility score itself stays deterministic (see
/// <see cref="DemoSynastrie"/> in Node.Data.Data); this only supplies the text.
/// </summary>
public interface ISwipeTeaserService
{
    /// <summary>
    /// Generates the fun compatibility-test text and a matching first-date
    /// idea for the candidate shown to the viewer, in the requested language.
    /// Called every time a new candidate appears in the swipe stack, so the
    /// content stays fresh per viewing rather than being stored anywhere.
    /// </summary>
    /// <param name="language">ISO 639-1 code of the requested language (e.g. "nl", "en", "fr").</param>
    Task<(string CompatibilityTest, string DateIdea)> WriteTeaserAsync(
        ApplicationUser viewer, NatalChart viewerChart,
        ApplicationUser candidate, NatalChart candidateChart,
        int compatibilityScore,
        string language);
}
