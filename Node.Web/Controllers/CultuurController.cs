using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Node.Web.Controllers;

/// <summary>
/// Taalkiezer: zet het cultuurcookie dat <c>UseRequestLocalization</c> leest,
/// en stuurt de gebruiker terug naar de pagina waar die vandaan kwam. Werkt
/// voor zowel ingelogde als anonieme bezoekers (dus ook op de Identity-schermen).
/// </summary>
public class CultuurController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Wijzig(string cultuur, string terugUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(cultuur)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

        return LocalRedirect(string.IsNullOrEmpty(terugUrl) ? "/" : terugUrl);
    }
}
