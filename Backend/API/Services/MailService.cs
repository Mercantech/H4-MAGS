using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace API.Services;

/// <summary>
/// Konfiguration for udgående mail (Gmail SMTP).
/// Brug et Google App Password – ikke jeres almindelige Gmail-adgangskode.
/// </summary>
public class MailSettings
{
    public const string SectionName = "Mail";

    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
}

public interface IMailService
{
    /// <summary>
    /// Sender velkomstmail til en ny bruger efter tilmelding.
    /// </summary>
    Task SendWelcomeEmailAsync(string toEmail, string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sender en testmail (til demo-endpoint). Valgfri emne, HTML-body eller skabelon (fx "Welcome").
    /// </summary>
    Task SendTestEmailAsync(string toEmail, string? subject = null, string? htmlBody = null, string? templateName = null, string? templateUsername = null, CancellationToken cancellationToken = default);
}

public class MailService : IMailService
{
    private readonly MailSettings _settings;
    private readonly ILogger<MailService> _logger;
    private readonly IWebHostEnvironment _env;

    public MailService(
        IOptions<MailSettings> options,
        ILogger<MailService> logger,
        IWebHostEnvironment env)
    {
        _settings = options.Value;
        _logger = logger;
        _env = env;
    }

    /// <inheritdoc />
    public async Task SendWelcomeEmailAsync(string toEmail, string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.UserName) || string.IsNullOrWhiteSpace(_settings.Password))
        {
            _logger.LogWarning("Mail er ikke konfigureret (UserName/Password). Velkomstmail springes over.");
            return;
        }

        var fromEmail = string.IsNullOrWhiteSpace(_settings.FromEmail) ? _settings.UserName : _settings.FromEmail;
        var fromName = string.IsNullOrWhiteSpace(_settings.FromName) ? "Kahoot.Mercantec.tech" : _settings.FromName;

        using var client = CreateSmtpClient();

