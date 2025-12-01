namespace API.DTOs.Participant;

public class ParticipantDto
{
    public int Id { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public DateTime JoinedAt { get; set; }
    public int QuizSessionId { get; set; }
}

