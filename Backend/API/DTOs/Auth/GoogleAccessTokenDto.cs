using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Auth;

public class GoogleAccessTokenDto
{
    [Required]
    public string AccessToken { get; set; } = string.Empty;
}

