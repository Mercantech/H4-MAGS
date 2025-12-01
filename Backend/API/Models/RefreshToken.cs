namespace API.Models;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;

    // Navigation property
    public User User { get; set; } = null!;
}

