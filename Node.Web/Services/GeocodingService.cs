using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

/// <summary>
/// Looks up coordinates via Nominatim, OpenStreetMap's free geocoding
/// service. Used during registration so the birth place the user types in
/// gets coordinates for the natal chart calculation.
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
    public async Task<(decimal Latitude, decimal Longitude)?> FindCoordinatesAsync(string place)
    {
        var url = $"search?q={Uri.EscapeDataString(place)}&format=json&limit=1";

        List<NominatimResult>? results;
        try
        {
            results = await _httpClient.GetFromJsonAsync<List<NominatimResult>>(url);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Geocoding via Nominatim failed for '{Place}'.", place);
            return null;
        }

        var first = results?.FirstOrDefault();
        if (first is null)
        {
            return null; // Place not found.
        }

        return (
            decimal.Parse(first.Lat, CultureInfo.InvariantCulture),
            decimal.Parse(first.Lon, CultureInfo.InvariantCulture));
    }

    /// <summary>Only the fields of the Nominatim response we actually use.</summary>
    private class NominatimResult
    {
        [JsonPropertyName("lat")]
        public string Lat { get; set; } = string.Empty;

        [JsonPropertyName("lon")]
        public string Lon { get; set; } = string.Empty;
    }
}
