using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

public class ProfilePictureService : IProfilePictureService
{
    public IReadOnlyDictionary<string, string> ToegestaneTypes { get; } = new Dictionary<string, string>
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    public long MaxGrootteBytes { get; } = 5 * 1024 * 1024; // 5 MB

    private readonly IWebHostEnvironment _omgeving;

    public ProfilePictureService(IWebHostEnvironment omgeving)
    {
        _omgeving = omgeving;
    }

    public async Task<string> BewaarAsync(string userId, IFormFile foto)
    {
        var extensie = ToegestaneTypes[foto.ContentType];
        var mapPad = Path.Combine(_omgeving.WebRootPath, "uploads", "profiles");
        Directory.CreateDirectory(mapPad);

        // The user id as filename means a new upload simply replaces the old photo.
        var bestandsPad = Path.Combine(mapPad, $"{userId}{extensie}");
        await using (var stream = new FileStream(bestandsPad, FileMode.Create))
        {
            await foto.CopyToAsync(stream);
        }

        return $"/uploads/profiles/{userId}{extensie}";
    }
}
