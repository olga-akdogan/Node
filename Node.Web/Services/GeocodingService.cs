using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

/// <summary>
/// Zoekt coördinaten op via Nominatim, de gratis geocodingdienst van
/// OpenStreetMap. Wordt gebruikt bij registratie zodat de geboorteplaats die
/// de gebruiker intypt, coördinaten krijgt voor de horoscoopberekening.
/// </summary>
public class GeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeocodingService> _logger;

    public GeocodingService(HttpClient httpClient, ILogger<GeocodingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Null means "place not found" both when Nominatim genuinely has no
    /// match and when the call itself fails (offline, timeout, unexpected
    /// response): callers already show a friendly "place not found, try
    /// again" message for null, so a network hiccup degrades to that instead
    /// of crashing the registration/profile-edit flow with an unhandled exception.
    /// </summary>
    public async Task<(decimal Latitude, decimal Longitude)?> ZoekCoordinatenAsync(string plaats)
    {
        var url = $"search?q={Uri.EscapeDataString(plaats)}&format=json&limit=1";

        List<NominatimResultaat>? resultaten;
        try
        {
            resultaten = await _httpClient.GetFromJsonAsync<List<NominatimResultaat>>(url);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Geocoding via Nominatim mislukt voor '{Plaats}'.", plaats);
            return null;
        }

        var eerste = resultaten?.FirstOrDefault();
        if (eerste is null)
        {
            return null; // Plaats niet gevonden.
        }

        return (
            decimal.Parse(eerste.Lat, CultureInfo.InvariantCulture),
            decimal.Parse(eerste.Lon, CultureInfo.InvariantCulture));
    }

    /// <summary>Enkel de velden van de Nominatim-respons die we effectief gebruiken.</summary>
    private class NominatimResultaat
    {
        [JsonPropertyName("lat")]
        public string Lat { get; set; } = string.Empty;

        [JsonPropertyName("lon")]
        public string Lon { get; set; } = string.Empty;
    }
}
