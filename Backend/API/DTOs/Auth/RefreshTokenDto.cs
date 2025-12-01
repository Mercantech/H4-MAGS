using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Auth;

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

