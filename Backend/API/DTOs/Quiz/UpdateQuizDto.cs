using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Quiz;

public class UpdateQuizDto
{
    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }
}

