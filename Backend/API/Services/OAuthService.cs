using API.Data;
using API.Extensions;
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
    /// Indlæs OAuth provider konfigurationer fra environment variabler eller appsettings
    /// </summary>
    private Dictionary<string, OAuthProviderConfiguration> LoadProviderConfigurations()
    {
        var providers = new Dictionary<string, OAuthProviderConfiguration>();
        
        // Load Google
        var googleClientId = _configuration.GetConfigValue("OAuth:Google:ClientId", "OAuth__Google__ClientId");
        if (!string.IsNullOrEmpty(googleClientId))
        {
            providers["Google"] = new OAuthProviderConfiguration
            {
                ProviderName = "Google",
                ClientId = googleClientId,
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
        var microsoftClientId = _configuration.GetConfigValue("OAuth:Microsoft:ClientId", "OAuth__Microsoft__ClientId");
        if (!string.IsNullOrEmpty(microsoftClientId))
        {
            providers["Microsoft"] = new OAuthProviderConfiguration
            {
                ProviderName = "Microsoft",
                ClientId = microsoftClientId,
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
        var githubClientId = _configuration.GetConfigValue("OAuth:GitHub:ClientId", "OAuth__GitHub__ClientId");
        var githubClientSecret = _configuration.GetConfigValue("OAuth:GitHub:ClientSecret", "OAuth__GitHub__ClientSecret");
        if (!string.IsNullOrEmpty(githubClientId))
        {
            providers["GitHub"] = new OAuthProviderConfiguration
            {
                ProviderName = "GitHub",
                ClientId = githubClientId,
                ClientSecret = githubClientSecret, // Til OAuth flow
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
                Email = GetJsonProperty(root, providerConfig.UserInfoMapping.EmailProperty),
                Name = GetJsonProperty(root, providerConfig.UserInfoMapping.NameProperty),
                Picture = GetJsonProperty(root, providerConfig.UserInfoMapping.PictureProperty)
            };

            // GitHub specifik: Hent email fra /user/emails hvis email er null
            if (providerName == "GitHub" && string.IsNullOrEmpty(userInfo.Email))
            {
                _logger.LogInformation("GitHub email er null, henter fra /user/emails endpoint...");
                userInfo.Email = await GetGitHubEmailAsync(httpClient, accessToken);
            }

            if (string.IsNullOrEmpty(userInfo.ProviderUserId) || string.IsNullOrEmpty(userInfo.Email))
            {
                _logger.LogWarning("{Provider} API response mangler påkrævede felter. ProviderUserId: {Id}, Email: {Email}, JSON: {Json}", 
                    providerName, userInfo.ProviderUserId, userInfo.Email ?? "null", json);
                return null;
            }
            
            // Sæt email til empty string hvis null (for at undgå null reference)
            if (userInfo.Email == null)
            {
                userInfo.Email = string.Empty;
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
    /// Håndterer både string og number types (f.eks. GitHub's id er et nummer)
    /// </summary>
    private string? GetJsonProperty(JsonElement root, string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return null;

        JsonElement? element = null;

        // Prøv exact match først
        if (root.TryGetProperty(propertyName, out var exactElement))
        {
            element = exactElement;
        }
        else
        {
            // Prøv case-insensitive match
            foreach (var prop in root.EnumerateObject())
            {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    element = prop.Value;
                    break;
                }
            }
        }

        if (!element.HasValue)
            return null;

        var value = element.Value;

        // Håndter forskellige JSON types
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(), // Konverter nummer til string
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => value.GetRawText() // Fallback for andre typer
        };
    }

    /// <summary>
    /// Hent GitHub email fra /user/emails endpoint
    /// 
    /// GitHub's /user endpoint returnerer ikke altid email (hvis bruger har skjult det).
    /// Vi skal hente fra /user/emails i stedet.
    /// </summary>
    private async Task<string?> GetGitHubEmailAsync(HttpClient httpClient, string accessToken)
    {
        try
        {
            // GitHub kræver User-Agent header
            // httpClient har allerede User-Agent sat fra GetUserInfoFromAccessTokenAsync
            // Men vi sikrer at det er sat hvis metoden kaldes direkte
            if (!httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "H4-MAGS-API");
            }
            
            var emailsResponse = await httpClient.GetAsync("https://api.github.com/user/emails");
            
            if (!emailsResponse.IsSuccessStatusCode)
            {
                var errorContent = await emailsResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("GitHub /user/emails fejlede. Status: {StatusCode}, Content: {Content}", 
                    emailsResponse.StatusCode, errorContent);
                return null;
            }

            var emailsJson = await emailsResponse.Content.ReadAsStringAsync();
            var emailsDoc = JsonDocument.Parse(emailsJson);
            var emailsArray = emailsDoc.RootElement;

            // Find primær email eller første verified email
            foreach (var emailObj in emailsArray.EnumerateArray())
            {
                var email = GetJsonProperty(emailObj, "email");
                var isPrimary = emailObj.TryGetProperty("primary", out var primaryElement) && primaryElement.GetBoolean();
                var isVerified = emailObj.TryGetProperty("verified", out var verifiedElement) && verifiedElement.GetBoolean();

                // Prioritér primær email
                if (isPrimary && !string.IsNullOrEmpty(email))
                {
                    _logger.LogInformation("Fundet primær GitHub email: {Email}", email);
                    return email;
                }
            }

            // Hvis ingen primær, find første verified
            foreach (var emailObj in emailsArray.EnumerateArray())
            {
                var email = GetJsonProperty(emailObj, "email");
                var isVerified = emailObj.TryGetProperty("verified", out var verifiedElement) && verifiedElement.GetBoolean();

                if (isVerified && !string.IsNullOrEmpty(email))
                {
                    _logger.LogInformation("Fundet verified GitHub email: {Email}", email);
                    return email;
                }
            }

            // Hvis ingen verified, brug første email
            foreach (var emailObj in emailsArray.EnumerateArray())
            {
                var email = GetJsonProperty(emailObj, "email");
                if (!string.IsNullOrEmpty(email))
                {
                    _logger.LogInformation("Fundet GitHub email: {Email}", email);
                    return email;
                }
            }

            _logger.LogWarning("Ingen email fundet i GitHub /user/emails response");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved hentning af GitHub email fra /user/emails");
            return null;
        }
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

    /// <summary>
    /// Exchange GitHub authorization code for access token
    /// </summary>
    public async Task<string?> ExchangeGitHubCodeForTokenAsync(string code, string redirectUri)
    {
        if (!_providers.TryGetValue("GitHub", out var githubConfig))
        {
            _logger.LogError("GitHub provider ikke konfigureret");
            return null;
        }

        if (string.IsNullOrEmpty(githubConfig.ClientSecret))
        {
            _logger.LogError("GitHub ClientSecret ikke konfigureret i appsettings.json");
            return null;
        }

        try
        {
            _logger.LogInformation("Exchanging GitHub authorization code for access token...");
            
            using var httpClient = new HttpClient();
            var requestBody = new Dictionary<string, string>
            {
                { "client_id", githubConfig.ClientId },
                { "client_secret", githubConfig.ClientSecret },
                { "code", code },
                { "redirect_uri", redirectUri }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
            {
                Content = new FormUrlEncodedContent(requestBody),
                Headers = { { "Accept", "application/json" } }
            };

            var response = await httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("GitHub token exchange fejlede. Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, errorContent);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("access_token", out var accessTokenElement))
            {
                var accessToken = accessTokenElement.GetString();
                _logger.LogInformation("GitHub access token hentet succesfuldt");
                return accessToken;
            }

            if (root.TryGetProperty("error", out var errorElement))
            {
                var error = errorElement.GetString();
                var errorDescription = root.TryGetProperty("error_description", out var desc) 
                    ? desc.GetString() 
                    : "Unknown error";
                _logger.LogError("GitHub token exchange fejl: {Error}, Description: {Description}", 
                    error, errorDescription);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uventet fejl ved GitHub token exchange");
            return null;
        }
    }
}

