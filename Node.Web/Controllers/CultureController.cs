using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Node.Web.Controllers;

/// <summary>
/// Language picker: sets the culture cookie that <c>UseRequestLocalization</c>
/// reads, and sends the user back to the page they came from. Works for both
/// logged-in and anonymous visitors (so also on the Identity screens).
/// </summary>
public class CultureController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Change(string culture, string returnUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }
}
