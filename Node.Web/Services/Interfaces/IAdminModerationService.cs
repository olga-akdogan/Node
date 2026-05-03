using Node.Data.Models;

namespace Node.Web.Services.Interfaces;

public interface IAdminModerationService
{
    Task<List<MemberProfile>> GetAllMembersAsync();

    Task BlockMemberAsync(string memberUserId, string adminUserId, string reason);

    Task UnblockMemberAsync(string memberUserId);

    Task<List<Report>> GetReportsAsync();
}