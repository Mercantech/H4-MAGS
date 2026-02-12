using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Mail;

/// <summary>
/// Request til demo-mail endpoint. Kræver Admin-rolle.
/// </summary>
public class SendDemoMailRequest
{
    /// <summary>
    /// E-mailadresse der modtager testmailen.
    /// </summary>
    [Required(ErrorMessage = "Modtager e-mail er påkrævet")]
    [EmailAddress(ErrorMessage = "Ugyldig e-mailadresse")]
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// Valgfrit emne. Default: "Testmail fra Kahoot.Mercantec.tech 🎮"
    /// </summary>
    [MaxLength(500)]
    public string? Subject { get; set; }

    /// <summary>
    /// Valgfri HTML-body. Hvis tom og ingen Template angives sendes velkomst-skabelonen med demo-bruger.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Valgfri skabelon til demo. "Welcome" = velkomstmail-skabelon (EmailTemplates/WelcomeEmail.html).
    /// Når angivet bruges skabelonen og Body ignoreres.
    /// </summary>
    [MaxLength(50)]
    public string? Template { get; set; }

    /// <summary>
    /// Brugernavn til skabelon-placeholders (fx {{Username}}). Bruges ved Template = "Welcome". Default: "DemoBruger".
    /// </summary>
    [MaxLength(200)]
    public string? TemplateUsername { get; set; }
}
