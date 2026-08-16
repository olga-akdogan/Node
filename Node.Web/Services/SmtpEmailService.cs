using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Node.Web.Services.Interfaces;

namespace Node.Web.Services;

/// <summary>
/// Email service via SMTP (MailKit). Credentials are NOT in the repo but in
/// user-secrets or environment variables: Email:SmtpHost, Email:SmtpPort,
/// Email:SmtpUser, Email:SmtpPassword, Email:FromAddress, Email:FromName.
///
/// Without a configured SMTP server (local development) the email isn't
/// sent but fully logged instead, so the confirmation link can be copied
/// from the log.
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
            // Development mode: no SMTP configured, only log the email.
            _logger.LogWarning(
                "SMTP not configured; email is only logged. To: {To}, subject: {Subject}, body: {Body}",
                to, subject, htmlBody);
            return;
        }

        var port = int.TryParse(_configuration["Email:SmtpPort"], out var p) ? p : 587;
        var fromAddress = _configuration["Email:FromAddress"] ?? "noreply@node.be";
        var fromName = _configuration["Email:FromName"] ?? "Node";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);

        // Only authenticate when credentials are configured
        // (some relay servers don't require authentication).
        var smtpUser = _configuration["Email:SmtpUser"];
        var smtpPassword = _configuration["Email:SmtpPassword"];
        if (!string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPassword))
        {
            await client.AuthenticateAsync(smtpUser, smtpPassword);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(quit: true);

        _logger.LogInformation("Verification email sent to {To}.", to);
    }
}
