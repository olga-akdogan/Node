using System.Globalization;
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

    public GeocodingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(decimal Latitude, decimal Longitude)?> ZoekCoordinatenAsync(string plaats)
    {
        var url = $"search?q={Uri.EscapeDataString(plaats)}&format=json&limit=1";
        var resultaten = await _httpClient.GetFromJsonAsync<List<NominatimResultaat>>(url);

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
