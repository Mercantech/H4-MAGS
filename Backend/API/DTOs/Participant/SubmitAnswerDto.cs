using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Participant;

public class SubmitAnswerDto
{
    [Required]
    public int ParticipantId { get; set; }

    [Required]
    public int QuestionId { get; set; }

    [Required]
    public int AnswerId { get; set; }

    [Range(0, int.MaxValue)]
    public int ResponseTimeMs { get; set; }
}

