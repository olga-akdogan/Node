using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Node.Data.Data;
using Node.Data.Models;

namespace Node.Web.Middleware;

/// <summary>
/// Eigen middleware (geen controller): houdt bij of een bezoeker al een
/// cookiekeuze maakte, en verwerkt die keuze rechtstreeks op het pad
/// <c>POST /cookieconsent/kies</c> — inclusief het wegschrijven naar
/// <see cref="CookieConsentLog"/>, zodat aantoonbaar is wanneer en vanwaar
/// iemand toestemming gaf of weigerde.
/// </summary>
public class CookieConsentMiddleware
{
    public const string ConsentCookieName = "node-cookie-consent";
    private const string KeuzePad = "/cookieconsent/kies";

    private readonly RequestDelegate _next;

    public CookieConsentMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Extra parameters na <see cref="HttpContext"/> worden door ASP.NET Core
    /// per verzoek uit de DI-container gehaald — zo kan deze middleware de
    /// (scoped) DbContext en de antiforgery-dienst gebruiken zonder dat de
    /// middlewareklasse zelf scoped hoeft te zijn.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db, IAntiforgery antiforgery)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.Equals(KeuzePad, StringComparison.OrdinalIgnoreCase))
        {
            await VerwerkKeuzeAsync(context, db, antiforgery);
            return; // Kort-sluiten: dit pad gaat nooit door naar MVC.
        }

        // Vlag voor de layout: heeft deze bezoeker al een keuze gemaakt?
        // (_Layout.cshtml toont de cookiebanner enkel wanneer dit false is.)
        context.Items["CookieConsentGegeven"] = context.Request.Cookies.ContainsKey(ConsentCookieName);

        await _next(context);
    }

    private static async Task VerwerkKeuzeAsync(HttpContext context, ApplicationDbContext db, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var form = await context.Request.ReadFormAsync();
        var geaccepteerd = form["keuze"] == "accepteren";

        context.Response.Cookies.Append(ConsentCookieName, geaccepteerd ? "1" : "0", new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true, // De cookie zelf onthoudt net de toestemming, dus mag altijd gezet worden.
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
        });

        db.CookieConsentLogs.Add(new CookieConsentLog
        {
            UserId = context.User.Identity?.IsAuthenticated == true
                ? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null,
            HasAcceptedCookies = geaccepteerd,
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
        });
        await db.SaveChangesAsync();

        var terugUrl = form["terugUrl"].ToString();
        context.Response.Redirect(string.IsNullOrEmpty(terugUrl) ? "/" : terugUrl);
    }
}

/// <summary>Registratie-extensie, naar de gangbare ASP.NET Core-conventie voor middleware.</summary>
public static class CookieConsentMiddlewareExtensions
{
    public static IApplicationBuilder UseCookieConsent(this IApplicationBuilder app) =>
        app.UseMiddleware<CookieConsentMiddleware>();
}
