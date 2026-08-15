namespace Node.Web.Services.Interfaces;

/// <summary>Zoekt de coördinaten van een vrij ingetypte plaatsnaam op.</summary>
public interface IGeocodingService
{
    /// <summary>
    /// Geeft de (breedtegraad, lengtegraad) van de opgegeven plaats, of null
    /// wanneer de plaats niet gevonden werd.
    /// </summary>
    Task<(decimal Latitude, decimal Longitude)?> ZoekCoordinatenAsync(string plaats);
}
