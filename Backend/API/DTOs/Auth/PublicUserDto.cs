namespace API.DTOs.Auth;

/// <summary>
/// Minimal bruger DTO til offentlig visning - indeholder kun essentiel information
/// </summary>
public class PublicUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Picture { get; set; }
}

