namespace API.Models;

public enum QuizSessionStatus
{
    Waiting,    // Ventende på deltagere
    InProgress, // Quiz i gang
    Completed   // Quiz afsluttet
}

public class QuizSession : BaseEntity
{
    public string SessionPin { get; set; } = string.Empty; // Unik PIN for denne session
    public QuizSessionStatus Status { get; set; } = QuizSessionStatus.Waiting;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CurrentQuestionOrderIndex { get; set; } // Nuværende spørgsmål alle deltagere skal se (null = ingen aktivt spørgsmål)
    public DateTime? CurrentQuestionStartedAt { get; set; } // Hvornår det nuværende spørgsmål startede (for timeout håndtering)

    // Foreign key
    public int QuizId { get; set; }

    // Navigation properties
    public Quiz Quiz { get; set; } = null!;
    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
}

