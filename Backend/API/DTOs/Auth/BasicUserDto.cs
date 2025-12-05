namespace API.DTOs.Auth;

/// <summary>
/// Basis bruger DTO - indeholder grundlæggende information uden følsomme data
/// </summary>
public class BasicUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Picture { get; set; }
}

