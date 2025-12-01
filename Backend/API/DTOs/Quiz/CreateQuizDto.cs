using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Quiz;

public class CreateQuizDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public int UserId { get; set; }

    public List<CreateQuestionDto> Questions { get; set; } = new();
}

