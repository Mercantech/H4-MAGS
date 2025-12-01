using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Session;

public class JoinSessionDto
{
    [Required]
    [StringLength(10)]
    public string SessionPin { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Nickname { get; set; } = string.Empty;
}

