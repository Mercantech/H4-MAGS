namespace API.DTOs.Participant;

public class LeaderboardEntryDto
{
    public int ParticipantId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int Rank { get; set; }
}

