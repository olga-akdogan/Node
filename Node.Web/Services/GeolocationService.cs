using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

public class GeolocationService : IGeolocationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeolocationService> _logger;

    public GeolocationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeolocationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(decimal Latitude, decimal Longitude)?> GetCoordinatesAsync(string location)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return null;
            }

            // Temporary placeholder.
            // Later this will call Google Geolocation or Geocoding API.
            await Task.Delay(100);

            return (51.2194m, 4.4025m); // Antwerp placeholder
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get coordinates for location {Location}", location);
            return null;
        }
    }
}