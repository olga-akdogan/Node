using Node.Data.Models;

namespace Node.Web.Services.Interfaces;

public interface ICompatibilityService
{
    Task<(int Score, string Explanation)> CalculateCompatibilityAsync(
        AstrologyProfile currentUserAstrology,
        AstrologyProfile targetUserAstrology);
}