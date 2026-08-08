using GetStream;
using GetStream.Models;
using Node.Data.Models;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

/// <summary>
/// Implementatie van <see cref="IStreamChatService"/> bovenop de officiële
/// GetStream .NET-SDK (pakket "getstream-net").
/// [AI-gegenereerd: Claude (Sonnet 5), prompt "add GetStream Chat to my .NET
/// app, replace the custom chat with it" — aangepast en van Nederlandse
/// documentatie voorzien voor dit examenproject. De exacte klasse- en
/// methodenamen zijn via reflectie op het geïnstalleerde pakket geverifieerd.]
/// </summary>
public class StreamChatService : IStreamChatService
{
    /// <summary>GetStream-kanaaltype voor 1-op-1 chat (ingebouwd standaardtype).</summary>
    private const string KanaalType = "messaging";

    private readonly StreamClient _client;
    private readonly ChatClient _chat;
    private readonly ILogger<StreamChatService> _logger;

    public StreamChatService(StreamClient client, ChatClient chat, IConfiguration configuration, ILogger<StreamChatService> logger)
    {
        _client = client;
        _chat = chat;
        _logger = logger;

        // Al gevalideerd bij het opstarten (Program.cs); hier enkel opnieuw uitlezen.
        ApiKey = configuration["Stream:ApiKey"]!;
    }

    public string ApiKey { get; }

    public string MaakGebruikersToken(string userId) => _client.CreateUserToken(userId);

    public async Task ZorgVoorGebruikerAsync(ApplicationUser gebruiker)
    {
        await _client.UpdateUsersAsync(new UpdateUsersRequest
        {
            Users = new Dictionary<string, UserRequest>
            {
                [gebruiker.Id] = new UserRequest
                {
                    ID = gebruiker.Id,
                    Name = gebruiker.DisplayName,
                    Image = gebruiker.ProfilePictureUrl,
                },
            },
        });

        _logger.LogInformation("GetStream-gebruiker bijgewerkt voor {UserId}.", gebruiker.Id);
    }

    public async Task<IReadOnlyDictionary<string, StreamKanaalStatus>> GetKanaalStatussenAsync(string userId)
    {
        var response = await _chat.QueryChannelsAsync(new QueryChannelsRequest
        {
            UserID = userId,
            FilterConditions = new Dictionary<string, object>
            {
                ["type"] = KanaalType,
                ["members"] = new Dictionary<string, object> { ["$in"] = new[] { userId } },
            },
            // Enkel het laatste bericht nodig voor het matchoverzicht.
            MessageLimit = 1,
        });

        var statussen = new Dictionary<string, StreamKanaalStatus>();

        // response.Data is altijd gevuld wanneer de aanroep niet gooit (SDK-garantie bij succes).
        foreach (var kanaal in response.Data!.Channels)
        {
            // De "andere" gebruiker van dit 1-op-1-kanaal, om te koppelen aan onze Match.
            var andereGebruiker = kanaal.Members
                .Select(lid => lid.User)
                .Where(gebruikerInKanaal => gebruikerInKanaal is not null)
                .FirstOrDefault(gebruikerInKanaal => gebruikerInKanaal!.ID != userId);

            if (andereGebruiker is null)
            {
                continue;
            }

            var laatsteBericht = kanaal.Messages.LastOrDefault();
            var ongelezenAantal = kanaal.Read
                .FirstOrDefault(leesstatus => leesstatus.User.ID == userId)
                ?.UnreadMessages ?? 0;

            statussen[andereGebruiker.ID] = new StreamKanaalStatus(
                laatsteBericht?.Text,
                laatsteBericht?.CreatedAt,
                ongelezenAantal);
        }

        return statussen;
    }
}
