using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Extensions;
using API.Models;
using Microsoft.IdentityModel.Tokens;

namespace API.Services;

public interface IJwtService
{
    /// <summary>
    /// Generate JWT token - som sendes til brugeren i Authorization header
    /// </summary>
    string GenerateToken(User user, string? authProvider = null);

    /// <summary>
    /// Generate refresh token - som gemmes i databasen, 
    /// hvilket gør det muligt at generere nye tokens uden at brugeren skal logge ind igen.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Get principal from token - som bruges til at håndtere claims i token
    /// Claims er de data der er gemt i token..
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromToken(string token);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateToken(User user, string? authProvider = null)
    {
        var secretKey = _configuration.GetConfigValue("Jwt:SecretKey", "Jwt__SecretKey") 
            ?? throw new InvalidOperationException("JWT SecretKey er ikke konfigureret");
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Sæt auth_provider claim baseret på login metode
        // OldSchool for normal login, Google/GitHub for OAuth login
        var provider = authProvider switch
        {
            "Google" => "Google",
            "GitHub" => "GitHub",
            _ => "OldSchool" // Normal email/password login
        };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("auth_provider", provider) // Custom claim for login metode
        };

        // Issuer er altid H4-MAGS-API (ikke baseret på login metode, da det er et seperat claim)
        var issuer = _configuration.GetConfigValue("Jwt:Issuer", "Jwt__Issuer") ?? "H4-MAGS-API";
        var audience = _configuration.GetConfigValue("Jwt:Audience", "Jwt__Audience") ?? "H4-MAGS-Client";
        var expirationMinutes = int.Parse(_configuration.GetConfigValue("Jwt:ExpirationMinutes", "Jwt__ExpirationMinutes") ?? "60");

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var secretKey = _configuration.GetConfigValue("Jwt:SecretKey", "Jwt__SecretKey") 
                ?? throw new InvalidOperationException("JWT SecretKey er ikke konfigureret");
            
            // Issuer er altid H4-MAGS-API
            var issuer = _configuration.GetConfigValue("Jwt:Issuer", "Jwt__Issuer") ?? "H4-MAGS-API";
            var audience = _configuration.GetConfigValue("Jwt:Audience", "Jwt__Audience") ?? "H4-MAGS-Client";

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer, // Altid H4-MAGS-API
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = false, // Vi validerer kun strukturen, ikke udløbstid
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}

