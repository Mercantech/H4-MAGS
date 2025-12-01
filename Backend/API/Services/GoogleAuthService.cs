using Google.Apis.Auth;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface IGoogleAuthService
{
    Task<GoogleJsonWebSignature.Payload?> VerifyGoogleTokenAsync(string idToken);
    Task<User?> GetOrCreateUserFromGoogleAsync(GoogleJsonWebSignature.Payload payload);
    Task<User?> GetOrCreateUserFromAccessTokenAsync(string accessToken);
}

public class GoogleAuthService : IGoogleAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GoogleAuthService> _logger;
    private readonly IConfiguration _configuration;

    public GoogleAuthService(
        ApplicationDbContext context, 
        ILogger<GoogleAuthService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<GoogleJsonWebSignature.Payload?> VerifyGoogleTokenAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings();
            
            // Valider audience hvis Google Client ID er konfigureret
            var googleClientId = _configuration["Google:ClientId"];
            if (!string.IsNullOrEmpty(googleClientId))
            {
                settings.Audience = new[] { googleClientId };
                _logger.LogDebug("Validerer Google token med audience: {ClientId}", googleClientId);
            }
            else
            {
                _logger.LogWarning("Google Client ID er ikke konfigureret - token valideres uden audience check");
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved verifikation af Google token");
            return null;
        }
    }

    public async Task<User?> GetOrCreateUserFromGoogleAsync(GoogleJsonWebSignature.Payload payload)
    {
        // Normaliser email til lowercase
        var normalizedEmail = payload.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrEmpty(normalizedEmail))
        {
            _logger.LogWarning("Google payload mangler email");
            return null;
        }

        // Tjek om bruger allerede eksisterer
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (existingUser != null)
        {
            // Opdater last login og picture
            existingUser.LastLoginAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(payload.Picture) && existingUser.Picture != payload.Picture)
            {
                existingUser.Picture = payload.Picture;
            }
            await _context.SaveChangesAsync();
            return existingUser;
        }

        // Opret ny bruger fra Google info
        // Brug email som username hvis name ikke er tilgængelig
        var username = payload.Name?.Trim().ToLowerInvariant() 
            ?? payload.Email?.Split('@')[0].ToLowerInvariant() 
            ?? $"user_{Guid.NewGuid().ToString("N")[..8]}";

        // Tjek om username allerede er taget
        var usernameBase = username;
        var counter = 1;
        while (await _context.Users.AnyAsync(u => u.Username == username))
        {
            username = $"{usernameBase}{counter}";
            counter++;
        }

        var newUser = new User
        {
            Username = username,
            Email = normalizedEmail,
            PasswordHash = string.Empty, // Google brugere har ikke password
            Role = UserRole.Student, // Default rolle
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            Picture = payload.Picture // Gem Google profile picture
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ny bruger oprettet fra Google: {Email}", normalizedEmail);

        return newUser;
    }

    /// <summary>
    /// Hent eller opret bruger fra Google access token
    /// 
    /// Bruger access_token til at hente brugerinfo fra Google API
    /// i stedet for at verificere idToken. Dette er en workaround
    /// for Flutter Web hvor idToken ikke altid er tilgængelig.
    /// </summary>
    public async Task<User?> GetOrCreateUserFromAccessTokenAsync(string accessToken)
    {
        try
        {
            _logger.LogInformation("🔍 [DEBUG] Modtog access_token, længde: {Length}", accessToken?.Length ?? 0);
            
            // Hent brugerinfo fra Google API ved hjælp af access_token
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            _logger.LogInformation("📤 [DEBUG] Sender request til Google UserInfo API...");
            var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            
            _logger.LogInformation("📥 [DEBUG] Google API response status: {StatusCode}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ [DEBUG] Google API fejl. Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, errorContent);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("✅ [DEBUG] Modtog JSON fra Google: {Json}", json);
            
            // Brug case-insensitive JSON options for at matche Google's lowercase property names
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var userInfo = System.Text.Json.JsonSerializer.Deserialize<GoogleUserInfo>(json, jsonOptions);

            if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
            {
                _logger.LogWarning("⚠️ [DEBUG] Google API response mangler email. JSON: {Json}", json);
                return null;
            }
            
            _logger.LogInformation("✅ [DEBUG] Parsed user info - Email: {Email}, Name: {Name}", 
                userInfo.Email, userInfo.Name);

            // Normaliser email til lowercase
            var normalizedEmail = userInfo.Email.Trim().ToLowerInvariant();

            // Tjek om bruger allerede eksisterer
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (existingUser != null)
            {
                // Opdater last login og picture
                existingUser.LastLoginAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(userInfo.Picture) && existingUser.Picture != userInfo.Picture)
                {
                    existingUser.Picture = userInfo.Picture;
                }
                await _context.SaveChangesAsync();
                return existingUser;
            }

            // Opret ny bruger fra Google info
            var username = userInfo.Name?.Trim().ToLowerInvariant() 
                ?? userInfo.Email.Split('@')[0].ToLowerInvariant() 
                ?? $"user_{Guid.NewGuid().ToString("N")[..8]}";

            // Tjek om username allerede er taget
            var usernameBase = username;
            var counter = 1;
            while (await _context.Users.AnyAsync(u => u.Username == username))
            {
                username = $"{usernameBase}{counter}";
                counter++;
            }

            var newUser = new User
            {
                Username = username,
                Email = normalizedEmail,
                PasswordHash = string.Empty, // Google brugere har ikke password
                Role = UserRole.Student, // Default rolle
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                Picture = userInfo.Picture // Gem Google profile picture
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ [DEBUG] Ny bruger oprettet fra Google access_token: {Email}, ID: {Id}, Username: {Username}", 
                normalizedEmail, newUser.Id, newUser.Username);

            return newUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved hentning af brugerinfo fra Google API");
            return null;
        }
    }

    /// <summary>
    /// Helper class til at deserialisere Google UserInfo API response
    /// </summary>
    private class GoogleUserInfo
    {
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? Picture { get; set; }
    }
}

