using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Node.Data.Models;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public JwtTokenService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<(string Token, DateTime ExpiresAtUtc)> CreateTokenAsync(ApplicationUser gebruiker)
    {
        var sleutel = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Configuratie 'Jwt:Key' ontbreekt.");
        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Configuratie 'Jwt:Issuer' ontbreekt.");
        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Configuratie 'Jwt:Audience' ontbreekt.");

        var rollen = await _userManager.GetRolesAsync(gebruiker);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, gebruiker.Id),
            new(ClaimTypes.Name, gebruiker.UserName ?? gebruiker.Email ?? gebruiker.Id),
            new(ClaimTypes.Email, gebruiker.Email ?? string.Empty),
        };
        claims.AddRange(rollen.Select(rol => new Claim(ClaimTypes.Role, rol)));

        var vervalt = DateTime.UtcNow.AddHours(12);
        var ondertekeningssleutel = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sleutel));
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: vervalt,
            signingCredentials: new SigningCredentials(ondertekeningssleutel, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), vervalt);
    }
}
