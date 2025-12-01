using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Quiz;

public class CreateQuestionDto
{
    [Required]
    [StringLength(500)]
    public string Text { get; set; } = string.Empty;

    [Range(1, 300)]
    public int TimeLimitSeconds { get; set; } = 30;

    [Range(1, 10000)]
    public int Points { get; set; } = 1000;

    public int OrderIndex { get; set; }

    [Required]
    [MinLength(2, ErrorMessage = "Hvert spørgsmål skal have mindst 2 svar")]
    [MaxLength(6, ErrorMessage = "Hvert spørgsmål kan maksimalt have 6 svar")]
    public List<CreateAnswerDto> Answers { get; set; } = new();
}

