using GetStream;
using GetStream.Models;
using Node.Data.Models;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

/// <summary>
/// Implementation of <see cref="IStreamChatService"/> on top of the official
/// GetStream .NET SDK (package "getstream-net").
/// </summary>
public class StreamChatService : IStreamChatService
{
    /// <summary>GetStream channel type for 1-on-1 chat (built-in default type).</summary>
    private const string ChannelType = "messaging";

    private readonly StreamClient _client;
    private readonly ChatClient _chat;
    private readonly ILogger<StreamChatService> _logger;

    public StreamChatService(StreamClient client, ChatClient chat, IConfiguration configuration, ILogger<StreamChatService> logger)
    {
        _client = client;
        _chat = chat;
        _logger = logger;

        // Already validated at startup (Program.cs); just re-read it here.
        ApiKey = configuration["Stream:ApiKey"]!;
    }

    public string ApiKey { get; }

    public string CreateUserToken(string userId) => _client.CreateUserToken(userId);

    public async Task EnsureUserExistsAsync(ApplicationUser user)
    {
        await _client.UpdateUsersAsync(new UpdateUsersRequest
        {
            Users = new Dictionary<string, UserRequest>
            {
                [user.Id] = new UserRequest
                {
                    ID = user.Id,
                    Name = user.DisplayName,
                    Image = user.ProfilePictureUrl,
                },
            },
        });

        _logger.LogInformation("GetStream user updated for {UserId}.", user.Id);
    }

    public async Task<IReadOnlyDictionary<string, StreamChannelStatus>> GetChannelStatusesAsync(string userId)
    {
        var response = await _chat.QueryChannelsAsync(new QueryChannelsRequest
        {
            UserID = userId,
            FilterConditions = new Dictionary<string, object>
            {
                ["type"] = ChannelType,
                ["members"] = new Dictionary<string, object> { ["$in"] = new[] { userId } },
            },
            // Only the last message is needed for the match overview.
            MessageLimit = 1,
        });

        var statuses = new Dictionary<string, StreamChannelStatus>();

        // response.Data is always populated when the call doesn't throw (SDK guarantee on success).
        foreach (var channel in response.Data!.Channels)
        {
            // The "other" user of this 1-on-1 channel, to link back to our Match.
            var otherUser = channel.Members
                .Select(member => member.User)
                .Where(memberInChannel => memberInChannel is not null)
                .FirstOrDefault(memberInChannel => memberInChannel!.ID != userId);

            if (otherUser is null)
            {
                continue;
            }

            var lastMessage = channel.Messages.LastOrDefault();
            var unreadCount = channel.Read
                .FirstOrDefault(readState => readState.User.ID == userId)
                ?.UnreadMessages ?? 0;

            statuses[otherUser.ID] = new StreamChannelStatus(
                lastMessage?.Text,
                lastMessage?.CreatedAt,
                unreadCount);
        }

        return statuses;
    }
}
