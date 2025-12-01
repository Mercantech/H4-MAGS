namespace API.Models;

public enum QuizStatus
{
    Created,    // Quiz er oprettet, klar til at starte
    Active,     // Quiz er i gang
    Finished    // Quiz er afsluttet
}

public class Quiz : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Pin { get; set; } = string.Empty; // Unik PIN for at deltage i quizzen
    public QuizStatus Status { get; set; } = QuizStatus.Created;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    // Foreign key
    public int UserId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<QuizSession> Sessions { get; set; } = new List<QuizSession>();
}
