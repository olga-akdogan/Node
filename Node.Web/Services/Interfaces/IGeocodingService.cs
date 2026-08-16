namespace Node.Web.Services.Interfaces;

/// <summary>Looks up the coordinates of a freely typed place name.</summary>
public interface IGeocodingService
{
    /// <summary>
    /// Returns the (latitude, longitude) of the given place, or null when
    /// the place wasn't found.
    /// </summary>
    Task<(decimal Latitude, decimal Longitude)?> FindCoordinatesAsync(string place);
}
