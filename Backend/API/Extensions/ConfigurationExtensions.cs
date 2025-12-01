using Microsoft.Extensions.Configuration;

namespace API.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Henter konfigurationsværdi fra environment variabel først, derefter fra IConfiguration
    /// Environment variabel navne bruger dobbelt underscore (__) for nested properties
    /// </summary>
    public static string? GetConfigValue(this IConfiguration configuration, string configKey, string? envVarName = null)
    {
        // Hvis envVarName ikke er angivet, konverter configKey til environment variabel format
        // F.eks. "Jwt:SecretKey" -> "Jwt__SecretKey"
        var envKey = envVarName ?? configKey.Replace(":", "__");
        
        // Prøv environment variabel først
        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }
        
        // Fallback til configuration
        return configuration[configKey];
    }
    
    /// <summary>
    /// Henter connection string fra environment variabel først, derefter fra IConfiguration
    /// </summary>
    public static string? GetConnectionStringWithEnv(this IConfiguration configuration, string name)
    {
        // Prøv environment variabel først (ConnectionStrings__DefaultConnection)
        var envKey = $"ConnectionStrings__{name}";
        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }
        
        // Fallback til configuration
        return configuration.GetConnectionString(name);
    }
}

