namespace API.Models;

public enum UserRole
{
    Student,
    Teacher,
    Admin
}

public class User : BaseEntity
{
    private string _username = string.Empty;
    private string _email = string.Empty;

    public string Username
    {
        get => _username;
        set => _username = value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    public string Email
    {
        get => _email;
        set => _email = value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    /// <summary>
    /// Hashed password. Null hvis brugeren kun bruger SSO (har ingen password).
    /// </summary>
    public string? PasswordHash { get; set; }
    
    public UserRole Role { get; set; } = UserRole.Student;
    public DateTime? LastLoginAt { get; set; }
    public string? Picture { get; set; } // Google profile picture URL

    // Navigation properties
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    
    /// <summary>
    /// Eksterne identiteter (SSO providers) knyttet til denne bruger.
    /// En bruger kan have både password og flere SSO providers.
    /// </summary>
    public ICollection<ExternalIdentity> ExternalIdentities { get; set; } = new List<ExternalIdentity>();
}
