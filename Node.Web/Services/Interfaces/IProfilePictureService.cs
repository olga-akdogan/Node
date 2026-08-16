namespace Node.Web.Services.Interfaces;

/// <summary>
/// Validates and stores an uploaded profile picture. Shared between the web
/// profile form (ManageController) and the API profile endpoint, so both
/// enforce the same allowed types and size limit.
/// </summary>
public interface IProfilePictureService
{
    /// <summary>Content-type -> file extension for the types allowed as a profile picture.</summary>
    IReadOnlyDictionary<string, string> AllowedTypes { get; }

    long MaxSizeBytes { get; }

    /// <summary>Saves the file to wwwroot/uploads/profiles, replacing any previous photo for this user, and returns its public URL.</summary>
    Task<string> SaveAsync(string userId, IFormFile photo);
}
