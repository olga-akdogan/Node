using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

public class ProfilePictureService : IProfilePictureService
{
    public IReadOnlyDictionary<string, string> AllowedTypes { get; } = new Dictionary<string, string>
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    public long MaxSizeBytes { get; } = 5 * 1024 * 1024; // 5 MB

    private readonly IWebHostEnvironment _environment;

    public ProfilePictureService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveAsync(string userId, IFormFile photo)
    {
        var extension = AllowedTypes[photo.ContentType];
        var folderPath = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
        Directory.CreateDirectory(folderPath);

        // The user id as filename means a new upload simply replaces the old photo.
        var filePath = Path.Combine(folderPath, $"{userId}{extension}");
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await photo.CopyToAsync(stream);
        }

        return $"/uploads/profiles/{userId}{extension}";
    }
}
