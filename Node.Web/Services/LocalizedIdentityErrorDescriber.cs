using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Node.Web.Resources;

namespace Node.Web.Services;

/// <summary>
/// Translates the built-in Identity error messages
/// </summary>
public class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LocalizedIdentityErrorDescriber(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public override IdentityError DefaultError() => Error("Identity_DefaultError");

    public override IdentityError ConcurrencyFailure() => Error("Identity_ConcurrencyFailure");

    public override IdentityError PasswordMismatch() => Error("Identity_PasswordMismatch");

    public override IdentityError InvalidToken() => Error("Identity_InvalidToken");

    public override IdentityError RecoveryCodeRedemptionFailed() => Error("Identity_RecoveryCodeRedemptionFailed");

    public override IdentityError LoginAlreadyAssociated() => Error("Identity_LoginAlreadyAssociated");

    public override IdentityError InvalidUserName(string? userName) => Error("Identity_InvalidUserName", userName ?? string.Empty);

    public override IdentityError InvalidEmail(string? email) => Error("Identity_InvalidEmail", email ?? string.Empty);

    public override IdentityError DuplicateUserName(string userName) => Error("Identity_DuplicateUserName", userName);

    public override IdentityError DuplicateEmail(string email) => Error("Identity_DuplicateEmail", email);

    public override IdentityError InvalidRoleName(string? role) => Error("Identity_InvalidRoleName", role ?? string.Empty);

    public override IdentityError DuplicateRoleName(string role) => Error("Identity_DuplicateRoleName", role);

    public override IdentityError UserAlreadyHasPassword() => Error("Identity_UserAlreadyHasPassword");

    public override IdentityError UserLockoutNotEnabled() => Error("Identity_UserLockoutNotEnabled");

    public override IdentityError UserAlreadyInRole(string role) => Error("Identity_UserAlreadyInRole", role);

    public override IdentityError UserNotInRole(string role) => Error("Identity_UserNotInRole", role);

    public override IdentityError PasswordTooShort(int length) => Error("Identity_PasswordTooShort", length);

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Error("Identity_PasswordRequiresUniqueChars", uniqueChars);

    public override IdentityError PasswordRequiresNonAlphanumeric() => Error("Identity_PasswordRequiresNonAlphanumeric");

    public override IdentityError PasswordRequiresDigit() => Error("Identity_PasswordRequiresDigit");

    public override IdentityError PasswordRequiresLower() => Error("Identity_PasswordRequiresLower");

    public override IdentityError PasswordRequiresUpper() => Error("Identity_PasswordRequiresUpper");

    private IdentityError Error(string key, params object[] arguments) => new()
    {
        Code = key,
        Description = _localizer[key, arguments],
    };
}
