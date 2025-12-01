using API.Data;
using API.DTOs.Auth;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
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
    /// GitHub OAuth callback endpoint
    /// </summary>
    /// <remarks>
    /// Auth: Anonymous - Modtager authorization code fra GitHub og logger brugeren ind.
    /// Dette endpoint kaldes når GitHub redirecter efter bruger har godkendt app.
    /// </remarks>
    [HttpGet("github/callback")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GitHubCallback([FromQuery] string? code, [FromQuery] string? error, [FromQuery] string? state)
    {
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("GitHub OAuth fejl: {Error}", error);
            return BadRequest(new { message = $"GitHub OAuth fejl: {error}" });
        }

        if (string.IsNullOrEmpty(code))
        {
            _logger.LogWarning("GitHub OAuth callback mangler authorization code");
            return BadRequest(new { message = "Authorization code mangler" });
        }

        // Hent redirect URI (skal matche GitHub OAuth App konfiguration PRÆCIST - case-sensitive!)
        // Controller route er "api/auth" (lowercase for konsistens)
        // Brug HTTPS hvis request kommer gennem proxy (X-Forwarded-Proto) eller i production
        var scheme = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() 
                     ?? Request.Headers["X-Forwarded-Scheme"].FirstOrDefault()
                     ?? (Request.IsHttps ? "https" : Request.Scheme);
        
        // Force HTTPS i production (når vi kører på mercantec.tech domain)
        // GitHub OAuth App callback URL er altid HTTPS
        if (Request.Host.Host.Contains("mercantec.tech"))
        {
            scheme = "https";
        }
        
        var redirectUri = $"{scheme}://{Request.Host}/api/auth/github/callback";
        _logger.LogInformation("GitHub OAuth callback modtaget. Code: {Code}, RedirectUri: {RedirectUri}", 
            code.Substring(0, Math.Min(20, code.Length)) + "...", redirectUri);

        // Exchange authorization code for access token
        var accessToken = await _oauthService.ExchangeGitHubCodeForTokenAsync(code, redirectUri);
        
        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("Kunne ikke exchange GitHub authorization code for access token");
            return Unauthorized(new { message = "Kunne ikke hente access token fra GitHub" });
        }

        // Hent brugerinfo fra GitHub
        var userInfo = await _oauthService.GetUserInfoFromAccessTokenAsync(accessToken, "GitHub");
        
        if (userInfo == null)
        {
            _logger.LogWarning("Kunne ikke hente brugerinfo fra GitHub");
            return Unauthorized(new { message = "Kunne ikke hente brugerinfo fra GitHub" });
        }

        // Hent eller opret bruger
        var user = await _oauthService.GetOrCreateUserFromOAuthAsync(userInfo, "GitHub");
        
        if (user == null)
        {
            _logger.LogWarning("Kunne ikke hente eller oprette bruger fra GitHub");
            return Unauthorized(new { message = "Kunne ikke hente eller oprette bruger fra GitHub" });
        }

        _logger.LogInformation("GitHub OAuth login succesfuld for bruger: {Email}", user.Email);
        
        // Generer auth response direkte (uden at wrappe i ActionResult)
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

        var authResponseDto = new AuthResponseDto
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

        // Serialiser auth response til JSON med camelCase
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        var jsonString = System.Text.Json.JsonSerializer.Serialize(authResponseDto, jsonOptions);
        
        // Escape JSON string for JavaScript (escape quotes and backslashes)
        var escapedJson = jsonString.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        
        // Returner HTML page der sender token til Flutter app via postMessage
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>GitHub Login Success</title>
    <meta charset=""utf-8"">
    <script>
        (function() {{
            try {{
                // Parse JSON fra backend (som escaped string)
                var jsonString = '{escapedJson}';
                var authData = JSON.parse(jsonString);
                
                console.log('GitHub OAuth callback: Parsed auth data', authData);
                
                // Send til Flutter app
                if (window.opener && !window.opener.closed) {{
                    window.opener.postMessage({{
                        type: 'github_oauth_success',
                        data: authData
                    }}, '*');
                    
                    console.log('GitHub OAuth success message sent to opener');
                    
                    // Luk popup efter kort delay
                    setTimeout(function() {{
                        window.close();
                    }}, 500);
                }} else {{
                    console.error('Window opener is null or closed');
                    document.body.innerHTML = '<p>Error: Could not communicate with parent window</p>';
                    setTimeout(function() {{
                        window.close();
                    }}, 3000);
                }}
            }} catch (error) {{
                console.error('Error in GitHub OAuth callback:', error);
                document.body.innerHTML = '<p>Error: ' + error.message + '</p><pre>' + error.stack + '</pre>';
            }}
        }})();
    </script>
</head>
<body>
    <p>Login succesfuld! Du kan lukke dette vindue.</p>
</body>
</html>";
        
        return Content(html, "text/html");
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

    /// <summary>
    /// Opdater password for nuværende bruger
    /// </summary>
    /// <remarks>
    /// Auth: Authenticated - Opdaterer password for den authentificerede bruger.
    /// Virker både for OAuth brugere (tilføjer password) og normale brugere (opdaterer password).
    /// </remarks>
    [HttpPost("update-password")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        var success = await _authService.AddPasswordToUserAsync(userId, dto.NewPassword);
        
        if (!success)
        {
            return BadRequest(new { message = "Kunne ikke opdatere password" });
        }

        _logger.LogInformation("Password opdateret for bruger ID: {UserId}", userId);
        return Ok(new { message = "Password opdateret succesfuldt" });
    }
}

