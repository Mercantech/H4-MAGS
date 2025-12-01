namespace API.DTOs.Session;

public class SessionDto
{
    public int Id { get; set; }
    public string SessionPin { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
}

