namespace API.Models;

public class Answer : BaseEntity
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; } // Angiver om dette er det korrekte svar
    public int OrderIndex { get; set; } // Rækkefølge af svar (A, B, C, D)

    // Foreign key
    public int QuestionId { get; set; }

    // Navigation properties
    public Question Question { get; set; } = null!;
    public ICollection<ParticipantAnswer> ParticipantAnswers { get; set; } = new List<ParticipantAnswer>();
}

