using Node.Data.Models;

namespace Node.Data.Services;

/// <summary>
/// Writes the "My chart" interpretation texts with Claude (Anthropic): an
/// interpretation of the full natal chart, and a text about what the user is
/// looking for in a partner/relationship based on that same chart.
/// </summary>
public interface IChartInterpretationService
{
    /// <summary>
    /// Generates both texts in the requested language. Called when the chart
    /// has no interpretation yet, or when the user views the page in a
    /// language that differs from NatalChart.InterpretationLanguage.
    /// </summary>
    /// <param name="language">ISO 639-1 code of the requested language (e.g. "nl", "en", "fr").</param>
    Task<(string Interpretation, string PartnerPreferenceText)> WriteInterpretationAsync(
        ApplicationUser user, NatalChart chart, string language);
}
