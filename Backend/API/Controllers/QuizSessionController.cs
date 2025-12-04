using API.Data;
using API.DTOs.Participant;
using API.DTOs.Session;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuizSessionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QuizSessionController> _logger;

    public QuizSessionController(ApplicationDbContext context, ILogger<QuizSessionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Start en ny quiz session
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Opretter ny session med unik PIN.</remarks>
    [HttpPost]
    public async Task<ActionResult<SessionDto>> CreateSession(CreateSessionDto createDto)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == createDto.QuizId);

        if (quiz == null)
        {
            return NotFound($"Quiz med ID {createDto.QuizId} blev ikke fundet");
        }

        if (quiz.Questions.Count == 0)
        {
            return BadRequest("Quiz har ingen spørgsmål og kan ikke startes");
        }

        // Generer unik session PIN
        string sessionPin;
        do
        {
            sessionPin = GeneratePin();
        } while (await _context.QuizSessions.AnyAsync(s => s.SessionPin == sessionPin));

        var session = new QuizSession
        {
            SessionPin = sessionPin,
            Status = QuizSessionStatus.Waiting,
            QuizId = quiz.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.QuizSessions.Add(session);
        await _context.SaveChangesAsync();

        var sessionDto = await MapToSessionDto(session);
        return CreatedAtAction(nameof(GetSession), new { id = session.Id }, sessionDto);
    }

    /// <summary>
    /// Hent en session
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Returnerer session info med deltagere. Tjekker automatisk om tiden er udløbet for nuværende spørgsmål.</remarks>
    [HttpGet("{id}")]
    public async Task<ActionResult<SessionDto>> GetSession(int id)
    {
        var session = await _context.QuizSessions
            .Include(s => s.Quiz)
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null)
        {
            return NotFound($"Session med ID {id} blev ikke fundet");
        }

        // Tjek om tiden er udløbet for nuværende spørgsmål (hvis quiz er i gang)
        if (session.Status == QuizSessionStatus.InProgress && session.CurrentQuestionOrderIndex.HasValue)
        {
            await CheckAndAdvanceQuestionIfTimeout(session);
            // Reload session efter potentiel opdatering
            session = await _context.QuizSessions
                .Include(s => s.Quiz)
                .Include(s => s.Participants)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        var sessionDto = await MapToSessionDto(session);
        return Ok(sessionDto);
    }

    /// <summary>
    /// Hent session via PIN
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Henter session via unik session PIN. Tjekker automatisk om tiden er udløbet for nuværende spørgsmål.</remarks>
    [HttpGet("pin/{pin}")]
    public async Task<ActionResult<SessionDto>> GetSessionByPin(string pin)
    {
        var session = await _context.QuizSessions
            .Include(s => s.Quiz)
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.SessionPin == pin);

        if (session == null)
        {
            return NotFound($"Session med PIN {pin} blev ikke fundet");
        }

        // Tjek om tiden er udløbet for nuværende spørgsmål (hvis quiz er i gang)
        if (session.Status == QuizSessionStatus.InProgress && session.CurrentQuestionOrderIndex.HasValue)
        {
            await CheckAndAdvanceQuestionIfTimeout(session);
            // Reload session efter potentiel opdatering
            session = await _context.QuizSessions
                .Include(s => s.Quiz)
                .Include(s => s.Participants)
                .FirstOrDefaultAsync(s => s.SessionPin == pin);
        }

        var sessionDto = await MapToSessionDto(session);
        return Ok(sessionDto);
    }

    /// <summary>
    /// Start en session
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Starter quiz session (skal have mindst én deltager).</remarks>
    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartSession(int id)
    {
        var session = await _context.QuizSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null)
        {
            return NotFound($"Session med ID {id} blev ikke fundet");
        }

        if (session.Status != QuizSessionStatus.Waiting)
        {
            return BadRequest($"Session kan kun startes når status er 'Waiting'. Nuværende status: {session.Status}");
        }

        if (session.Participants.Count == 0)
        {
            return BadRequest("Session kan ikke startes uden deltagere");
        }

        session.Status = QuizSessionStatus.InProgress;
        session.StartedAt = DateTime.UtcNow;
        session.CurrentQuestionOrderIndex = 1; // Start med første spørgsmål
        session.CurrentQuestionStartedAt = DateTime.UtcNow; // Start tid for første spørgsmål

        await _context.SaveChangesAsync();

        return Ok(new { message = "Session er startet", sessionId = session.Id, status = session.Status.ToString() });
    }

    /// <summary>
    /// Afslut en session
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Sætter session status til Completed.</remarks>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteSession(int id)
    {
        var session = await _context.QuizSessions.FindAsync(id);
        if (session == null)
        {
            return NotFound($"Session med ID {id} blev ikke fundet");
        }

        session.Status = QuizSessionStatus.Completed;
        session.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Session er afsluttet", sessionId = session.Id, status = session.Status.ToString() });
    }

    /// <summary>
    /// Join en session som deltager
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Tilføjer deltager til session med unikt nickname.</remarks>
    [HttpPost("join")]
    public async Task<ActionResult<ParticipantDto>> JoinSession(JoinSessionDto joinDto)
    {
        var session = await _context.QuizSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.SessionPin == joinDto.SessionPin);

        if (session == null)
        {
            return NotFound($"Session med PIN {joinDto.SessionPin} blev ikke fundet");
        }

        if (session.Status != QuizSessionStatus.Waiting)
        {
            return BadRequest($"Kan kun join session når status er 'Waiting'. Nuværende status: {session.Status}");
        }

        // Tjek om nickname allerede er taget i denne session
        if (session.Participants.Any(p => p.Nickname.Equals(joinDto.Nickname, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict($"Nickname '{joinDto.Nickname}' er allerede taget i denne session");
        }

        var participant = new Participant
        {
            Nickname = joinDto.Nickname,
            TotalPoints = 0,
            QuizSessionId = session.Id,
            JoinedAt = DateTime.UtcNow
        };

        _context.Participants.Add(participant);
        await _context.SaveChangesAsync();

        var participantDto = new ParticipantDto
        {
            Id = participant.Id,
            Nickname = participant.Nickname,
            TotalPoints = participant.TotalPoints,
            JoinedAt = participant.JoinedAt,
            QuizSessionId = participant.QuizSessionId
        };

        return CreatedAtAction(nameof(GetParticipant), new { participantId = participant.Id }, participantDto);
    }

    /// <summary>
    /// Hent deltager
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Returnerer deltager info.</remarks>
    [HttpGet("participants/{participantId}")]
    public async Task<ActionResult<ParticipantDto>> GetParticipant(int participantId)
    {
        var participant = await _context.Participants.FindAsync(participantId);
        if (participant == null)
        {
            return NotFound($"Deltager med ID {participantId} blev ikke fundet");
        }

        var participantDto = new ParticipantDto
        {
            Id = participant.Id,
            Nickname = participant.Nickname,
            TotalPoints = participant.TotalPoints,
            JoinedAt = participant.JoinedAt,
            QuizSessionId = participant.QuizSessionId
        };

        return Ok(participantDto);
    }

    private async Task<SessionDto> MapToSessionDto(QuizSession session)
    {
        var quiz = await _context.Quizzes.FindAsync(session.QuizId);
        
        return new SessionDto
        {
            Id = session.Id,
            SessionPin = session.SessionPin,
            Status = session.Status.ToString(),
            CreatedAt = session.CreatedAt,
            StartedAt = session.StartedAt,
            CompletedAt = session.CompletedAt,
            CurrentQuestionOrderIndex = session.CurrentQuestionOrderIndex,
            QuizId = session.QuizId,
            QuizTitle = quiz?.Title ?? string.Empty,
            ParticipantCount = session.Participants?.Count ?? 0
        };
    }

    /// <summary>
    /// Tjek om tiden er udløbet for nuværende spørgsmål og gå videre hvis den er
    /// </summary>
    private async Task CheckAndAdvanceQuestionIfTimeout(QuizSession session)
    {
        if (!session.CurrentQuestionOrderIndex.HasValue || !session.CurrentQuestionStartedAt.HasValue)
        {
            return;
        }

        // Hent nuværende spørgsmål
        var currentQuestion = await _context.Questions
            .Where(q => q.QuizId == session.QuizId && q.OrderIndex == session.CurrentQuestionOrderIndex.Value)
            .FirstOrDefaultAsync();

        if (currentQuestion == null)
        {
            return;
        }

        // Tjek om tiden er udløbet
        var elapsed = DateTime.UtcNow - session.CurrentQuestionStartedAt.Value;
        if (elapsed.TotalSeconds < currentQuestion.TimeLimitSeconds)
        {
            return; // Tiden er ikke udløbet endnu
        }

        // Tiden er udløbet - gå videre til næste spørgsmål
        var allQuestions = await _context.Questions
            .Where(q => q.QuizId == session.QuizId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();

        var currentQuestionIndex = allQuestions.FindIndex(q => q.Id == currentQuestion.Id);
        
        if (currentQuestionIndex < allQuestions.Count - 1)
        {
            // Der er flere spørgsmål - gå videre til næste
            session.CurrentQuestionOrderIndex = allQuestions[currentQuestionIndex + 1].OrderIndex;
            session.CurrentQuestionStartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        else
        {
            // Dette var sidste spørgsmål - afslut sessionen
            session.Status = QuizSessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            session.CurrentQuestionOrderIndex = null;
            session.CurrentQuestionStartedAt = null;
            await _context.SaveChangesAsync();
        }
    }

    private static string GeneratePin()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}

