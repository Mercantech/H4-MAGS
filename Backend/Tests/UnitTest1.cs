namespace Tests;

/// <summary>
/// Eksempel Unit Test
/// 
/// Dette er en simpel eksempel test der viser grundlæggende struktur.
/// Se de andre test filer for mere komplekse eksempler.
/// </summary>
[TestFixture]
public class UnitTest1
{
    [SetUp]
    public void Setup()
    {
        // Setup køres før hver test
        // Her kan du sætte op fælles test data
    }

    [TearDown]
    public void TearDown()
    {
        // TearDown køres efter hver test
        // Her kan du rydde op i ressourcer
    }

    /// <summary>
    /// Simpel eksempel test
    /// 
    /// ARRANGE: Forbered test data
    /// ACT: Udfør operation
    /// ASSERT: Verificer resultat
    /// </summary>
    [Test]
    public void Test1()
    {
        // ARRANGE
        var expected = 2;

        // ACT
        var result = 1 + 1;

        // ASSERT
        Assert.That(result, Is.EqualTo(expected));
    }
}
