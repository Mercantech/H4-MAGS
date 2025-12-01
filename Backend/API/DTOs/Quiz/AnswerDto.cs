namespace API.DTOs.Quiz;

public class AnswerDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
}

