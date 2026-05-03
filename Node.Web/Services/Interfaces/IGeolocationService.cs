namespace Node.Web.Services.Interfaces;

public interface IGeolocationService
{
    Task<(decimal Latitude, decimal Longitude)?> GetCoordinatesAsync(string location);
}