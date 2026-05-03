using Node.Data.Models;

namespace Node.Web.Services.Interfaces;

public interface ISwipeService
{
    Task<Swipe> CreateSwipeAsync(string swiperUserId, string targetUserId, bool isLike);
}