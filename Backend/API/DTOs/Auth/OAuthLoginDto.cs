using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Auth;

/// <summary>
/// [Google Auth] Generisk DTO for OAuth login – body til POST /api/auth/oauth-login (provider + accessToken).
/// </summary>
public class OAuthLoginDto
{
    /// <summary>
    /// OAuth provider navn (Google, Microsoft, GitHub, etc.)
    /// </summary>
    [Required]
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Access token fra provider
    /// </summary>
    [Required]
    public string AccessToken { get; set; } = string.Empty;
}

