using API.Models;

namespace API.Services;

/// <summary>
/// Generisk OAuth service der håndterer alle OAuth 2.0 / OpenID Connect providers
/// </summary>
public interface IOAuthService
{
    /// <summary>
    /// Verificer ID token fra en OAuth provider
    /// </summary>
    Task<OAuthUserInfo?> VerifyIdTokenAsync(string idToken, string providerName);
    
    /// <summary>
    /// Hent brugerinfo fra provider ved hjælp af access token
    /// </summary>
    Task<OAuthUserInfo?> GetUserInfoFromAccessTokenAsync(string accessToken, string providerName);
    
    /// <summary>
    /// Hent eller opret bruger fra OAuth login
    /// </summary>
    Task<User?> GetOrCreateUserFromOAuthAsync(OAuthUserInfo userInfo, string providerName);
    
    /// <summary>
    /// Exchange GitHub authorization code for access token
    /// </summary>
    Task<string?> ExchangeGitHubCodeForTokenAsync(string code, string redirectUri);
}

/// <summary>
/// Standardiseret brugerinfo fra OAuth provider
/// </summary>
public class OAuthUserInfo
{
    /// <summary>
    /// Provider's unikke bruger ID (f.eks. Google Sub, Microsoft oid)
    /// </summary>
    public string ProviderUserId { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Picture { get; set; }
}

