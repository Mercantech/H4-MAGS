using API.Data;
using API.DTOs.Participant;
using API.DTOs.Quiz;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParticipantController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ParticipantController> _logger;

    public ParticipantController(ApplicationDbContext context, ILogger<ParticipantController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Indsend svar på et spørgsmål
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Beregner og gemmer point baseret på hastighed og korrekthed.</remarks>
    [HttpPost("submit-answer")]
    public async Task<ActionResult> SubmitAnswer(SubmitAnswerDto submitDto)
    {
        // Valider deltager
        var participant = await _context.Participants
            .Include(p => p.QuizSession)
            .FirstOrDefaultAsync(p => p.Id == submitDto.ParticipantId);

        if (participant == null)
        {
            return NotFound($"Deltager med ID {submitDto.ParticipantId} blev ikke fundet");
        }

        // Valider session status
        if (participant.QuizSession.Status != QuizSessionStatus.InProgress)
        {
            return BadRequest($"Kan kun indsende svar når session er 'InProgress'. Nuværende status: {participant.QuizSession.Status}");
        }

        // Valider spørgsmål
        var question = await _context.Questions
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == submitDto.QuestionId);

        if (question == null)
        {
            return NotFound($"Spørgsmål med ID {submitDto.QuestionId} blev ikke fundet");
        }

        // Valider at spørgsmålet tilhører quizzen i sessionen
        if (question.QuizId != participant.QuizSession.QuizId)
        {
            return BadRequest("Spørgsmål tilhører ikke quizzen i denne session");
        }

        // Valider at spørgsmålet matcher sessionens nuværende spørgsmål (centralt styret)
        if (!participant.QuizSession.CurrentQuestionOrderIndex.HasValue || 
            question.OrderIndex != participant.QuizSession.CurrentQuestionOrderIndex.Value)
        {
            return BadRequest($"Kan kun indsende svar på det nuværende spørgsmål (order index {participant.QuizSession.CurrentQuestionOrderIndex?.ToString() ?? "ingen"}). Du prøvede at svare på spørgsmål med order index {question.OrderIndex}");
        }

        // Valider svar
        var answer = question.Answers.FirstOrDefault(a => a.Id == submitDto.AnswerId);
        if (answer == null)
        {
            return NotFound($"Svar med ID {submitDto.AnswerId} blev ikke fundet for dette spørgsmål");
        }

        // Tjek om deltageren allerede har svaret på dette spørgsmål
        var existingAnswer = await _context.ParticipantAnswers
            .FirstOrDefaultAsync(pa => pa.ParticipantId == submitDto.ParticipantId && 
                                       pa.QuestionId == submitDto.QuestionId);

        if (existingAnswer != null)
        {
            return Conflict("Deltageren har allerede svaret på dette spørgsmål");
        }

        // Beregn point baseret på om svaret er korrekt og hvor hurtigt det blev svaret
        int pointsEarned = 0;
        if (answer.IsCorrect)
        {
            // Point beregning: Jo hurtigere, jo flere point (Kahoot-stil)
            var maxPoints = question.Points;
            var timeLimitMs = question.TimeLimitSeconds * 1000;
            var timeBonus = Math.Max(0, timeLimitMs - submitDto.ResponseTimeMs);
            var bonusPercentage = (double)timeBonus / timeLimitMs;
            pointsEarned = (int)(maxPoints * (1.0 + bonusPercentage * 0.5)); // Max 50% bonus for hurtigt svar
            pointsEarned = Math.Min(pointsEarned, (int)(maxPoints * 1.5)); // Cap ved 150% af max point
        }

        var participantAnswer = new ParticipantAnswer
        {
            ParticipantId = submitDto.ParticipantId,
            QuestionId = submitDto.QuestionId,
            AnswerId = submitDto.AnswerId,
            PointsEarned = pointsEarned,
            ResponseTimeMs = submitDto.ResponseTimeMs,
            AnsweredAt = DateTime.UtcNow
        };

        _context.ParticipantAnswers.Add(participantAnswer);

        // Opdater deltagerens totale point
        participant.TotalPoints += pointsEarned;

        await _context.SaveChangesAsync();

        // Tjek om alle deltagere har svaret på dette spørgsmål
        await CheckAndAdvanceQuestion(participant.QuizSession);

        return Ok(new 
        { 
            message = "Svar indsendt",
            participantAnswerId = participantAnswer.Id,
            pointsEarned = pointsEarned,
            isCorrect = answer.IsCorrect,
            totalPoints = participant.TotalPoints
        });
    }

    /// <summary>
    /// Hent leaderboard for en session
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Returnerer deltagere sorteret efter point.</remarks>
    [HttpGet("leaderboard/{sessionId}")]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetLeaderboard(int sessionId)
    {
        var session = await _context.QuizSessions.FindAsync(sessionId);
        if (session == null)
        {
            return NotFound($"Session med ID {sessionId} blev ikke fundet");
        }

        var participants = await _context.Participants
            .Where(p => p.QuizSessionId == sessionId)
            .OrderByDescending(p => p.TotalPoints)
            .ThenBy(p => p.JoinedAt) // Ved samme point, den der join først kommer først
            .ToListAsync();

        var leaderboard = participants
            .Select((p, index) => new LeaderboardEntryDto
            {
                ParticipantId = p.Id,
                Nickname = p.Nickname,
                TotalPoints = p.TotalPoints,
                Rank = index + 1
            })
            .ToList();

        return Ok(leaderboard);
    }

    /// <summary>
    /// Hent alle deltagere i en session
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Returnerer alle deltagere i session.</remarks>
    [HttpGet("session/{sessionId}")]
    public async Task<ActionResult<IEnumerable<ParticipantDto>>> GetParticipants(int sessionId)
    {
        var session = await _context.QuizSessions.FindAsync(sessionId);
        if (session == null)
        {
            return NotFound($"Session med ID {sessionId} blev ikke fundet");
        }

        var participants = await _context.Participants
            .Where(p => p.QuizSessionId == sessionId)
            .OrderByDescending(p => p.TotalPoints)
            .Select(p => new ParticipantDto
            {
                Id = p.Id,
                Nickname = p.Nickname,
                TotalPoints = p.TotalPoints,
                JoinedAt = p.JoinedAt,
                QuizSessionId = p.QuizSessionId
            })
            .ToListAsync();

        return Ok(participants);
    }

    /// <summary>
    /// Hent nuværende spørgsmål for en session (centralt styret)
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Returnerer det nuværende spørgsmål baseret på sessionens CurrentQuestionOrderIndex.</remarks>
    [HttpGet("session/{sessionId}/current-question")]
    public async Task<ActionResult<QuestionWithoutAnswersDto>> GetCurrentQuestion(int sessionId)
    {
        var session = await _context.QuizSessions
            .Include(s => s.Quiz)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            return NotFound($"Session med ID {sessionId} blev ikke fundet");
        }

        if (session.Status != QuizSessionStatus.InProgress)
        {
            return BadRequest($"Session skal være 'InProgress' for at hente spørgsmål. Nuværende status: {session.Status}");
        }

        if (!session.CurrentQuestionOrderIndex.HasValue)
        {
            return NotFound("Ingen aktivt spørgsmål i denne session");
        }

        var question = await _context.Questions
            .Include(q => q.Answers)
            .Where(q => q.QuizId == session.QuizId && q.OrderIndex == session.CurrentQuestionOrderIndex.Value)
            .FirstOrDefaultAsync();

        if (question == null)
        {
            return NotFound($"Spørgsmål med order index {session.CurrentQuestionOrderIndex.Value} blev ikke fundet i denne quiz");
        }

        var questionDto = new QuestionWithoutAnswersDto
        {
            Id = question.Id,
            Text = question.Text,
            TimeLimitSeconds = question.TimeLimitSeconds,
            Points = question.Points,
            OrderIndex = question.OrderIndex,
            Answers = question.Answers
                .OrderBy(a => a.OrderIndex)
                .Select(a => new AnswerDto
                {
                    Id = a.Id,
                    Text = a.Text,
                    IsCorrect = false, // Skjul korrekt svar fra deltagere
                    OrderIndex = a.OrderIndex,
                    CreatedAt = a.CreatedAt
                })
                .ToList()
        };

        return Ok(questionDto);
    }

    /// <summary>
    /// Hent et spørgsmål til en session (deprecated - brug GetCurrentQuestion i stedet)
    /// </summary>
    /// <remarks>Auth: None - Offentlig endpoint. Returnerer spørgsmål uden korrekt svar (for deltagere).</remarks>
    [HttpGet("session/{sessionId}/question/{questionOrderIndex}")]
    public async Task<ActionResult<QuestionWithoutAnswersDto>> GetQuestion(int sessionId, int questionOrderIndex)
    {
        var session = await _context.QuizSessions
            .Include(s => s.Quiz)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            return NotFound($"Session med ID {sessionId} blev ikke fundet");
        }

        // Valider at spørgsmålet matcher sessionens nuværende spørgsmål (centralt styret)
        if (session.Status == QuizSessionStatus.InProgress)
        {
            if (!session.CurrentQuestionOrderIndex.HasValue)
            {
                return BadRequest("Ingen aktivt spørgsmål i denne session. Brug /current-question endpoint i stedet.");
            }

            if (questionOrderIndex != session.CurrentQuestionOrderIndex.Value)
            {
                return BadRequest($"Kan kun hente det nuværende spørgsmål (order index {session.CurrentQuestionOrderIndex.Value}). Du prøvede at hente spørgsmål med order index {questionOrderIndex}. Brug /current-question endpoint i stedet.");
            }
        }

        var question = await _context.Questions
            .Include(q => q.Answers)
            .Where(q => q.QuizId == session.QuizId && q.OrderIndex == questionOrderIndex)
            .FirstOrDefaultAsync();

        if (question == null)
        {
            return NotFound($"Spørgsmål med order index {questionOrderIndex} blev ikke fundet i denne quiz");
        }

        var questionDto = new QuestionWithoutAnswersDto
        {
            Id = question.Id,
            Text = question.Text,
            TimeLimitSeconds = question.TimeLimitSeconds,
            Points = question.Points,
            OrderIndex = question.OrderIndex,
            Answers = question.Answers
                .OrderBy(a => a.OrderIndex)
                .Select(a => new AnswerDto
                {
                    Id = a.Id,
                    Text = a.Text,
                    IsCorrect = false, // Skjul korrekt svar fra deltagere
                    OrderIndex = a.OrderIndex,
                    CreatedAt = a.CreatedAt
                })
                .ToList()
        };

        return Ok(questionDto);
    }

    /// <summary>
    /// Tjek om alle deltagere har svaret på nuværende spørgsmål eller om tiden er udløbet, og gå videre hvis en af dem er opfyldt
    /// </summary>
    private async Task CheckAndAdvanceQuestion(QuizSession session)
    {
        if (!session.CurrentQuestionOrderIndex.HasValue)
        {
            return; // Ingen aktivt spørgsmål
        }

        // Hent alle deltagere i sessionen
        var participants = await _context.Participants
            .Where(p => p.QuizSessionId == session.Id)
            .ToListAsync();

        if (participants.Count == 0)
        {
            return; // Ingen deltagere
        }

        // Hent nuværende spørgsmål
        var currentQuestion = await _context.Questions
            .Where(q => q.QuizId == session.QuizId && q.OrderIndex == session.CurrentQuestionOrderIndex.Value)
            .FirstOrDefaultAsync();

        if (currentQuestion == null)
        {
            return; // Spørgsmål ikke fundet
        }

        // Tjek om tiden er udløbet
        bool timeExpired = false;
        if (session.CurrentQuestionStartedAt.HasValue)
        {
            var elapsed = DateTime.UtcNow - session.CurrentQuestionStartedAt.Value;
            if (elapsed.TotalSeconds >= currentQuestion.TimeLimitSeconds)
            {
                timeExpired = true;
            }
        }

        // Tjek om alle deltagere har svaret på dette spørgsmål
        var answeredCount = await _context.ParticipantAnswers
            .CountAsync(pa => pa.QuestionId == currentQuestion.Id && 
                             participants.Select(p => p.Id).Contains(pa.ParticipantId));

        bool allAnswered = answeredCount >= participants.Count;

        // Gå kun videre hvis alle har svaret ELLER tiden er udløbet
        if (!allAnswered && !timeExpired)
        {
            return; // Vent på flere svar eller timeout
        }

        // Hvis tiden er udløbet og nogle ikke har svaret, giv dem 0 point (ingen svar)
        // Vi opretter ikke ParticipantAnswer for dem - de får bare 0 point implicit
        // Dette gør det nemmere og vi undgår at skulle gøre AnswerId nullable

        // Gå videre til næste spørgsmål
        // Hent alle spørgsmål i quizzen for at finde næste
        var allQuestions = await _context.Questions
            .Where(q => q.QuizId == session.QuizId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();

        var currentQuestionIndex = allQuestions.FindIndex(q => q.Id == currentQuestion.Id);
        
        if (currentQuestionIndex < allQuestions.Count - 1)
        {
            // Der er flere spørgsmål - gå videre til næste
            session.CurrentQuestionOrderIndex = allQuestions[currentQuestionIndex + 1].OrderIndex;
            session.CurrentQuestionStartedAt = DateTime.UtcNow; // Start tid for næste spørgsmål
            await _context.SaveChangesAsync();
        }
        else
        {
            // Dette var sidste spørgsmål - afslut sessionen
            session.Status = QuizSessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            session.CurrentQuestionOrderIndex = null; // Ingen aktivt spørgsmål længere
            session.CurrentQuestionStartedAt = null;
            await _context.SaveChangesAsync();
        }
    }
}

