using API.Data;
using API.Models;
using API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Tests;

/// <summary>
/// Unit Tests for AuthService
/// 
/// DETTE ER UNIT TESTS - tester individuelle metoder i isolation
/// - Ingen database calls (bruger InMemory database eller mocks)
/// - Ingen HTTP requests
/// - Tester kun logikken i metoderne
/// 
/// Læringsmål:
/// 1. Arrange-Act-Assert pattern
/// 2. Forskellige assert typer
/// 3. Mocking af dependencies (ILogger, DbContext)
/// </summary>
[TestFixture]
public class AuthServiceTests
{
    private Mock<ILogger<AuthService>> _mockLogger = null!;
    private ApplicationDbContext _context = null!;
    private AuthService _authService = null!;

    /// <summary>
    /// Setup køres før hver test
    /// Her arrangerer vi (Arrange) de fælles dependencies
    /// </summary>
    [SetUp]
    public void Setup()
    {
        // ARRANGE: Opret mock logger
        _mockLogger = new Mock<ILogger<AuthService>>();

        // ARRANGE: Opret InMemory database (ikke en rigtig database - kun i hukommelsen)
        // Dette gør testen hurtig og isoleret
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}") // Unikt navn for hver test
            .Options;

        _context = new ApplicationDbContext(options);

