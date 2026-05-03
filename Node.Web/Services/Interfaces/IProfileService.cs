using Node.Data.Models;

namespace Node.Web.Services.Interfaces;

public interface IProfileService
{
    Task<MemberProfile?> GetProfileByUserIdAsync(string userId);

    Task<List<MemberProfile>> GetSwipeCandidatesAsync(string currentUserId);

    Task<MemberProfile> CreateOrUpdateProfileAsync(MemberProfile profile);
}