using Node.Data.Models;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

public class CompatibilityService : ICompatibilityService
{
    private readonly ILogger<CompatibilityService> _logger;

    public CompatibilityService(ILogger<CompatibilityService> logger)
    {
        _logger = logger;
    }

    public async Task<(int Score, string Explanation)> CalculateCompatibilityAsync(
        AstrologyProfile currentUserAstrology,
        AstrologyProfile targetUserAstrology)
    {
        try
        {
            await Task.Delay(100);

            int score = 78;

            string explanation =
                $"Your {currentUserAstrology.SunSign} energy connects warmly with their {targetUserAstrology.SunSign} energy. " +
                "This compatibility score is only for fun and should not be seen as a fixed prediction.";

            return (score, explanation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate compatibility");

            return (0, "Compatibility could not be calculated right now.");
        }
    }
}