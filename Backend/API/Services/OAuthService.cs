using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.Services;

/// <summary>
/// Generisk OAuth service der håndterer alle OAuth 2.0 / OpenID Connect providers
/// </summary>
public class OAuthService : IOAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OAuthService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, OAuthProviderConfiguration> _providers;

    public OAuthService(
        ApplicationDbContext context,
        ILogger<OAuthService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _providers = LoadProviderConfigurations();
    }

    /// <summary>
    /// Indlæs OAuth provider konfigurationer fra appsettings
    /// </summary>
    private Dictionary<string, OAuthProviderConfiguration> LoadProviderConfigurations()
    {
        var providers = new Dictionary<string, OAuthProviderConfiguration>();
        
        // Load Google
        var googleConfig = _configuration.GetSection("OAuth:Google");
        if (!string.IsNullOrEmpty(googleConfig["ClientId"]))
        {
            providers["Google"] = new OAuthProviderConfiguration
            {
                ProviderName = "Google",
                ClientId = googleConfig["ClientId"] ?? string.Empty,
                UserInfoEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo",
                Issuer = "https://accounts.google.com",
                ValidateAudience = true,
                UserInfoMapping = new UserInfoMapping
                {
                    IdProperty = "id",
                    EmailProperty = "email",
                    NameProperty = "name",
                    PictureProperty = "picture"
                }
            };
        }

        // Load Microsoft
        var microsoftConfig = _configuration.GetSection("OAuth:Microsoft");
        if (!string.IsNullOrEmpty(microsoftConfig["ClientId"]))
        {
            providers["Microsoft"] = new OAuthProviderConfiguration
            {
                ProviderName = "Microsoft",
                ClientId = microsoftConfig["ClientId"] ?? string.Empty,
                UserInfoEndpoint = "https://graph.microsoft.com/v1.0/me",
                Issuer = "https://login.microsoftonline.com/common/v2.0",
                ValidateAudience = true,
                UserInfoMapping = new UserInfoMapping
                {
                    IdProperty = "id",
                    EmailProperty = "mail", // Microsoft bruger "mail" i stedet for "email"
                    NameProperty = "displayName",
                    PictureProperty = null // Microsoft har ikke picture i standard endpoint (null er OK)
                }
            };
        }

        // Load GitHub
        var githubConfig = _configuration.GetSection("OAuth:GitHub");
        if (!string.IsNullOrEmpty(githubConfig["ClientId"]))
        {
            providers["GitHub"] = new OAuthProviderConfiguration
            {
                ProviderName = "GitHub",
                ClientId = githubConfig["ClientId"] ?? string.Empty,
                ClientSecret = null,
                UserInfoEndpoint = "https://api.github.com/user",
                Issuer = null, // GitHub bruger ikke standard OpenID Connect
                ValidateAudience = false,
                UserInfoMapping = new UserInfoMapping
                {
                    IdProperty = "id",
                    EmailProperty = "email",
                    NameProperty = "name",
                    PictureProperty = "avatar_url"
                }
            };
        }

        return providers;
    }

    public async Task<OAuthUserInfo?> VerifyIdTokenAsync(string idToken, string providerName)
    {
        // For nu, vi fokuserer på access token flow (mere generisk)
        // ID token verification kan tilføjes senere med generisk JWT validation
        _logger.LogWarning("ID token verification ikke endnu implementeret generisk for {Provider}", providerName);
        return null;
    }

    public async Task<OAuthUserInfo?> GetUserInfoFromAccessTokenAsync(string accessToken, string providerName)
    {
        if (!_providers.TryGetValue(providerName, out var providerConfig))
        {
            _logger.LogError("Ukendt OAuth provider: {Provider}", providerName);
            return null;
        }

        try
        {
            _logger.LogInformation("Henter brugerinfo fra {Provider} API...", providerName);
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            
            // GitHub kræver User-Agent header
            if (providerName == "GitHub")
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "H4-MAGS-API");
            }

            var response = await httpClient.GetAsync(providerConfig.UserInfoEndpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("{Provider} API fejl. Status: {StatusCode}, Content: {Content}",
                    providerName, response.StatusCode, errorContent);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            
            // Parse JSON med case-insensitive options
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            var jsonDoc = JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;

            // Map provider-specifikke felter til vores standard format
            var userInfo = new OAuthUserInfo
            {
                ProviderUserId = GetJsonProperty(root, providerConfig.UserInfoMapping.IdProperty) ?? string.Empty,
                Email = GetJsonProperty(root, providerConfig.UserInfoMapping.EmailProperty) ?? string.Empty,
                Name = GetJsonProperty(root, providerConfig.UserInfoMapping.NameProperty),
                Picture = GetJsonProperty(root, providerConfig.UserInfoMapping.PictureProperty)
            };

            if (string.IsNullOrEmpty(userInfo.ProviderUserId) || string.IsNullOrEmpty(userInfo.Email))
            {
                _logger.LogWarning("{Provider} API response mangler påkrævede felter. JSON: {Json}", providerName, json);
                return null;
            }

            _logger.LogInformation("Hentet brugerinfo fra {Provider}: {Email}", providerName, userInfo.Email);
            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved hentning af brugerinfo fra {Provider} API", providerName);
            return null;
        }
    }

    /// <summary>
    /// Hjælpemetode til at hente JSON property med case-insensitive match
    /// </summary>
    private string? GetJsonProperty(JsonElement root, string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return null;

        // Prøv exact match først
        if (root.TryGetProperty(propertyName, out var element))
            return element.GetString();

        // Prøv case-insensitive match
        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                return prop.Value.GetString();
        }

        return null;
    }

    public async Task<User?> GetOrCreateUserFromOAuthAsync(OAuthUserInfo userInfo, string providerName)
    {
        if (string.IsNullOrEmpty(userInfo.Email) || string.IsNullOrEmpty(userInfo.ProviderUserId))
        {
            _logger.LogWarning("{Provider} user info mangler email eller provider user ID", providerName);
            return null;
        }

        var normalizedEmail = userInfo.Email.Trim().ToLowerInvariant();

        // BEST PRACTICE: Tjek først efter eksisterende ExternalIdentity
        var existingIdentity = await _context.ExternalIdentities
            .Include(ei => ei.User)
            .FirstOrDefaultAsync(ei => ei.Provider == providerName && ei.ProviderUserId == userInfo.ProviderUserId);

        if (existingIdentity != null)
        {
            // Eksisterende identitet - opdater info
            existingIdentity.User.LastLoginAt = DateTime.UtcNow;
            existingIdentity.LastLoginAt = DateTime.UtcNow;
            
            if (!string.IsNullOrEmpty(userInfo.Picture) && existingIdentity.User.Picture != userInfo.Picture)
            {
                existingIdentity.User.Picture = userInfo.Picture;
            }
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("Eksisterende {Provider} identitet fundet for bruger: {Email}", providerName, normalizedEmail);
            return existingIdentity.User;
        }

        // Account linking: Tjek om bruger eksisterer med samme email
        var existingUserByEmail = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (existingUserByEmail != null)
        {
            // Link provider til eksisterende bruger
            var newIdentity = new ExternalIdentity
            {
                UserId = existingUserByEmail.Id,
                Provider = providerName,
                ProviderUserId = userInfo.ProviderUserId,
                ProviderEmail = normalizedEmail,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.ExternalIdentities.Add(newIdentity);
            
            existingUserByEmail.LastLoginAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(userInfo.Picture) && existingUserByEmail.Picture != userInfo.Picture)
            {
                existingUserByEmail.Picture = userInfo.Picture;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("{Provider} identitet linket til eksisterende bruger: {Email}", providerName, normalizedEmail);
            return existingUserByEmail;
        }

        // Opret ny bruger
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
            PasswordHash = null, // SSO-only bruger
            Role = UserRole.Student,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            Picture = userInfo.Picture
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // Opret ExternalIdentity
        var externalIdentity = new ExternalIdentity
        {
            UserId = newUser.Id,
            Provider = providerName,
            ProviderUserId = userInfo.ProviderUserId,
            ProviderEmail = normalizedEmail,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        _context.ExternalIdentities.Add(externalIdentity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ny bruger oprettet fra {Provider}: {Email}, ID: {Id}", providerName, normalizedEmail, newUser.Id);
        return newUser;
    }
}

