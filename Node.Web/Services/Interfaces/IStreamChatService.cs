namespace Node.Web.Services.Interfaces;

/// <summary>
/// Koppeling met GetStream Chat: de externe dienst die de chatgesprekken van
/// matches in real time afhandelt (Node.Data bewaart zelf geen berichten meer).
/// [AI-gegenereerd: Claude (Sonnet 5), prompt "add GetStream Chat to my .NET
/// app, replace the custom chat with it" — aangepast en van Nederlandse
/// documentatie voorzien voor dit examenproject.]
/// </summary>
public interface IStreamChatService
{
    /// <summary>GetStream API-sleutel: publiek, de browser heeft ze nodig om te verbinden.</summary>
    string ApiKey { get; }

    /// <summary>
    /// Maakt een kort geldig GetStream-gebruikerstoken waarmee de browser van
    /// de ingelogde gebruiker rechtstreeks (los van onze server) met GetStream
    /// mag verbinden.
    /// </summary>
    string MaakGebruikersToken(string userId);

    /// <summary>
    /// Zorgt dat de gebruiker als GetStream-gebruiker bestaat (aanmaken of
    /// bijwerken van naam/foto). Nodig vóór die gebruiker aan een kanaal kan
    /// deelnemen.
    /// </summary>
    Task ZorgVoorGebruikerAsync(Node.Data.Models.ApplicationUser gebruiker);

    /// <summary>
    /// Haalt voor elke actieve match van de gebruiker de laatste chatstatus op
    /// bij GetStream (laatste bericht + aantal ongelezen), opgezocht via de
    /// andere-gebruiker-id. Matches zonder gesprek staan niet in het resultaat.
    /// </summary>
    Task<IReadOnlyDictionary<string, StreamKanaalStatus>> GetKanaalStatussenAsync(string userId);
}

/// <summary>Chatstatus van één GetStream-kanaal, gezien vanuit één gebruiker.</summary>
public record StreamKanaalStatus(string? LaatsteBerichtTekst, DateTime? LaatsteBerichtOp, int OngelezenAantal);
