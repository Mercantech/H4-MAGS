using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace API.Services;

public interface IAuthService
{
    Task<User?> RegisterAsync(string username, string email, string password, UserRole role);
    Task<User?> LoginAsync(string usernameOrEmail, string password);
    Task<bool> VerifyPasswordAsync(string password, string passwordHash);
    string HashPassword(string password);
    Task<User?> LoginWithGoogleAsync(string idToken);
    Task<User?> LoginWithGoogleAccessTokenAsync(string accessToken);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private readonly IGoogleAuthService _googleAuthService;

    public AuthService(
        ApplicationDbContext context, 
        ILogger<AuthService> logger,
        IGoogleAuthService googleAuthService)
    {
        _context = context;
        _logger = logger;
        _googleAuthService = googleAuthService;
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

        if (!await VerifyPasswordAsync(password, user.PasswordHash))
        {
            return null;
        }

        // Opdater last login
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return user;
    }

    public Task<bool> VerifyPasswordAsync(string password, string passwordHash)
    {
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

    public async Task<User?> LoginWithGoogleAsync(string idToken)
    {
        // Verificer Google token
        var payload = await _googleAuthService.VerifyGoogleTokenAsync(idToken);
        if (payload == null)
        {
            return null;
        }

        // Hent eller opret bruger
        return await _googleAuthService.GetOrCreateUserFromGoogleAsync(payload);
    }

    public async Task<User?> LoginWithGoogleAccessTokenAsync(string accessToken)
    {
        // Hent eller opret bruger fra access_token
        return await _googleAuthService.GetOrCreateUserFromAccessTokenAsync(accessToken);
    }
}

