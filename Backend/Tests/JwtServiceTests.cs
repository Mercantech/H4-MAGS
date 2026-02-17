using System.Security.Claims;
using API.Models;
using API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Tests;

/// <summary>
/// Unit Tests for JwtService
///
/// Tester token-generering, refresh token og validering i isolation.
/// IConfiguration og ILogger mocks for at undgå eksterne afhængigheder.
/// </summary>
[TestFixture]
public class JwtServiceTests
{
    private const string TestSecretKey = "my-super-secret-key-at-least-32-characters-long";
    private const string TestIssuer = "H4-MAGS-API";
    private const string TestAudience = "H4-MAGS-Client";
    private const string TestExpirationMinutes = "60";

    private Mock<IConfiguration> _mockConfig = null!;
    private Mock<ILogger<JwtService>> _mockLogger = null!;
    private JwtService _jwtService = null!;

    [SetUp]
    public void Setup()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["Jwt:SecretKey"]).Returns(TestSecretKey);
        _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns(TestIssuer);
        _mockConfig.Setup(c => c["Jwt:Audience"]).Returns(TestAudience);
        _mockConfig.Setup(c => c["Jwt:ExpirationMinutes"]).Returns(TestExpirationMinutes);

        _mockLogger = new Mock<ILogger<JwtService>>();
        _jwtService = new JwtService(_mockConfig.Object, _mockLogger.Object);
    }

    #region GenerateToken

    [Test]
    public void GenerateToken_WithValidUser_ReturnsNonEmptyToken()
    {
        // ARRANGE
        var user = CreateTestUser(1, "testbruger", "test@example.com", UserRole.Student);

        // ACT
        var token = _jwtService.GenerateToken(user);

        // ASSERT
        Assert.That(token, Is.Not.Null.And.Not.Empty);
        Assert.That(token.Split('.').Length, Is.EqualTo(3), "JWT skal have 3 segmenter (header.payload.signature)");
    }

    [Test]
    public void GenerateToken_WithValidUser_SetsAuthProviderToOldSchoolWhenNull()
    {
        var user = CreateTestUser(1, "testbruger", "test@example.com", UserRole.Student);

        var token = _jwtService.GenerateToken(user, authProvider: null);
        var principal = _jwtService.GetPrincipalFromToken(token);

        Assert.That(principal, Is.Not.Null);
        var authProvider = principal!.FindFirst("auth_provider")?.Value;
        Assert.That(authProvider, Is.EqualTo("OldSchool"));
    }

    [Test]
    public void GenerateToken_WithAuthProviderGoogle_SetsAuthProviderClaimToGoogle()
    {
        var user = CreateTestUser(1, "testbruger", "test@example.com", UserRole.Student);

        var token = _jwtService.GenerateToken(user, "Google");
        var principal = _jwtService.GetPrincipalFromToken(token);

        Assert.That(principal, Is.Not.Null);
        Assert.That(principal!.FindFirst("auth_provider")?.Value, Is.EqualTo("Google"));
    }

    [Test]
    public void GenerateToken_WithAuthProviderGitHub_SetsAuthProviderClaimToGitHub()
    {
        var user = CreateTestUser(1, "testbruger", "test@example.com", UserRole.Student);

        var token = _jwtService.GenerateToken(user, "GitHub");
        var principal = _jwtService.GetPrincipalFromToken(token);

        Assert.That(principal, Is.Not.Null);
        Assert.That(principal!.FindFirst("auth_provider")?.Value, Is.EqualTo("GitHub"));
    }

    [Test]
    public void GenerateToken_WithValidUser_ContainsUserClaims()
    {
        var user = CreateTestUser(42, "teacher1", "teacher@school.dk", UserRole.Teacher);

        var token = _jwtService.GenerateToken(user);
        var principal = _jwtService.GetPrincipalFromToken(token);

        Assert.That(principal, Is.Not.Null);
        Assert.That(principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value, Is.EqualTo("42"));
        Assert.That(principal.FindFirst(ClaimTypes.Name)?.Value, Is.EqualTo("teacher1"));
        Assert.That(principal.FindFirst(ClaimTypes.Email)?.Value, Is.EqualTo("teacher@school.dk"));
        Assert.That(principal.FindFirst(ClaimTypes.Role)?.Value, Is.EqualTo("Teacher"));
    }

    [Test]
    public void GenerateToken_WhenSecretKeyNotConfigured_ThrowsInvalidOperationException()
    {
        _mockConfig.Setup(c => c["Jwt:SecretKey"]).Returns((string?)null);
        var service = new JwtService(_mockConfig.Object, _mockLogger.Object);
        var user = CreateTestUser(1, "u", "e@e.dk", UserRole.Student);

        var ex = Assert.Throws<InvalidOperationException>(() => service.GenerateToken(user));

        Assert.That(ex!.Message, Does.Contain("SecretKey"));
    }

    #endregion

    #region GenerateRefreshToken

    [Test]
    public void GenerateRefreshToken_WhenCalled_ReturnsNonEmptyString()
    {
        var token = _jwtService.GenerateRefreshToken();

        Assert.That(token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GenerateRefreshToken_WhenCalled_ReturnsValidBase64()
    {
        var token = _jwtService.GenerateRefreshToken();

        Assert.DoesNotThrow(() => Convert.FromBase64String(token));
    }

    [Test]
    public void GenerateRefreshToken_WhenCalledMultipleTimes_ReturnsDifferentValues()
    {
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        Assert.That(token1, Is.Not.EqualTo(token2));
    }

    [Test]
    public void GenerateRefreshToken_WhenCalled_ReturnsExpectedLength()
    {
        // 64 bytes => Base64 encoded = 88 chars (ceiling(64*8/6))
        var token = _jwtService.GenerateRefreshToken();
        var decoded = Convert.FromBase64String(token);

        Assert.That(decoded.Length, Is.EqualTo(64));
    }

    #endregion

    #region GetPrincipalFromToken

    [Test]
    public void GetPrincipalFromToken_WithValidToken_ReturnsPrincipalWithClaims()
    {
        var user = CreateTestUser(7, "bruger", "b@b.dk", UserRole.Admin);
        var token = _jwtService.GenerateToken(user);

        var principal = _jwtService.GetPrincipalFromToken(token);

        Assert.That(principal, Is.Not.Null);
        Assert.That(principal!.Identity?.IsAuthenticated, Is.True);
        Assert.That(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, Is.EqualTo("7"));
    }

    [Test]
    public void GetPrincipalFromToken_WithInvalidToken_ReturnsNull()
    {
        var result = _jwtService.GetPrincipalFromToken("invalid.token.here");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetPrincipalFromToken_WithTamperedToken_ReturnsNull()
    {
        var user = CreateTestUser(1, "u", "e@e.dk", UserRole.Student);
        var token = _jwtService.GenerateToken(user);
        var parts = token.Split('.');
        // Ændr payload (middle part)
        parts[1] = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("tampered"));
        var tamperedToken = string.Join(".", parts);

        var result = _jwtService.GetPrincipalFromToken(tamperedToken);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetPrincipalFromToken_WithEmptyString_ReturnsNull()
    {
        var result = _jwtService.GetPrincipalFromToken("");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetPrincipalFromToken_WhenSecretKeyNotConfigured_ReturnsNull()
    {
        var user = CreateTestUser(1, "u", "e@e.dk", UserRole.Student);
        var token = _jwtService.GenerateToken(user);

        _mockConfig.Setup(c => c["Jwt:SecretKey"]).Returns((string?)null);
        var service = new JwtService(_mockConfig.Object, _mockLogger.Object);

        var result = service.GetPrincipalFromToken(token);

        // ValidateToken kaster ved manglende secret; service fanger og returnerer null
        Assert.That(result, Is.Null);
    }

    #endregion

    private static User CreateTestUser(int id, string username, string email, UserRole role)
    {
        return new User
        {
            Id = id,
            Username = username,
            Email = email,
            Role = role
        };
    }
}
