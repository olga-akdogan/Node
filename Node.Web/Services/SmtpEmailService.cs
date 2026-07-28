using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

/// <summary>
/// E-mailservice via SMTP (MailKit). De toegangsgegevens staan NIET in de repo
/// maar in user-secrets of
/// omgevingsvariabelen: Email:SmtpHost, Email:SmtpPort, Email:SmtpUser,
/// Email:SmtpPassword, Email:FromAddress, Email:FromName.
///
/// Zonder geconfigureerde SMTP-server (lokale ontwikkeling) wordt de e-mail
/// niet verstuurd maar volledig gelogd, zodat de bevestigingslink uit de log
/// geknipt kan worden.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var host = _configuration["Email:SmtpHost"];

        if (string.IsNullOrWhiteSpace(host))
        {
            // Ontwikkelmodus: geen SMTP geconfigureerd, e-mail alleen loggen.
            _logger.LogWarning(
                "SMTP niet geconfigureerd; e-mail wordt alleen gelogd. Aan: {To}, onderwerp: {Subject}, inhoud: {Body}",
                to, subject, htmlBody);
            return;
        }

        var poort = int.TryParse(_configuration["Email:SmtpPort"], out var p) ? p : 587;
        var afzenderAdres = _configuration["Email:FromAddress"] ?? "noreply@node.be";
        var afzenderNaam = _configuration["Email:FromName"] ?? "Node";

        var bericht = new MimeMessage();
        bericht.From.Add(new MailboxAddress(afzenderNaam, afzenderAdres));
        bericht.To.Add(MailboxAddress.Parse(to));
        bericht.Subject = subject;
        bericht.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, poort, SecureSocketOptions.StartTls);

        // Alleen aanmelden wanneer er inloggegevens geconfigureerd zijn
        // (sommige relay-servers vereisen geen authenticatie).
        var smtpGebruiker = _configuration["Email:SmtpUser"];
        var smtpWachtwoord = _configuration["Email:SmtpPassword"];
        if (!string.IsNullOrWhiteSpace(smtpGebruiker) && !string.IsNullOrWhiteSpace(smtpWachtwoord))
        {
            await client.AuthenticateAsync(smtpGebruiker, smtpWachtwoord);
        }

        await client.SendAsync(bericht);
        await client.DisconnectAsync(quit: true);

        _logger.LogInformation("Verificatie-e-mail verstuurd naar {To}.", to);
    }
}