        var htmlBody = await BuildWelcomeEmailFromTemplateAsync(username, fromName, cancellationToken)
            ?? BuildWelcomeEmailHtml(username, fromName);

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = "Velkommen til Kahoot.Mercantec.tech 🎮",
            Body = htmlBody,
            IsBodyHtml = true
        };
        mailMessage.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Velkomstmail sendt til {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kunne ikke sende velkomstmail til {Email}", toEmail);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SendTestEmailAsync(string toEmail, string? subject = null, string? htmlBody = null, string? templateName = null, string? templateUsername = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.UserName) || string.IsNullOrWhiteSpace(_settings.Password))
        {
            throw new InvalidOperationException("Mail er ikke konfigureret (UserName/Password).");
        }

        var fromEmail = string.IsNullOrWhiteSpace(_settings.FromEmail) ? _settings.UserName : _settings.FromEmail;
        var fromName = string.IsNullOrWhiteSpace(_settings.FromName) ? "Kahoot.Mercantec.tech" : _settings.FromName;
        var subj = string.IsNullOrWhiteSpace(subject) ? "Testmail fra Kahoot.Mercantec.tech 🎮" : subject;

        string body;
        var useWelcomeTemplate = string.Equals(templateName, "Welcome", StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrWhiteSpace(templateName) && string.IsNullOrWhiteSpace(htmlBody));
        if (useWelcomeTemplate)
        {
            var username = string.IsNullOrWhiteSpace(templateUsername) ? "DemoBruger" : templateUsername;
            body = await BuildWelcomeEmailFromTemplateAsync(username, fromName, cancellationToken)
                ?? BuildWelcomeEmailHtml(username, fromName);
        }
        else if (!string.IsNullOrWhiteSpace(htmlBody))
        {
            body = htmlBody;
        }
        else
        {
            body = "<p style=\"font-family:sans-serif;\">Dette er en testmail fra Kahoot.Mercantec.tech. Mail-service kører korrekt.</p>";
        }

        using var client = CreateSmtpClient();

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subj,
            Body = body,
            IsBodyHtml = true
        };
        mailMessage.To.Add(toEmail);

        await client.SendMailAsync(mailMessage, cancellationToken);
        _logger.LogInformation("Testmail sendt til {Email}", toEmail);
    }

    private SmtpClient CreateSmtpClient()
    {
        var user = (_settings.UserName ?? "").Trim();
        var pass = (_settings.Password ?? "").Trim();
        return new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(user, pass)
        };
    }

    /// <summary>
    /// Loader velkomstmail fra HTML-fil (EmailTemplates/WelcomeEmail.html) og udfylder
    /// pladsholdere: {{Username}}, {{FromName}}. Returnerer null hvis filen ikke findes.
    /// </summary>
    private async Task<string?> BuildWelcomeEmailFromTemplateAsync(
        string username,
        string fromName,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(_env.ContentRootPath, "EmailTemplates", "WelcomeEmail.html");
        if (!File.Exists(path))
        {
            _logger.LogDebug("Email-skabelon ikke fundet på {Path}, bruger inline HTML", path);
            return null;
        }

        var html = await File.ReadAllTextAsync(path, cancellationToken);
        var u = System.Net.WebUtility.HtmlEncode(username);
        var f = System.Net.WebUtility.HtmlEncode(fromName);

        return html
            .Replace("{{Username}}", u)
            .Replace("{{FromName}}", f);
    }

    /// <summary>
    /// Bygger HTML-body til velkomstmail med inline CSS (email-klient kompatibel).
    /// Bruges som fallback når template-fil ikke findes.
    /// </summary>
    private static string BuildWelcomeEmailHtml(string username, string fromName)
    {
        var u = System.Net.WebUtility.HtmlEncode(username);
        var f = System.Net.WebUtility.HtmlEncode(fromName);

        return "<!DOCTYPE html><html lang=\"da\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Velkommen til Kahoot.Mercantec.tech</title></head>" +
            "<body style=\"margin:0; padding:0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color:#f0f2f5;\">" +
            "<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"background-color:#f0f2f5;\">" +
            "<tr><td align=\"center\" style=\"padding:40px 20px;\">" +
            "<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"max-width:560px; border-radius:16px; overflow:hidden; box-shadow:0 8px 24px rgba(0,0,0,0.12);\">" +
            "<tr><td style=\"background:linear-gradient(135deg, #6c5ce7 0%, #a29bfe 100%); padding:32px 40px; text-align:center;\">" +
            "<h1 style=\"margin:0; color:#ffffff; font-size:28px; font-weight:700; letter-spacing:-0.5px;\">Kahoot.Mercantec.tech</h1>" +
            "<p style=\"margin:8px 0 0 0; color:rgba(255,255,255,0.9); font-size:14px;\">Din quiz-platform</p></td></tr>" +
            "<tr><td style=\"background-color:#ffffff; padding:40px;\">" +
            "<p style=\"margin:0 0 16px 0; color:#2d3436; font-size:18px; font-weight:600;\">Hej " + u + "! 👋</p>" +
            "<p style=\"margin:0 0 24px 0; color:#636e72; font-size:16px; line-height:1.6;\">Velkommen til platformen. Du er nu tilmeldt og kan logge ind og deltage i quizzer.</p>" +
            "<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\"><tr><td style=\"background-color:#f8f9fa; border-radius:12px; padding:20px; border-left:4px solid #6c5ce7;\">" +
            "<p style=\"margin:0; color:#2d3436; font-size:15px; line-height:1.5;\">Du kan nu <strong>logge ind</strong> med din e-mail og adgangskode på platformen.</p></td></tr></table>" +
            "</td></tr>" +
            "<tr><td style=\"background-color:#dfe6e9; padding:24px 40px; text-align:center;\">" +
            "<p style=\"margin:0; color:#636e72; font-size:14px;\">Med venlig hilsen,</p>" +
            "<p style=\"margin:4px 0 0 0; color:#2d3436; font-weight:600;\">" + f + "</p></td></tr>" +
            "</table></td></tr></table></body></html>";
    }
}
