namespace API.Models;

public class Question : BaseEntity
{
    public string Text { get; set; } = string.Empty;
    public int TimeLimitSeconds { get; set; } = 30; // Standard tid per spørgsmål
    public int Points { get; set; } = 1000; // Point værdi for korrekt svar
    public int OrderIndex { get; set; } // Rækkefølge af spørgsmål i quizzen

    // Foreign key
    public int QuizId { get; set; }

    // Navigation properties
    public Quiz Quiz { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<ParticipantAnswer> ParticipantAnswers { get; set; } = new List<ParticipantAnswer>();
}

