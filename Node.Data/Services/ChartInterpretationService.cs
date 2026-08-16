using System.Text;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Node.Data.Models;

namespace Node.Data.Services;

/// <summary>
/// Calls the Claude API for the two "My chart" interpretation
/// texts. Both texts come from a single answer, separated by a fixed
/// delimiter, so only one API call is needed per (re)generation.
/// </summary>
public class ChartInterpretationService : IChartInterpretationService
{
    private const string Model = "claude-haiku-4-5";

    /// <summary>Delimiter between the two parts of the answer (see BuildSystemPrompt).</summary>
    private const string Delimiter = "###PARTNER###";

    private readonly AnthropicClient _client;
    private readonly ILogger<ChartInterpretationService> _logger;

    public ChartInterpretationService(AnthropicClient client, ILogger<ChartInterpretationService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<(string Interpretation, string PartnerPreferenceText)> WriteInterpretationAsync(
        ApplicationUser user, NatalChart chart, string language)
    {
        try
        {
            var response = await _client.Messages.Create(new MessageCreateParams
            {
                Model = Model,
                MaxTokens = 700,
                System = BuildSystemPrompt(language),
                Messages = [new() { Role = Role.User, Content = BuildPrompt(user, chart) }],
            });

            var text = response.Content
                .Select(b => b.Value)
                .OfType<TextBlock>()
                .FirstOrDefault()?.Text;

            return string.IsNullOrWhiteSpace(text) ? Fallback(language) : SplitAnswer(text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Chart interpretation via Claude failed for user {UserId}; using fallback text.",
                user.Id);
            return Fallback(language);
        }
    }

    private static string BuildSystemPrompt(string language) =>
        "You are a warm, vivid astrologer who interprets one user's full natal chart for a dating app. " +
        "Write in " + MatchInterpretationService.LanguageName(language) + ", in the second person singular. " +
        "Refer concretely to at least three of the given placements. " +
        "Answer in two parts, separated by exactly the line \"" + Delimiter + "\" on its own line:\n" +
        "1. An interpretation of the chart as a whole (personality, motivations): 4 to 6 sentences.\n" +
        "2. What this person looks for in a partner and a relationship, derived mainly from Venus, Mars " +
        "and the 7th house: 3 to 5 sentences.\n" +
        "Answer in plain text: no Markdown, no titles, no '#' headings, no bullet points.";

    private static string BuildPrompt(ApplicationUser user, NatalChart chart)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Chart of {user.DisplayName}:");
        foreach (var placement in chart.Placements.OrderBy(p => p.Body))
        {
            sb.AppendLine($"- {placement.Body} in {placement.Sign} (house {placement.House})");
        }

        sb.AppendLine();
        sb.AppendLine($"Write the chart interpretation and the partner/relationship text for {user.DisplayName}.");

        return sb.ToString();
    }

    /// <summary>
    /// Splits the answer on the delimiter. If it's missing (Claude doesn't
    /// always follow the instruction perfectly), the full text is used as the
    /// interpretation and the partner text stays empty instead of crashing.
    /// </summary>
    private static (string Interpretation, string PartnerPreferenceText) SplitAnswer(string text)
    {
        var parts = text.Trim().Split(Delimiter, 2, StringSplitOptions.None);
        var interpretation = CleanMarkdown(parts[0]);
        var partnerPreferenceText = parts.Length > 1 ? CleanMarkdown(parts[1]) : string.Empty;

        return (interpretation, partnerPreferenceText);
    }

    private static string CleanMarkdown(string text)
    {
        var lines = text.Trim().Split('\n');
        var start = lines.Length > 0 && lines[0].TrimStart().StartsWith('#') ? 1 : 0;
        return string.Join('\n', lines.Skip(start)).Trim();
    }

    private static (string Interpretation, string PartnerPreferenceText) Fallback(string language) => language switch
    {
        "en" => ("Your chart interpretation could not be retrieved right now.", "Your partner preferences could not be retrieved right now."),
        "fr" => ("L'interprétation de ton thème n'a pas pu être récupérée pour le moment.", "Tes préférences de partenaire n'ont pas pu être récupérées pour le moment."),
        _ => ("Je horoscoopinterpretatie kon nu niet opgehaald worden.", "Je partnervoorkeuren konden nu niet opgehaald worden."),
    };
}
