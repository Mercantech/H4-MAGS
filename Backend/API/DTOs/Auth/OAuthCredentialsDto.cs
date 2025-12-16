namespace API.DTOs.Auth;

/// <summary>
/// DTO for OAuth credentials response
/// </summary>
public class OAuthCredentialsDto
{
    public GoogleCredentialsDto Google { get; set; } = new();
    public GitHubCredentialsDto GitHub { get; set; } = new();
    public ApiInfoDto Api { get; set; } = new();
}

public class GoogleCredentialsDto
{
    public string ClientId { get; set; } = string.Empty;
}

public class GitHubCredentialsDto
{
    public string ClientId { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string Scope { get; set; } = "user:email";
}

public class ApiInfoDto
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ProductionBaseUrl { get; set; } = string.Empty;
    public string DevelopmentBaseUrl { get; set; } = string.Empty;
}

