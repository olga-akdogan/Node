using Node.Data.Models;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

public class AstrologyService : IAstrologyService
{
    private readonly ILogger<AstrologyService> _logger;

    public AstrologyService(ILogger<AstrologyService> logger)
    {
        _logger = logger;
    }

    public async Task<AstrologyProfile?> CreateAstrologyProfileAsync(MemberProfile profile)
    {
        try
        {
            await Task.Delay(100);

            return new AstrologyProfile
            {
                UserId = profile.UserId,
                SunSign = "Pisces",
                MoonSign = "Cancer",
                RisingSign = "Libra",
                PlanetaryDataJson = "{}",
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create astrology profile for user {UserId}", profile.UserId);
            return null;
        }
    }
}