using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace API.Services;

public interface IAuthService
{
    /// <summary>
    /// Register med username, email og password
    /// </summary>
    Task<User?> RegisterAsync(string username, string email, string password, UserRole role);

    /// <summary>
    /// Login med username eller email og password
    /// </summary>
    Task<User?> LoginAsync(string usernameOrEmail, string password);

    /// <summary>
    /// Verify password med PBKDF2 med HMAC-SHA256
    /// 
    /// PBKDF2 med HMAC-SHA256 er en hash funktion der bruges til at hashe passwords.
    /// HMAC-SHA256 er en hash funktion der bruges til at hashe passwords.
    /// PBKDF2 er en hash funktion der bruges til at hashe passwords.
    /// HMAC-SHA256 er en hash funktion der bruges til at hashe passwords.
    /// </summary>
    Task<bool> VerifyPasswordAsync(string password, string passwordHash);

    /// <summary>
    /// Hash password med PBKDF2 med HMAC-SHA256, 100000 iterations
    /// 
    /// PBKDF2 med HMAC-SHA256 er en hash funktion der bruges til at hashe passwords.
    /// HMAC-SHA256 er en hash funktion der bruges til at hashe passwords.
    /// PBKDF2 er en hash funktion der bruges til at hashe passwords.
    /// HMAC-SHA256 er en hash funktion der bruges til at hashe passwords.
    /// </summary>
    string HashPassword(string password);
    
    /// <summary>
    /// Tilføj password til en eksisterende SSO-only bruger (account linking).
    /// Gør det muligt for brugeren at logge ind med både SSO og password.
    /// </summary>
    Task<bool> AddPasswordToUserAsync(int userId, string password);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context, 
        ILogger<AuthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> RegisterAsync(string username, string email, string password, UserRole role)
    {
        // Normaliser input til lowercase for sammenligning
        var normalizedUsername = username?.Trim().ToLowerInvariant() ?? string.Empty;
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;

        // Tjek om username eller email allerede eksisterer (case-insensitive)
        if (await _context.Users.AnyAsync(u => 
            u.Username == normalizedUsername || u.Email == normalizedEmail))
        {
            return null;
        }

        var user = new User
        {
            Username = normalizedUsername,
            Email = normalizedEmail,
            PasswordHash = HashPassword(password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<User?> LoginAsync(string usernameOrEmail, string password)
    {
        // Normaliser input til lowercase for case-insensitive lookup
        var normalizedInput = usernameOrEmail?.Trim().ToLowerInvariant() ?? string.Empty;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == normalizedInput || u.Email == normalizedInput);

        if (user == null)
        {
            return null;
        }

        // BEST PRACTICE: Tjek om brugeren har et password
        // SSO-only brugere kan ikke logge ind med password uden at have linket en password til brugeren (account linking)
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            _logger.LogWarning("Bruger {Email} forsøger at logge ind med password, men har kun SSO", user.Email);
            return null;
        }

        if (!await VerifyPasswordAsync(password, user.PasswordHash))
        {
            return null;
        }

        // Opdater last login
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return user;
    }

    public Task<bool> VerifyPasswordAsync(string password, string? passwordHash)
    {
        // BEST PRACTICE: Tjek om password hash eksisterer
        if (string.IsNullOrEmpty(passwordHash))
        {
            return Task.FromResult(false);
        }

        // PBKDF2 med HMAC-SHA256
        var parts = passwordHash.Split(':', 3);
        if (parts.Length != 3)
        {
            return Task.FromResult(false);
        }

        var iterations = int.Parse(parts[0]);
        var salt = Convert.FromBase64String(parts[1]);
        var hash = parts[2];

        var testHash = Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations, HashAlgorithmName.SHA256, 32)); // 256 bits

        return Task.FromResult(hash == testHash);
    }

    public string HashPassword(string password)
    {
        // PBKDF2 med HMAC-SHA256, 100000 iterations
        const int iterations = 100000;
        var salt = RandomNumberGenerator.GetBytes(16); // 128 bits salt

        var hash = Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations, HashAlgorithmName.SHA256, 32)); // 256 bits hash

        return $"{iterations}:{Convert.ToBase64String(salt)}:{hash}";
    }

    /// <summary>
    /// Tilføj password til en eksisterende SSO-only bruger (account linking).
    /// Gør det muligt for brugeren at logge ind med både SSO og password.
    /// </summary>
    public async Task<bool> AddPasswordToUserAsync(int userId, string password)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        // Hvis brugeren allerede har et password, opdater det
        // Dette gør det muligt at ændre password også
        user.PasswordHash = HashPassword(password);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Password tilføjet/opdateret for bruger ID: {UserId}", userId);
        
        return true;
    }
}

