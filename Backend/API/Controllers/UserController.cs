using API.Data;
using API.DTOs.Auth;
using API.Models;
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

    public UserController(ApplicationDbContext context, ILogger<UserController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Hent alle brugere
    /// </summary>
    /// <remarks>Auth: Admin - Returnerer liste af alle brugere i systemet.</remarks>
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

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Slet bruger
    /// </summary>
    /// <remarks>Auth: Admin - Sletter bruger fra systemet. Admin kan ikke slette sig selv.</remarks>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
        {
            return Unauthorized();
        }

        // Admin kan ikke slette sig selv
        if (currentUserId == id)
        {
            return BadRequest("Du kan ikke slette din egen konto");
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound($"Bruger med ID {id} blev ikke fundet");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

