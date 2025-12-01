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
    string GenerateToken(User user, string? provider = null);
    string GenerateRefreshToken();
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

    public string GenerateToken(User user, string? provider = null)
    {
        var secretKey = _configuration.GetConfigValue("Jwt:SecretKey", "Jwt__SecretKey") 
            ?? throw new InvalidOperationException("JWT SecretKey er ikke konfigureret");
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Sæt issuer baseret på login metode
        // H4-MAGS-API for normal login, Google/GitHub for OAuth login
        var issuer = provider switch
        {
            "Google" => "Google",
            "GitHub" => "GitHub",
            _ => _configuration.GetConfigValue("Jwt:Issuer", "Jwt__Issuer") ?? "H4-MAGS-API"
        };
        
        var audience = _configuration.GetConfigValue("Jwt:Audience", "Jwt__Audience") ?? "H4-MAGS-Client";
        var expirationMinutes = int.Parse(_configuration.GetConfigValue("Jwt:ExpirationMinutes", "Jwt__ExpirationMinutes") ?? "60");

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes + 120),
            // +120 to account for local danish time difference
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
            
            // Accepter alle mulige issuers (H4-MAGS-API, Google, GitHub)
            var validIssuers = new[] 
            { 
                _configuration.GetConfigValue("Jwt:Issuer", "Jwt__Issuer") ?? "H4-MAGS-API",
                "H4-MAGS-API",
                "Google",
                "GitHub"
            };
            
            var audience = _configuration.GetConfigValue("Jwt:Audience", "Jwt__Audience") ?? "H4-MAGS-Client";

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuers = validIssuers, // Accepter alle mulige issuers
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

