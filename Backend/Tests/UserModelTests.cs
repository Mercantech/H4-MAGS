using API.Models;
using NUnit.Framework;

namespace Tests;

/// <summary>
/// Unit Tests for User Model
/// 
/// DETTE ER UNIT TESTS - tester model logik (properties, normalization)
/// - Ingen database
/// - Ingen services
/// - Tester kun model behavior
/// 
/// Læringsmål:
/// 1. Teste model properties og deres behavior
/// 2. Teste normalization (lowercase conversion)
/// </summary>
[TestFixture]
public class UserModelTests
{
    #region Username Property Tests

    /// <summary>
    /// Test 1: Username skal normalisere til lowercase
    /// 
    /// ARRANGE: Opret User med store bogstaver
    /// ACT: Sæt Username property
    /// ASSERT: Verificer at den er lowercase
    /// </summary>
    [Test]
    public void Username_SetMedStoreBogstaver_NormalisererTilLowercase()
    {
        // ARRANGE & ACT
        var user = new User
        {
            Username = "TestBruger123"
        };

        // ASSERT
        Assert.That(user.Username, Is.EqualTo("testbruger123"));
    }

    /// <summary>
    /// Test 2: Username skal trimme whitespace
    /// </summary>
    [Test]
    public void Username_SetMedWhitespace_TrimmerWhitespace()
    {
        // ARRANGE & ACT
        var user = new User
        {
            Username = "  testbruger  "
        };

        // ASSERT
        Assert.That(user.Username, Is.EqualTo("testbruger"));
    }

    /// <summary>
    /// Test 3: Username skal håndtere null
    /// </summary>
    [Test]
    public void Username_SetTilNull_BliverTilTomStreng()
    {
        // ARRANGE & ACT
        var user = new User
        {
            Username = null!
        };

        // ASSERT
        Assert.That(user.Username, Is.EqualTo(string.Empty));
    }

    /// <summary>
    /// Test 4: Username skal håndtere tom streng
    /// </summary>
    [Test]
    public void Username_SetTilTomStreng_ForbliverTomStreng()
    {
        // ARRANGE & ACT
        var user = new User
        {
            Username = ""
        };

        // ASSERT
        Assert.That(user.Username, Is.EqualTo(string.Empty));
    }

    #endregion

    #region Email Property Tests

    /// <summary>
    /// Test 5: Email skal normalisere til lowercase
    /// </summary>
    [Test]
    public void Email_SetMedStoreBogstaver_NormalisererTilLowercase()
    {
        // ARRANGE & ACT
        var user = new User
        {
            Email = "Test@Example.COM"
        };

        // ASSERT
        Assert.That(user.Email, Is.EqualTo("test@example.com"));
    }

    /// <summary>
    /// Test 6: Email skal trimme whitespace
    /// </summary>
    [Test]
    public void Email_SetMedWhitespace_TrimmerWhitespace()
    {
        // ARRANGE & ACT
        var user = new User
        {
            Email = "  test@example.com  "
        };

        // ASSERT
        Assert.That(user.Email, Is.EqualTo("test@example.com"));
    }

    /// <summary>
    /// Test 7: Email skal håndtere null
    /// </summary>
    [Test]
    public void Email_SetTilNull_BliverTilTomStreng()
    {
        // ARRANGE & ACT
        var user = new User
        {
            Email = null!
        };

        // ASSERT
        Assert.That(user.Email, Is.EqualTo(string.Empty));
    }

    #endregion

    #region Role Property Tests

    /// <summary>
    /// Test 8: Role skal have default værdi
    /// </summary>
    [Test]
    public void Role_DefaultVærdi_ErStudent()
    {
        // ARRANGE & ACT
        var user = new User();

        // ASSERT
        Assert.That(user.Role, Is.EqualTo(UserRole.Student));
    }

    /// <summary>
    /// Test 9: Role skal kunne sættes til Teacher
    /// </summary>
    [Test]
    public void Role_SetTilTeacher_BliverTeacher()
    {
        // ARRANGE & ACT
        var user = new User
        {
            Role = UserRole.Teacher
        };

        // ASSERT
        Assert.That(user.Role, Is.EqualTo(UserRole.Teacher));
    }

    /// <summary>
    /// Test 10: Role skal kunne sættes til Admin
    /// </summary>
    [Test]
    public void Role_SetTilAdmin_BliverAdmin()
    {
        // ARRANGE & ACT
        var user = new User
        {
            Role = UserRole.Admin
        };

        // ASSERT
        Assert.That(user.Role, Is.EqualTo(UserRole.Admin));
    }

    #endregion

    #region PasswordHash Property Tests

    /// <summary>
    /// Test 11: PasswordHash skal kunne være null (SSO-only brugere)
    /// </summary>
    [Test]
    public void PasswordHash_SetTilNull_ForbliverNull()
    {
        // ARRANGE & ACT
        var user = new User
        {
            PasswordHash = null
        };

        // ASSERT
        Assert.That(user.PasswordHash, Is.Null);
    }

    /// <summary>
    /// Test 12: PasswordHash skal kunne sættes
    /// </summary>
    [Test]
    public void PasswordHash_SetTilHash_ForbliverHash()
    {
        // ARRANGE & ACT
        var hash = "100000:salt:hash";
        var user = new User
        {
            PasswordHash = hash
        };

        // ASSERT
        Assert.That(user.PasswordHash, Is.EqualTo(hash));
    }

    #endregion

    #region Integration Tests (tester flere properties sammen)

    /// <summary>
    /// Test 13: User skal kunne oprettes med alle properties
    /// </summary>
    [Test]
    public void User_OprettetMedAlleProperties_HarAllePropertiesSæt()
    {
        // ARRANGE & ACT
        var user = new User
        {
            Username = "TestBruger",
            Email = "Test@Example.COM",
            PasswordHash = "100000:salt:hash",
            Role = UserRole.Teacher
        };

        // ASSERT
        Assert.That(user.Username, Is.EqualTo("testbruger"));
        Assert.That(user.Email, Is.EqualTo("test@example.com"));
        Assert.That(user.PasswordHash, Is.EqualTo("100000:salt:hash"));
        Assert.That(user.Role, Is.EqualTo(UserRole.Teacher));
    }

    #endregion
}

