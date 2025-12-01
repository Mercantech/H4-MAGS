namespace API.Models;

public class ParticipantAnswer : BaseEntity
{
    public int PointsEarned { get; set; } = 0; // Point modtaget for dette svar
    public int ResponseTimeMs { get; set; } // Hvor hurtigt deltageren svarede (millisekunder)
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

    // Foreign keys
    public int ParticipantId { get; set; }
    public int QuestionId { get; set; }
    public int AnswerId { get; set; }

    // Navigation properties
    public Participant Participant { get; set; } = null!;
    public Question Question { get; set; } = null!;
    public Answer Answer { get; set; } = null!;
}

