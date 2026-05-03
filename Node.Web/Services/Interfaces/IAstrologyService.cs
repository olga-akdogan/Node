using Node.Data.Models;

namespace Node.Web.Services.Interfaces;

public interface IAstrologyService
{
    Task<AstrologyProfile?> CreateAstrologyProfileAsync(MemberProfile profile);
}