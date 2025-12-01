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
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IOAuthService _oauthService;

    public AuthController(
        IAuthService authService,
        IJwtService jwtService,
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        IOAuthService oauthService)
    {
        _authService = authService;
        _jwtService = jwtService;
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _oauthService = oauthService;
    }

    /// <summary>
    /// Registrer en ny bruger
    /// </summary>
    /// <remarks>Auth: Anonymous - Alle kan registrere sig. Nye brugere får automatisk Student rolle.</remarks>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
    {
        // Alle nye brugere får automatisk Student rolle
        var user = await _authService.RegisterAsync(
            registerDto.Username,
            registerDto.Email,
            registerDto.Password,
            UserRole.Student);

        if (user == null)
        {
            return BadRequest(new { message = "Brugernavn eller email er allerede i brug" });
        }

        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Gem refresh token i database
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        var response = new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            Expires = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60")),
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                Picture = user.Picture
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Login
    /// </summary>
    /// <remarks>Auth: Anonymous - Returnerer JWT token og refresh token ved succesfuld login.</remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
    {
        var user = await _authService.LoginAsync(loginDto.UsernameOrEmail, loginDto.Password);

        if (user == null)
        {
            return Unauthorized(new { message = "Forkert brugernavn eller password" });
        }

        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Gem refresh token i database
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        var response = new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            Expires = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60")),
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                Picture = user.Picture
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Refresh JWT token med refresh token
    /// </summary>
    /// <remarks>Auth: Anonymous - Genererer nye tokens baseret på gyldig refresh token.</remarks>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenDto refreshTokenDto)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshTokenDto.RefreshToken);

        if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Ugyldig eller udløbet refresh token" });
        }

        // Generer nye tokens
        var newToken = _jwtService.GenerateToken(refreshToken.User);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // Revoke gamle refresh token
        refreshToken.IsRevoked = true;

        // Tilføj ny refresh token
        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            UserId = refreshToken.User.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(newRefreshTokenEntity);
        await _context.SaveChangesAsync();

        var response = new AuthResponseDto
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            Expires = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60")),
            User = new UserDto
            {
                Id = refreshToken.User.Id,
                Username = refreshToken.User.Username,
                Email = refreshToken.User.Email,
                Role = refreshToken.User.Role.ToString(),
                CreatedAt = refreshToken.User.CreatedAt
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Logout - revoke refresh token
    /// </summary>
    /// <remarks>Auth: Authenticated - Revoker refresh token for at logge brugeren ud.</remarks>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(RefreshTokenDto refreshTokenDto)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshTokenDto.RefreshToken);

        if (refreshToken != null)
        {
            refreshToken.IsRevoked = true;
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Logout successful" });
    }

    /// <summary>
    /// Generisk OAuth login - Virker med alle providers (Google, Microsoft, GitHub, etc.)
    /// </summary>
    /// <remarks>
    /// Auth: Anonymous - Login med OAuth access token fra hvilken som helst provider.
    /// Understøtter: Google, Microsoft, GitHub, Facebook (konfigureret via appsettings.json)
    /// </remarks>
    [HttpPost("oauth-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<AuthResponseDto>> OAuthLogin([FromBody] OAuthLoginDto? dto)
    {
        if (dto == null || string.IsNullOrEmpty(dto.Provider) || string.IsNullOrEmpty(dto.AccessToken))
        {
            return BadRequest(new { message = "Provider og AccessToken er påkrævet" });
        }

        _logger.LogInformation("OAuth login forsøg med provider: {Provider}", dto.Provider);

        // Hent brugerinfo fra provider
        var userInfo = await _oauthService.GetUserInfoFromAccessTokenAsync(dto.AccessToken, dto.Provider);
        
        if (userInfo == null)
        {
            _logger.LogWarning("Kunne ikke hente brugerinfo fra {Provider}", dto.Provider);
            return Unauthorized(new { message = $"Kunne ikke hente brugerinfo fra {dto.Provider}" });
        }

        // Hent eller opret bruger
        var user = await _oauthService.GetOrCreateUserFromOAuthAsync(userInfo, dto.Provider);
        
        if (user == null)
        {
            _logger.LogWarning("Kunne ikke hente eller oprette bruger fra {Provider}", dto.Provider);
            return Unauthorized(new { message = $"Kunne ikke hente eller oprette bruger fra {dto.Provider}" });
        }

        _logger.LogInformation("OAuth login succesfuld med {Provider} for bruger: {Email}", dto.Provider, user.Email);
        return await GenerateAuthResponse(user);
    }

    /// <summary>
    /// Helper metode til at generere auth response
    /// </summary>
    private async Task<ActionResult<AuthResponseDto>> GenerateAuthResponse(User user)
    {
        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Gem refresh token i database
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        var response = new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            Expires = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60")),
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                Picture = user.Picture
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Hent nuværende bruger info
    /// </summary>
    /// <remarks>Auth: Authenticated - Returnerer info om den authentificerede bruger.</remarks>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
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
}

