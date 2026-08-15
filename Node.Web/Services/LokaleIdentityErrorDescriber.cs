using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Node.Web.Resources;

namespace Node.Web.Services;

/// <summary>
/// Vertaalt de ingebouwde Identity-foutmeldingen (bv. wachtwoordregels,
/// dubbel e-mailadres) via dezelfde gedeelde resource-bestanden als de rest
/// van de site, zodat ook deze berichten meertalig zijn.
/// </summary>
public class LokaleIdentityErrorDescriber : IdentityErrorDescriber
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LokaleIdentityErrorDescriber(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public override IdentityError DefaultError() => Fout("Identity_OnbekendeFout");

    public override IdentityError ConcurrencyFailure() => Fout("Identity_Concurrency");

    public override IdentityError PasswordMismatch() => Fout("Identity_WachtwoordFout");

    public override IdentityError InvalidToken() => Fout("Identity_OngeldigToken");

    public override IdentityError RecoveryCodeRedemptionFailed() => Fout("Identity_HerstelcodeFout");

    public override IdentityError LoginAlreadyAssociated() => Fout("Identity_LoginAlBestaat");

    public override IdentityError InvalidUserName(string? userName) => Fout("Identity_OngeldigeGebruikersnaam", userName ?? string.Empty);

    public override IdentityError InvalidEmail(string? email) => Fout("Identity_OngeldigEmail", email ?? string.Empty);

    public override IdentityError DuplicateUserName(string userName) => Fout("Identity_GebruikersnaamBestaat", userName);

    public override IdentityError DuplicateEmail(string email) => Fout("Identity_EmailBestaat", email);

    public override IdentityError InvalidRoleName(string? role) => Fout("Identity_OngeldigeRolnaam", role ?? string.Empty);

    public override IdentityError DuplicateRoleName(string role) => Fout("Identity_RolnaamBestaat", role);

    public override IdentityError UserAlreadyHasPassword() => Fout("Identity_HeeftAlWachtwoord");

    public override IdentityError UserLockoutNotEnabled() => Fout("Identity_LockoutNietIngeschakeld");

    public override IdentityError UserAlreadyInRole(string role) => Fout("Identity_HeeftAlRol", role);

    public override IdentityError UserNotInRole(string role) => Fout("Identity_HeeftRolNiet", role);

    public override IdentityError PasswordTooShort(int length) => Fout("Identity_WachtwoordTeKort", length);

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Fout("Identity_WachtwoordUniekeTekens", uniqueChars);

    public override IdentityError PasswordRequiresNonAlphanumeric() => Fout("Identity_WachtwoordNietAlfanumeriek");

    public override IdentityError PasswordRequiresDigit() => Fout("Identity_WachtwoordCijfer");

    public override IdentityError PasswordRequiresLower() => Fout("Identity_WachtwoordKleineLetter");

    public override IdentityError PasswordRequiresUpper() => Fout("Identity_WachtwoordHoofdletter");

    private IdentityError Fout(string sleutel, params object[] argumenten) => new()
    {
        Code = sleutel,
        Description = _localizer[sleutel, argumenten],
    };
}
