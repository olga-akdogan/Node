using System.Text;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Node.Data.Models;

namespace Node.Data.Services;

/// <summary>
/// Calls the Claude API for the swipe card's compatibility-test
/// blurb and first-date idea. Both parts come from a single answer,
/// separated by a fixed delimiter, so only one API call is needed per card.
/// </summary>
public class SwipeTeaserService : ISwipeTeaserService
{
    private const string Model = "claude-haiku-4-5";

    /// <summary>Delimiter between the two parts of the answer (see BuildSystemPrompt).</summary>
    private const string Delimiter = "###DATE_IDEA###";

    private readonly AnthropicClient _client;
    private readonly ILogger<SwipeTeaserService> _logger;

    public SwipeTeaserService(AnthropicClient client, ILogger<SwipeTeaserService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<(string CompatibilityTest, string DateIdea)> WriteTeaserAsync(
        ApplicationUser viewer, NatalChart viewerChart,
        ApplicationUser candidate, NatalChart candidateChart,
        int compatibilityScore,
        string language)
    {
        try
        {
            var response = await _client.Messages.Create(new MessageCreateParams
            {
                Model = Model,
                MaxTokens = 300,
                System = BuildSystemPrompt(language),
                Messages = [new() { Role = Role.User, Content = BuildPrompt(viewer, viewerChart, candidate, candidateChart, compatibilityScore) }],
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
                "Swipe card teaser via Claude failed for {ViewerId}/{CandidateId}; using fallback text.",
                viewer.Id, candidate.Id);
            return Fallback(language);
        }
    }

    private static string BuildSystemPrompt(string language) =>
        "You are a witty astrologer writing a playful \"compatibility test\" result for a dating app's " +
        "swipe card, based on two users' full natal charts. Write in " + MatchInterpretationService.LanguageName(language) +
        ", addressing the viewer in the second person singular about the candidate. " +
        "Answer in two parts, separated by exactly the line \"" + Delimiter + "\" on its own line:\n" +
        "1. A fun, quiz-verdict-style compatibility blurb: 2 to 3 short sentences, upbeat and a little cheeky, " +
        "referring concretely to at least one placement from each chart (e.g. \"her Venus in Leo\").\n" +
        "2. One concrete first-date suggestion (a single sentence) that ties back to a trait from the blurb " +
        "(e.g. an adventurous Mars sign suggests an active date).\n" +
        "Answer in plain text: no Markdown, no titles, no '#' headings, no bullet points, no emoji.";

    private static string BuildPrompt(
        ApplicationUser viewer, NatalChart viewerChart,
        ApplicationUser candidate, NatalChart candidateChart,
        int score)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Compatibility score: {score}/100.");
        sb.AppendLine();
        sb.AppendLine($"Chart of {viewer.DisplayName} (the viewer):");
        DescribePlacements(sb, viewerChart);
        sb.AppendLine();
        sb.AppendLine($"Chart of {candidate.DisplayName} (the candidate on the card):");
        DescribePlacements(sb, candidateChart);
        sb.AppendLine();
        sb.AppendLine($"Write the compatibility-test blurb and first-date idea for {viewer.DisplayName} about {candidate.DisplayName}.");

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
    /// Splits the answer on the delimiter. If it's missing (Claude doesn't
    /// always follow the instruction perfectly), the full text is used as the
    /// compatibility test and the date idea stays empty instead of crashing.
    /// </summary>
    private static (string CompatibilityTest, string DateIdea) SplitAnswer(string text)
    {
        var parts = text.Trim().Split(Delimiter, 2, StringSplitOptions.None);
        var compatibilityTest = CleanMarkdown(parts[0]);
        var dateIdea = parts.Length > 1 ? CleanMarkdown(parts[1]) : string.Empty;

        return (compatibilityTest, dateIdea);
    }

    private static string CleanMarkdown(string text)
    {
        var lines = text.Trim().Split('\n');
        var start = lines.Length > 0 && lines[0].TrimStart().StartsWith('#') ? 1 : 0;
        return string.Join('\n', lines.Skip(start)).Trim();
    }

    private static (string CompatibilityTest, string DateIdea) Fallback(string language) => language switch
    {
        "en" => ("Your compatibility test result couldn't be retrieved right now.", string.Empty),
        "fr" => ("Le résultat du test de compatibilité n'a pas pu être récupéré pour le moment.", string.Empty),
        _ => ("Je compatibiliteitstest kon nu niet opgehaald worden.", string.Empty),
    };
}
