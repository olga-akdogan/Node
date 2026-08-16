using System.Text;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Node.Data.Models;

namespace Node.Data.Services;

/// <summary>
/// Calls the Claude API to write a readable synastry
/// interpretation based on both full natal charts. The compatibility score
/// itself doesn't come from Claude (it stays deterministic, see
/// DemoSynastry) — only the accompanying text does.
/// </summary>
public class MatchInterpretationService : IMatchInterpretationService
{
    private const string Model = "claude-haiku-4-5";

    private readonly AnthropicClient _client;
    private readonly ILogger<MatchInterpretationService> _logger;

    public MatchInterpretationService(AnthropicClient client, ILogger<MatchInterpretationService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<string> WriteMatchInterpretationAsync(
        ApplicationUser userA, NatalChart chartA,
        ApplicationUser userB, NatalChart chartB,
        int compatibilityScore,
        string language)
    {
        try
        {
            var response = await _client.Messages.Create(new MessageCreateParams
            {
                Model = Model,
                MaxTokens = 400,
                System = BuildSystemPrompt(language),
                Messages = [new() { Role = Role.User, Content = BuildPrompt(userA, chartA, userB, chartB, compatibilityScore) }],
            });

            var text = response.Content
                .Select(b => b.Value)
                .OfType<TextBlock>()
                .FirstOrDefault()?.Text;

            return string.IsNullOrWhiteSpace(text) ? Fallback(userA, userB, language) : CleanMarkdown(text);
        }
        catch (Exception ex)
        {
            // A transient Claude outage must not block the match from being
            // created: use the fallback text and log the error.
            _logger.LogWarning(ex,
                "Interpretation via Claude failed for match {UserA}/{UserB}; using fallback text.",
                userA.Id, userB.Id);
            return Fallback(userA, userB, language);
        }
    }

    /// <summary>
    /// Builds the system prompt in English (matching ChartInterpretationService
    /// and SwipeTeaserService): with a Dutch-language instruction body, a
    /// smaller model like Haiku tends to keep answering in Dutch even when a
    /// single embedded word asks for another language. Writing the instructions
    /// themselves in English keeps the requested answer language reliable.
    /// </summary>
    private static string BuildSystemPrompt(string language) =>
        "You are a warm, vivid astrologer explaining synastry (compatibility between two " +
        "natal charts) for a dating app. Write in " + LanguageName(language) + ", 3 to 5 sentences, " +
        "addressing the couple in the second person plural ('you two'). Refer concretely to at " +
        "least two of the given placements (e.g. \"your Moon in Cancer\"). Be positive but honest: " +
        "feel free to mention a tension alongside the strengths. Answer in plain text: no Markdown, " +
        "no title, no '#' headings, no bullet points — start immediately with the first sentence of the explanation.";

    /// <summary>Maps an ISO 639-1 code to an English language name for the prompt.</summary>
    internal static string LanguageName(string language) => language switch
    {
        "en" => "English",
        "fr" => "French",
        _ => "Dutch",
    };

    private static string BuildPrompt(
        ApplicationUser userA, NatalChart chartA,
        ApplicationUser userB, NatalChart chartB,
        int score)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Compatibility score: {score}/100.");
        sb.AppendLine();
        sb.AppendLine($"Chart of {userA.DisplayName}:");
        DescribePlacements(sb, chartA);
        sb.AppendLine();
        sb.AppendLine($"Chart of {userB.DisplayName}:");
        DescribePlacements(sb, chartB);
        sb.AppendLine();
        sb.AppendLine($"Write the synastry interpretation for {userA.DisplayName} and {userB.DisplayName}.");

        return sb.ToString();
    }

    private static void DescribePlacements(StringBuilder sb, NatalChart chart)
    {
        foreach (var placement in chart.Placements.OrderBy(p => p.Body))
        {
            sb.AppendLine($"- {placement.Body} in {placement.Sign} (house {placement.House})");
        }
    }

    /// <summary>
    /// Claude doesn't always follow the "no Markdown" instruction; a single
    /// leading '#' title line is defensively stripped here so the text
    /// doesn't end up with a literal hash heading inside the &lt;p&gt;.
    /// </summary>
    private static string CleanMarkdown(string text)
    {
        var lines = text.Trim().Split('\n');
        var start = lines[0].TrimStart().StartsWith('#') ? 1 : 0;
        return string.Join('\n', lines.Skip(start)).Trim();
    }

    private static string Fallback(ApplicationUser userA, ApplicationUser userB, string language) => language switch
    {
        "en" => $"{userA.DisplayName} and {userB.DisplayName} match based on their charts (interpretation text could not be retrieved).",
        "fr" => $"{userA.DisplayName} et {userB.DisplayName} sont compatibles selon leurs thèmes (le texte d'interprétation n'a pas pu être récupéré).",
        _ => $"{userA.DisplayName} en {userB.DisplayName} matchen op basis van hun horoscopen (interpretatietekst kon niet opgehaald worden).",
    };
}
