using System.ComponentModel.DataAnnotations;
using API.Models;

namespace API.DTOs.Auth;

public class UpdateUserDto
{
    [StringLength(100, MinimumLength = 3)]
    public string? Username { get; set; }

    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    public UserRole? Role { get; set; } // Kun admin kan ændre rolle
}

