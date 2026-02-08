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
    /// Valgfri HTML-body. Hvis tom sendes en simpel testmail.
    /// </summary>
    public string? Body { get; set; }
}
