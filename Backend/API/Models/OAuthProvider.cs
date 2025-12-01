namespace API.Models;

/// <summary>
/// OAuth Provider typer der understøttes
/// </summary>
public enum OAuthProviderType
{
    Google,
    Microsoft,
    GitHub,
    Facebook
}

/// <summary>
/// Konfiguration for en OAuth provider
/// </summary>
public class OAuthProviderConfiguration
{
    public string ProviderName { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string UserInfoEndpoint { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public bool ValidateAudience { get; set; } = true;
    
    /// <summary>
    /// JSON property navne for at parse UserInfo response
    /// </summary>
    public UserInfoMapping UserInfoMapping { get; set; } = new();
}

/// <summary>
/// Mapping af JSON property navne fra provider til vores standard felter
/// </summary>
public class UserInfoMapping
{
    public string IdProperty { get; set; } = "sub"; // Standard OpenID Connect claim
    public string EmailProperty { get; set; } = "email";
    public string NameProperty { get; set; } = "name";
    public string PictureProperty { get; set; } = "picture";
}

