namespace Node.Web.Services.Interfaces;

/// <summary>
/// Sending emails (including the required email verification at registration).
/// </summary>
public interface IEmailService
{
    /// <summary>Sends an email with HTML content to a single recipient.</summary>
    Task SendAsync(string to, string subject, string htmlBody);
}
