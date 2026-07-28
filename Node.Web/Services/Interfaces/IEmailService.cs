namespace Node.Web.Services.Interfaces;

/// <summary>
/// Versturen van e-mails (o.a. de verplichte e-mailverificatie bij registratie).
/// </summary>
public interface IEmailService
{
    /// <summary>Verstuurt een e-mail met HTML-inhoud naar één ontvanger.</summary>
    Task SendAsync(string to, string subject, string htmlBody);
}
