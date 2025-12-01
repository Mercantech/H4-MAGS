using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Auth;

public class LoginDto
{
    [Required]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

