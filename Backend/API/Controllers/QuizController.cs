using API.Data;
using API.DTOs.Quiz;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuizController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuizController> _logger;

    public QuizController(ApplicationDbContext context, ILogger<QuizController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Hent alle quizzers
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Returnerer liste af alle quizzers.</remarks>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuizSummaryDto>>> GetQuizzes()
    {
        var quizzes = await _context.Quizzes
            .Include(q => q.Questions)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new QuizSummaryDto
            {
                Id = q.Id,
                Title = q.Title,
                Description = q.Description,
                Pin = q.Pin,
                Status = q.Status.ToString(),
                CreatedAt = q.CreatedAt,
                QuestionCount = q.Questions.Count
            })
            .ToListAsync();

        return Ok(quizzes);
    }

    /// <summary>
    /// Hent en specifik quiz med alle spørgsmål og svar
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Returnerer fuld quiz med spørgsmål og korrekte svar.</remarks>
    [HttpGet("{id}")]
    public async Task<ActionResult<QuizDto>> GetQuiz(int id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
        {
            return NotFound($"Quiz med ID {id} blev ikke fundet");
        }

        var quizDto = MapToQuizDto(quiz);
        return Ok(quizDto);
    }

    /// <summary>
    /// Hent quiz via PIN
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Henter quiz via unik PIN kode.</remarks>
    [HttpGet("pin/{pin}")]
    public async Task<ActionResult<QuizDto>> GetQuizByPin(string pin)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Pin == pin);

        if (quiz == null)
        {
            return NotFound($"Quiz med PIN {pin} blev ikke fundet");
        }

        var quizDto = MapToQuizDto(quiz);
        return Ok(quizDto);
    }

    /// <summary>
    /// Opret en ny quiz
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Opretter quiz med spørgsmål og svar.</remarks>
    [HttpPost]
    public async Task<ActionResult<QuizDto>> CreateQuiz(CreateQuizDto createDto)
    {
        // Valider at user eksisterer
        var user = await _context.Users.FindAsync(createDto.UserId);
        if (user == null)
        {
            return BadRequest($"Bruger med ID {createDto.UserId} blev ikke fundet");
        }

        // Generer unik PIN
        string pin;
        do
        {
            pin = GeneratePin();
        } while (await _context.Quizzes.AnyAsync(q => q.Pin == pin));

        var quiz = new Quiz
        {
            Title = createDto.Title,
            Description = createDto.Description,
            Pin = pin,
            Status = QuizStatus.Created,
            UserId = createDto.UserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        // Tilføj spørgsmål
        var orderIndex = 0;
        foreach (var questionDto in createDto.Questions.OrderBy(q => q.OrderIndex))
        {
            var question = new Question
            {
                Text = questionDto.Text,
                TimeLimitSeconds = questionDto.TimeLimitSeconds,
                Points = questionDto.Points,
                OrderIndex = questionDto.OrderIndex > 0 ? questionDto.OrderIndex : ++orderIndex,
                QuizId = quiz.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            // Valider at der er præcis ét korrekt svar
            var correctAnswers = questionDto.Answers.Count(a => a.IsCorrect);
            if (correctAnswers != 1)
            {
                return BadRequest($"Hvert spørgsmål skal have præcis ét korrekt svar. Spørgsmål '{questionDto.Text}' har {correctAnswers} korrekte svar.");
            }

            // Tilføj svar
            var answerOrderIndex = 0;
            foreach (var answerDto in questionDto.Answers.OrderBy(a => a.OrderIndex))
            {
                var answer = new Answer
                {
                    Text = answerDto.Text,
                    IsCorrect = answerDto.IsCorrect,
                    OrderIndex = answerDto.OrderIndex > 0 ? answerDto.OrderIndex : ++answerOrderIndex,
                    QuestionId = question.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Answers.Add(answer);
            }
        }

        await _context.SaveChangesAsync();

        // Hent quiz med alle relations
        var createdQuiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
            .FirstAsync(q => q.Id == quiz.Id);

        var result = MapToQuizDto(createdQuiz);
        return CreatedAtAction(nameof(GetQuiz), new { id = quiz.Id }, result);
    }

    /// <summary>
    /// Opdater en quiz
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Opdaterer quiz titel og beskrivelse.</remarks>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuiz(int id, UpdateQuizDto updateDto)
    {
        var quiz = await _context.Quizzes.FindAsync(id);
        if (quiz == null)
        {
            return NotFound($"Quiz med ID {id} blev ikke fundet");
        }

        if (!string.IsNullOrWhiteSpace(updateDto.Title))
        {
            quiz.Title = updateDto.Title;
        }

        if (updateDto.Description != null)
        {
            quiz.Description = updateDto.Description;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Slet en quiz
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Sletter quiz og alle relaterede data.</remarks>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuiz(int id)
    {
        var quiz = await _context.Quizzes.FindAsync(id);
        if (quiz == null)
        {
            return NotFound($"Quiz med ID {id} blev ikke fundet");
        }

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Start en quiz
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Sætter quiz status til Active.</remarks>
    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartQuiz(int id)
    {
        var quiz = await _context.Quizzes.FindAsync(id);
        if (quiz == null)
        {
            return NotFound($"Quiz med ID {id} blev ikke fundet");
        }

        if (quiz.Status != QuizStatus.Created)
        {
            return BadRequest($"Quiz kan kun startes når status er 'Created'. Nuværende status: {quiz.Status}");
        }

        quiz.Status = QuizStatus.Active;
        quiz.StartedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Quiz er startet", quizId = quiz.Id, status = quiz.Status.ToString() });
    }

    /// <summary>
    /// Afslut en quiz
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Sætter quiz status til Finished.</remarks>
    [HttpPost("{id}/finish")]
    public async Task<IActionResult> FinishQuiz(int id)
    {
        var quiz = await _context.Quizzes.FindAsync(id);
        if (quiz == null)
        {
            return NotFound($"Quiz med ID {id} blev ikke fundet");
        }

        quiz.Status = QuizStatus.Finished;
        quiz.FinishedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Quiz er afsluttet", quizId = quiz.Id, status = quiz.Status.ToString() });
    }

    // Helper methods
    private static QuizDto MapToQuizDto(Quiz quiz)
    {
        return new QuizDto
        {
            Id = quiz.Id,
            Title = quiz.Title,
            Description = quiz.Description,
            Pin = quiz.Pin,
            Status = quiz.Status.ToString(),
            CreatedAt = quiz.CreatedAt,
            UpdatedAt = quiz.UpdatedAt,
            StartedAt = quiz.StartedAt,
            FinishedAt = quiz.FinishedAt,
            UserId = quiz.UserId,
            Questions = quiz.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(q => new QuestionDto
                {
                    Id = q.Id,
                    Text = q.Text,
                    TimeLimitSeconds = q.TimeLimitSeconds,
                    Points = q.Points,
                    OrderIndex = q.OrderIndex,
                    CreatedAt = q.CreatedAt,
                    QuizId = q.QuizId,
                    Answers = q.Answers
                        .OrderBy(a => a.OrderIndex)
                        .Select(a => new AnswerDto
                        {
                            Id = a.Id,
                            Text = a.Text,
                            IsCorrect = a.IsCorrect,
                            OrderIndex = a.OrderIndex,
                            CreatedAt = a.CreatedAt
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static string GeneratePin()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}

