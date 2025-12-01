namespace API.Models;

/// <summary>
/// Repræsenterer en ekstern identitet (SSO provider) knyttet til en bruger.
/// Tillader en bruger at have flere authentication metoder (f.eks. både password og Google).
/// </summary>
public class ExternalIdentity : BaseEntity
{
    /// <summary>
    /// Den bruger som denne eksterne identitet er knyttet til
    /// </summary>
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Provider navn (f.eks. "Google", "Microsoft", "Facebook")
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Provider's unikke ID for denne bruger (f.eks. Google Sub ID)
    /// </summary>
    public string ProviderUserId { get; set; } = string.Empty;

    /// <summary>
    /// Provider email (kan være forskellig fra User.Email hvis bruger har linket flere accounts)
    /// </summary>
    public string? ProviderEmail { get; set; }

    /// <summary>
    /// Dato for sidste login med denne provider
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}

