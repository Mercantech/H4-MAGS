using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Auth;

/// <summary>
/// DTO for at opdatere password
/// Virker både for OAuth brugere (tilføjer password) og normale brugere (opdaterer password)
/// </summary>
public class UpdatePasswordDto
{
    [Required]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password skal være mellem 6 og 100 karakterer")]
    public string NewPassword { get; set; } = string.Empty;
}

