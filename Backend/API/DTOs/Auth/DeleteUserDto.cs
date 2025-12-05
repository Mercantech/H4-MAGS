using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Auth;

/// <summary>
/// DTO for at slette egen bruger
/// </summary>
public class DeleteUserDto
{
    /// <summary>
    /// Password bekræftelse (påkrævet hvis brugeren har password, ellers kan det være null for SSO-only brugere)
    /// </summary>
    public string? Password { get; set; }
}

