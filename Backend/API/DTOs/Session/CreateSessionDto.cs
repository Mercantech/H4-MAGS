using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Session;

public class CreateSessionDto
{
    [Required]
    public int QuizId { get; set; }
}

