using API.Data;
using API.DTOs.Auth;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserController> _logger;
    private readonly IAuthService _authService;

    public UserController(
        ApplicationDbContext context, 
        ILogger<UserController> logger,
        IAuthService authService)
    {
        _context = context;
        _logger = logger;
        _authService = authService;
    }

    /// <summary>
    /// Hent alle brugere (offentlig - minimal data)
    /// </summary>
    /// <remarks>
    /// Auth: Anonymous - Returnerer liste af alle brugere med minimal information (Id, Username, Picture).
    /// Brug dette endpoint når du kun har brug for grundlæggende brugerinformation.
    /// </remarks>
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<PublicUserDto>>> GetPublicUsers()
    {
        var users = await _context.Users
            .OrderBy(u => u.Username)
            .Select(u => new PublicUserDto
            {
                Id = u.Id,
                Username = u.Username,
                Picture = u.Picture
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// Hent alle brugere (offentlig - basis data)
    /// </summary>
    /// <remarks>
    /// Auth: Anonymous - Returnerer liste af alle brugere med basis information (Id, Username, Role, Picture).
    /// Email og CreatedAt er bevidst udeladt for at beskytte brugerdata.
    /// </remarks>
    [HttpGet("basic")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BasicUserDto>>> GetBasicUsers()
    {
        var users = await _context.Users
            .OrderBy(u => u.Username)
            .Select(u => new BasicUserDto
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role.ToString(),
                Picture = u.Picture
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// Hent alle brugere (fuld data)
    /// </summary>
    /// <remarks>Auth: Admin - Returnerer liste af alle brugere i systemet med fuld information.</remarks>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _context.Users
            .OrderBy(u => u.Username)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role.ToString(),
                CreatedAt = u.CreatedAt,
                Picture = u.Picture
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// Hent specifik bruger
    /// </summary>
    /// <remarks>Auth: Authenticated - Brugere kan se egen profil, Admin kan se alle.</remarks>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
        {
            return Unauthorized();
        }

        // Brugere kan kun se deres egen profil, medmindre de er admin
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && currentUserId != id)
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound($"Bruger med ID {id} blev ikke fundet");
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            Picture = user.Picture
        };

        return Ok(userDto);
    }

    /// <summary>
    /// Hent bruger profilbillede (proxy for Google profile picture)
    /// </summary>
    /// <remarks>
    /// Auth: Anonymous - Proxy endpoint for at undgå CORS problemer ved direkte loading af Google billeder.
    /// Billeder er offentlige, så authentication er ikke nødvendig.
    /// </remarks>
    [HttpGet("{id}/picture")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserPicture(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null || string.IsNullOrEmpty(user.Picture))
        {
            return NotFound();
        }

        try
        {
            // Hent billede fra Google (proxy for at undgå CORS)
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(user.Picture);
            
            if (!response.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

            // Cache billedet i 1 time
            Response.Headers.Add("Cache-Control", "public, max-age=3600");
            
            // CORS headers for at tillade Flutter Web at hente billedet
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.Headers.Add("Access-Control-Allow-Methods", "GET");
            
            return File(imageBytes, contentType);
        }
        catch
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Opdater bruger
    /// </summary>
    /// <remarks>Auth: Authenticated - Brugere kan opdatere egen profil, Admin kan opdatere alle og ændre roller.</remarks>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserDto updateDto)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound($"Bruger med ID {id} blev ikke fundet");
        }

        // Brugere kan kun opdatere deres egen profil, medmindre de er admin
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && currentUserId != id)
        {
            return Forbid();
        }

        // Admin kan ændre rolle, normale brugere kan ikke
        if (updateDto.Role.HasValue && isAdmin)
        {
            user.Role = updateDto.Role.Value;
        }

        if (!string.IsNullOrWhiteSpace(updateDto.Username))
        {
            // Normaliser til lowercase for case-insensitive sammenligning
            var normalizedUsername = updateDto.Username.Trim().ToLowerInvariant();
            
            // Tjek om nyt brugernavn allerede eksisterer (case-insensitive)
            if (await _context.Users.AnyAsync(u => u.Username == normalizedUsername && u.Id != id))
            {
                return BadRequest("Brugernavn er allerede taget");
            }
            user.Username = normalizedUsername;
        }

        if (!string.IsNullOrWhiteSpace(updateDto.Email))
        {
            // Normaliser til lowercase for case-insensitive sammenligning
            var normalizedEmail = updateDto.Email.Trim().ToLowerInvariant();
            
            // Tjek om ny email allerede eksisterer (case-insensitive)
            if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail && u.Id != id))
            {
                return BadRequest("Email er allerede i brug");
            }
            user.Email = normalizedEmail;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Slet egen bruger (selv-sletning)
    /// </summary>
    /// <remarks>
    /// Auth: Authenticated - Brugere kan slette deres egen konto.
    /// - Brugere med password skal bekræfte med password
    /// - SSO-only brugere kan slette uden password bekræftelse
    /// </remarks>
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteCurrentUser([FromBody] DeleteUserDto? deleteDto = null)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(currentUserId);
        if (user == null)
        {
            return NotFound("Bruger blev ikke fundet");
        }

        // SSO-only brugere har ikke password, så de kan slette uden bekræftelse
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            if (deleteDto == null || string.IsNullOrWhiteSpace(deleteDto.Password))
            {
                return BadRequest("Password bekræftelse er påkrævet for at slette din konto");
            }

            // Verificer password
            if (!await _authService.VerifyPasswordAsync(deleteDto.Password, user.PasswordHash))
            {
                return BadRequest("Forkert password");
            }
        }

        // Slet brugeren (cascade delete håndterer relateret data automatisk)
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Bruger {UserId} slettede sin egen konto", currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Slet bruger (Admin)
    /// </summary>
    /// <remarks>
    /// Auth: Admin - Sletter bruger fra systemet. Admin kan ikke slette sig selv (brug DELETE /api/user/me i stedet).
    /// </remarks>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
        {
            return Unauthorized();
        }

        // Admin kan ikke slette sig selv (brug selv-sletning i stedet)
        if (currentUserId == id)
        {
            return BadRequest("Admin kan ikke slette sig selv via admin endpoint. Brug DELETE /api/user/me i stedet.");
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound($"Bruger med ID {id} blev ikke fundet");
        }

        // Slet brugeren (cascade delete håndterer relateret data automatisk)
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Bruger {UserId} blev slettet af admin {AdminUserId}", id, currentUserId);

        return NoContent();
    }
}

