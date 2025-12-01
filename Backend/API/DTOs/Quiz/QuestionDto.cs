namespace API.DTOs.Quiz;

public class QuestionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int TimeLimitSeconds { get; set; }
    public int Points { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
    public int QuizId { get; set; }
    public List<AnswerDto> Answers { get; set; } = new();
}

