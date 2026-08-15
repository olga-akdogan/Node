using System.Text;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Node.Data.Models;

namespace Node.Data.Services;

/// <summary>
/// Roept de Claude API (Anthropic) op om een leesbare synastrie-interpretatie
/// te schrijven op basis van beide volledige horoscopen. De compatibiliteitsscore
/// zelf komt niet van Claude (die blijft deterministisch, zie DemoSynastrie) —
/// enkel de begeleidende tekst.
/// </summary>
public class MatchInterpretationService : IMatchInterpretationService
{
    private const string Model = "claude-haiku-4-5";

    private const string SysteemPrompt =
        "Je bent een warme, beeldende astroloog die synastrie (compatibiliteit tussen twee " +
        "geboortehoroscopen) uitlegt voor een datingapp. Schrijf in het Nederlands, 3 tot 5 zinnen, " +
        "in de tweede persoon meervoud ('jullie'). Verwijs concreet naar minstens twee van de " +
        "opgegeven plaatsingen (bv. \"jouw Maan in Kreeft\"). Wees positief maar eerlijk: benoem " +
        "gerust een spanning naast de sterke kanten. Antwoord met platte tekst: geen Markdown, geen " +
        "titel, geen '#'-kopjes, geen opsomming — start meteen met de eerste zin van de uitleg.";

    private readonly AnthropicClient _client;
    private readonly ILogger<MatchInterpretationService> _logger;

    public MatchInterpretationService(AnthropicClient client, ILogger<MatchInterpretationService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<string> SchrijfInterpretatieAsync(
        ApplicationUser gebruikerA, NatalChart chartA,
        ApplicationUser gebruikerB, NatalChart chartB,
        int compatibiliteitsScore)
    {
        try
        {
            var response = await _client.Messages.Create(new MessageCreateParams
            {
                Model = Model,
                MaxTokens = 400,
                System = SysteemPrompt,
                Messages = [new() { Role = Role.User, Content = BouwPrompt(gebruikerA, chartA, gebruikerB, chartB, compatibiliteitsScore) }],
            });

            var tekst = response.Content
                .Select(b => b.Value)
                .OfType<TextBlock>()
                .FirstOrDefault()?.Text;

            return string.IsNullOrWhiteSpace(tekst) ? Terugval(gebruikerA, gebruikerB) : OpschonenMarkdown(tekst);
        }
        catch (Exception ex)
        {
            // Een tijdelijke storing bij Claude mag het ontstaan van een match
            // niet blokkeren: terugvaltekst gebruiken en de fout loggen.
            _logger.LogWarning(ex,
                "Interpretatie via Claude mislukt voor match {UserA}/{UserB}; terugvaltekst gebruikt.",
                gebruikerA.Id, gebruikerB.Id);
            return Terugval(gebruikerA, gebruikerB);
        }
    }

    private static string BouwPrompt(
        ApplicationUser gebruikerA, NatalChart chartA,
        ApplicationUser gebruikerB, NatalChart chartB,
        int score)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Compatibiliteitsscore: {score}/100.");
        sb.AppendLine();
        sb.AppendLine($"Horoscoop van {gebruikerA.DisplayName}:");
        BeschrijfPlaatsingen(sb, chartA);
        sb.AppendLine();
        sb.AppendLine($"Horoscoop van {gebruikerB.DisplayName}:");
        BeschrijfPlaatsingen(sb, chartB);
        sb.AppendLine();
        sb.AppendLine($"Schrijf de synastrie-interpretatie voor {gebruikerA.DisplayName} en {gebruikerB.DisplayName}.");

        return sb.ToString();
    }

    private static void BeschrijfPlaatsingen(StringBuilder sb, NatalChart chart)
    {
        foreach (var plaatsing in chart.Placements.OrderBy(p => p.Body))
        {
            sb.AppendLine($"- {plaatsing.Body} in {plaatsing.Sign} (huis {plaatsing.House})");
        }
    }

    /// <summary>
    /// Claude volgt de instructie "geen Markdown" niet altijd; een enkele
    /// leidende '#'-titelregel wordt hier defensief verwijderd zodat de tekst
    /// niet met een letterlijke hekjeskop in de &lt;p&gt; belandt.
    /// </summary>
    private static string OpschonenMarkdown(string tekst)
    {
        var regels = tekst.Trim().Split('\n');
        var start = regels[0].TrimStart().StartsWith('#') ? 1 : 0;
        return string.Join('\n', regels.Skip(start)).Trim();
    }

    private static string Terugval(ApplicationUser gebruikerA, ApplicationUser gebruikerB) =>
        $"{gebruikerA.DisplayName} en {gebruikerB.DisplayName} matchen op basis van hun horoscopen " +
        "(interpretatietekst kon niet opgehaald worden).";
}
