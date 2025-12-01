namespace API.Models;

public class Participant : BaseEntity
{
    public string Nickname { get; set; } = string.Empty;
    public int TotalPoints { get; set; } = 0; // Samlet point for deltageren
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public int QuizSessionId { get; set; }

    // Navigation properties
    public QuizSession QuizSession { get; set; } = null!;
    public ICollection<ParticipantAnswer> Answers { get; set; } = new List<ParticipantAnswer>();
}

