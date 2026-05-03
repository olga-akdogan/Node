using Node.Data.Models;

namespace Node.Web.Services.Interfaces;

public interface IChatService
{
    Task<List<ChatMessage>> GetMessagesAsync(int matchId, string userId);

    Task<ChatMessage> SendMessageAsync(int matchId, string senderUserId, string message);
}