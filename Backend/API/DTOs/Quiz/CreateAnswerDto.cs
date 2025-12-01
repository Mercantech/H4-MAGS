using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Quiz;

public class CreateAnswerDto
{
    [Required]
    [StringLength(500)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public int OrderIndex { get; set; }
}

