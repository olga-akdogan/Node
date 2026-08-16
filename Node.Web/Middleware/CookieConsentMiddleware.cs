using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Node.Data.Data;
using Node.Data.Models;

namespace Node.Web.Middleware;

/// <summary>
/// Custom middleware (no controller): tracks whether a visitor already made
/// a cookie choice, and processes that choice directly on the path
/// <c>POST /cookieconsent/choose</c> — including writing to
/// <see cref="CookieConsentLog"/>, so it's provable when and from where
/// someone gave or refused consent.
/// </summary>
public class CookieConsentMiddleware
{
    public const string ConsentCookieName = "node-cookie-consent";
    private const string ChoicePath = "/cookieconsent/choose";

    private readonly RequestDelegate _next;

    public CookieConsentMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Extra parameters after <see cref="HttpContext"/> are resolved by ASP.NET
    /// Core from the DI container per request — this lets the middleware use
    /// the (scoped) DbContext and the antiforgery service without the
    /// middleware class itself needing to be scoped.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db, IAntiforgery antiforgery)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.Equals(ChoicePath, StringComparison.OrdinalIgnoreCase))
        {
            await ProcessChoiceAsync(context, db, antiforgery);
            return; // Short-circuit: this path never continues to MVC.
        }

        // Flag for the layout: has this visitor already made a choice?
        // (_Layout.cshtml only shows the cookie banner when this is false.)
        context.Items["CookieConsentGiven"] = context.Request.Cookies.ContainsKey(ConsentCookieName);

        await _next(context);
    }

    private static async Task ProcessChoiceAsync(HttpContext context, ApplicationDbContext db, IAntiforgery antiforgery)
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
        var accepted = form["choice"] == "accept";

        context.Response.Cookies.Append(ConsentCookieName, accepted ? "1" : "0", new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true, // The cookie itself just remembers the consent, so it may always be set.
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
        });

        db.CookieConsentLogs.Add(new CookieConsentLog
        {
            UserId = context.User.Identity?.IsAuthenticated == true
                ? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null,
            HasAcceptedCookies = accepted,
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
        });
        await db.SaveChangesAsync();

        var returnUrl = form["returnUrl"].ToString();
        context.Response.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }
}

/// <summary>Registration extension, following the common ASP.NET Core convention for middleware.</summary>
public static class CookieConsentMiddlewareExtensions
{
    public static IApplicationBuilder UseCookieConsent(this IApplicationBuilder app) =>
        app.UseMiddleware<CookieConsentMiddleware>();
}
