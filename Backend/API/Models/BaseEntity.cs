namespace API.Models;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(2);
    public DateTime? UpdatedAt { get; set; }
}