        // ARRANGE: Opret service med dependencies
        _authService = new AuthService(_context, _mockLogger.Object);
    }

    /// <summary>
    /// Cleanup køres efter hver test
    /// Rydder op i ressourcer
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    #region HashPassword Tests - Pure Function

    /// <summary>
    /// Test 1: HashPassword skal altid producere en hash
    /// 
    /// ARRANGE: Vi har et password
    /// ACT: Vi hasher passwordet
    /// ASSERT: Vi verificerer at hash er produceret
    /// </summary>
    [Test]
    public void HashPassword_MedGyldigtPassword_ReturnererHash()
    {
        // ARRANGE: Forbered test data
        var password = "MitSikrePassword123!";

        // ACT: Udfør den metode vi tester
        var hash = _authService.HashPassword(password);

        // ASSERT: Verificer resultatet
        Assert.That(hash, Is.Not.Null);
        Assert.That(hash, Is.Not.Empty);
        Assert.That(hash, Does.Contain(":")); // Hash format: "iterations:salt:hash"
    }

    /// <summary>
    /// Test 2: HashPassword skal producere forskellige hashes for samme password
    /// Dette er vigtigt for sikkerhed - hver hash skal have unik salt
    /// </summary>
    [Test]
    public void HashPassword_SammePasswordToGange_ReturnererForskelligeHashes()
    {
        // ARRANGE
        var password = "SammePassword123";

        // ACT
        var hash1 = _authService.HashPassword(password);
        var hash2 = _authService.HashPassword(password);

        // ASSERT: Hashes skal være forskellige (pga. unik salt)
        Assert.That(hash1, Is.Not.EqualTo(hash2), 
            "Hver hash skal have unik salt, så samme password giver forskellige hashes");
    }

    /// <summary>
    /// Test 3: HashPassword skal håndtere tomme/null passwords
    /// </summary>
    [Test]
    public void HashPassword_MedTomtPassword_ReturnererHash()
    {
        // ARRANGE
        var password = "";

        // ACT
        var hash = _authService.HashPassword(password);

        // ASSERT
        Assert.That(hash, Is.Not.Null);
        Assert.That(hash, Is.Not.Empty);
    }

    #endregion

    #region VerifyPasswordAsync Tests - Pure Function (perfekt til unit test!)

    /// <summary>
    /// Test 4: VerifyPasswordAsync skal verificere korrekt password
    /// 
    /// Dette er et godt eksempel på Arrange-Act-Assert:
    /// ARRANGE: Opret password og hash det
    /// ACT: Verificer password mod hash
    /// ASSERT: Verificer at resultatet er korrekt
    /// </summary>
    [Test]
    public async Task VerifyPasswordAsync_MedKorrektPassword_ReturnererTrue()
    {
        // ARRANGE: Opret password og hash det
        var password = "MitPassword123";
        var hash = _authService.HashPassword(password);

        // ACT: Verificer password
        var result = await _authService.VerifyPasswordAsync(password, hash);

        // ASSERT: Resultat skal være true
        Assert.That(result, Is.True, "Korrekt password skal verificeres som true");
    }

    /// <summary>
    /// Test 5: VerifyPasswordAsync skal afvise forkert password
    /// </summary>
    [Test]
    public async Task VerifyPasswordAsync_MedForkertPassword_ReturnererFalse()
    {
        // ARRANGE
        var korrektPassword = "KorrektPassword123";
        var hash = _authService.HashPassword(korrektPassword);
        var forkertPassword = "ForkertPassword456";

        // ACT
        var result = await _authService.VerifyPasswordAsync(forkertPassword, hash);

        // ASSERT
        Assert.That(result, Is.False, "Forkert password skal verificeres som false");
    }

    /// <summary>
    /// Test 6: VerifyPasswordAsync skal håndtere null/empty hash
    /// </summary>
    [Test]
    public async Task VerifyPasswordAsync_MedNullHash_ReturnererFalse()
    {
        // ARRANGE
        var password = "NogetPassword123";
        string? nullHash = null;

        // ACT
        var result = await _authService.VerifyPasswordAsync(password, nullHash);

        // ASSERT
        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Test 7: VerifyPasswordAsync skal håndtere ugyldig hash format
    /// </summary>
    [Test]
    public async Task VerifyPasswordAsync_MedUgyldigHashFormat_ReturnererFalse()
    {
        // ARRANGE
        var password = "NogetPassword123";
        var ugyldigHash = "ikke-et-gyldigt-format";

        // ACT
        var result = await _authService.VerifyPasswordAsync(password, ugyldigHash);

        // ASSERT
        Assert.That(result, Is.False);
    }

    #endregion

    #region RegisterAsync Tests - Med Mocking

    /// <summary>
    /// Test 8: RegisterAsync skal oprette ny bruger
    /// 
    /// Dette er stadig en unit test fordi vi bruger InMemory database
    /// Men det er tættere på integration test - vi tester faktisk database interaktion
    /// </summary>
    [Test]
    public async Task RegisterAsync_MedNytBrugernavnOgEmail_ReturnererBruger()
    {
        // ARRANGE
        var username = "testbruger";
        var email = "test@example.com";
        var password = "Password123";
        var role = UserRole.Student;

        // ACT
        var result = await _authService.RegisterAsync(username, email, password, role);

        // ASSERT
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Username, Is.EqualTo(username.ToLowerInvariant()));
        Assert.That(result.Email, Is.EqualTo(email.ToLowerInvariant()));
        Assert.That(result.PasswordHash, Is.Not.Null);
        Assert.That(result.Role, Is.EqualTo(role));

        // Verificer at brugeren faktisk er gemt i databasen
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == result.Id);
        Assert.That(savedUser, Is.Not.Null);
    }

    /// <summary>
    /// Test 9: RegisterAsync skal normalisere username og email til lowercase
    /// </summary>
    [Test]
    public async Task RegisterAsync_MedStorBogstaver_NormalisererTilLowercase()
    {
        // ARRANGE
        var username = "TestBruger";
        var email = "Test@Example.COM";

        // ACT
        var result = await _authService.RegisterAsync(username, email, "Password123", UserRole.Student);

        // ASSERT
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Username, Is.EqualTo("testbruger"));
        Assert.That(result.Email, Is.EqualTo("test@example.com"));
    }

    /// <summary>
    /// Test 10: RegisterAsync skal afvise duplikat username
    /// </summary>
    [Test]
    public async Task RegisterAsync_MedEksisterendeBrugernavn_ReturnererNull()
    {
        // ARRANGE: Opret først en bruger
        await _authService.RegisterAsync("eksisterende", "test1@example.com", "Password123", UserRole.Student);

        // ACT: Prøv at oprette bruger med samme username
        var result = await _authService.RegisterAsync("eksisterende", "test2@example.com", "Password123", UserRole.Student);

        // ASSERT
        Assert.That(result, Is.Null, "Duplikat username skal returnere null");
    }

    /// <summary>
    /// Test 11: RegisterAsync skal afvise duplikat email
    /// </summary>
    [Test]
    public async Task RegisterAsync_MedEksisterendeEmail_ReturnererNull()
    {
        // ARRANGE
        await _authService.RegisterAsync("bruger1", "samme@example.com", "Password123", UserRole.Student);

        // ACT
        var result = await _authService.RegisterAsync("bruger2", "samme@example.com", "Password123", UserRole.Student);

        // ASSERT
        Assert.That(result, Is.Null, "Duplikat email skal returnere null");
    }

    #endregion

    #region LoginAsync Tests

    /// <summary>
    /// Test 12: LoginAsync skal logge ind med korrekt password
    /// </summary>
    [Test]
    public async Task LoginAsync_MedKorrektPassword_ReturnererBruger()
    {
        // ARRANGE: Opret bruger først
        var username = "loginbruger";
        var email = "login@example.com";
        var password = "LoginPassword123";
        
        await _authService.RegisterAsync(username, email, password, UserRole.Student);

        // ACT: Log ind
        var result = await _authService.LoginAsync(username, password);

        // ASSERT
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Username, Is.EqualTo(username));
        Assert.That(result.LastLoginAt, Is.Not.Null, "LastLoginAt skal være sat efter login");
    }

    /// <summary>
    /// Test 13: LoginAsync skal afvise forkert password
    /// </summary>
    [Test]
    public async Task LoginAsync_MedForkertPassword_ReturnererNull()
    {
        // ARRANGE
        var username = "loginbruger2";
        var password = "KorrektPassword123";
        await _authService.RegisterAsync(username, "test@example.com", password, UserRole.Student);

        // ACT
        var result = await _authService.LoginAsync(username, "ForkertPassword");

        // ASSERT
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Test 14: LoginAsync skal kunne logge ind med email også
    /// </summary>
    [Test]
    public async Task LoginAsync_MedEmailIStedetForUsername_ReturnererBruger()
    {
        // ARRANGE
        var email = "email@example.com";
        var password = "Password123";
        await _authService.RegisterAsync("bruger", email, password, UserRole.Student);

        // ACT
        var result = await _authService.LoginAsync(email, password);

        // ASSERT
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Email, Is.EqualTo(email));
    }

    #endregion
}

