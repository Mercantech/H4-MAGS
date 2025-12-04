using API.Controllers;
using API.DTOs.Quiz;
using API.Models;
using NUnit.Framework;

namespace Tests;

/// <summary>
/// Unit Tests for QuizController's private helper metoder
/// 
/// DETTE ER UNIT TESTS - tester kun helper metoder i isolation
/// - Ingen HTTP requests
/// - Ingen database calls
/// - Tester kun ren logik
/// 
/// Læringsmål:
/// 1. Teste private metoder via reflection eller ved at gøre dem internal
/// 2. Teste pure functions (metoder uden side effects)
/// </summary>
[TestFixture]
public class QuizControllerHelperTests
{
    #region GeneratePin Tests

    /// <summary>
    /// Test 1: GeneratePin skal producere en 6-cifret PIN
    /// 
    /// ARRANGE: Ingen setup nødvendig (static metode)
    /// ACT: Generer PIN
    /// ASSERT: Verificer format
    /// </summary>
    [Test]
    public void GeneratePin_Returnerer6CifretNummer()
    {
        // ARRANGE: Ingen setup nødvendig for static metode

        // ACT: Brug reflection til at kalde private static metode
        var method = typeof(QuizController).GetMethod("GeneratePin", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.That(method, Is.Not.Null, "GeneratePin metode skal eksistere");
        
        var pin = method!.Invoke(null, null) as string;

        // ASSERT
        Assert.That(pin, Is.Not.Null);
        Assert.That(pin!.Length, Is.EqualTo(6), "PIN skal være præcis 6 cifre");
        Assert.That(int.TryParse(pin, out _), Is.True, "PIN skal kun indeholde tal");
    }

    /// <summary>
    /// Test 2: GeneratePin skal producere forskellige PINs
    /// </summary>
    [Test]
    public void GeneratePin_KaldtFlereGange_ReturnererForskelligePins()
    {
        // ARRANGE
        var method = typeof(QuizController).GetMethod("GeneratePin", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // ACT: Generer flere PINs
        var pin1 = method!.Invoke(null, null) as string;
        var pin2 = method!.Invoke(null, null) as string;
        var pin3 = method!.Invoke(null, null) as string;

        // ASSERT: Det er usandsynligt at alle tre er ens (men teknisk muligt)
        // Vi tester at mindst to er forskellige
        var alleEns = pin1 == pin2 && pin2 == pin3;
        Assert.That(alleEns, Is.False, 
            "GeneratePin skal producere forskellige værdier (statistisk usandsynligt at alle er ens)");
    }

    /// <summary>
    /// Test 3: GeneratePin skal producere PINs i korrekt interval
    /// </summary>
    [Test]
    public void GeneratePin_ReturnererPinsMellem100000Og999999()
    {
        // ARRANGE
        var method = typeof(QuizController).GetMethod("GeneratePin", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // ACT: Generer flere PINs og test dem alle
        var pins = new List<int>();
        for (int i = 0; i < 100; i++)
        {
            var pin = method!.Invoke(null, null) as string;
            if (int.TryParse(pin, out var pinValue))
            {
                pins.Add(pinValue);
            }
        }

        // ASSERT
        Assert.That(pins.All(p => p >= 100000 && p <= 999999), Is.True,
            "Alle PINs skal være mellem 100000 og 999999");
    }

    #endregion

    #region MapToQuizDto Tests

    /// <summary>
    /// Test 4: MapToQuizDto skal mappe alle properties korrekt
    /// 
    /// Dette er en pure function - perfekt til unit test!
    /// </summary>
    [Test]
    public void MapToQuizDto_MedKompletQuiz_MapperAlleProperties()
    {
        // ARRANGE: Opret en test quiz med alle data
        var quiz = new Quiz
        {
            Id = 1,
            Title = "Test Quiz",
            Description = "Beskrivelse",
            Pin = "123456",
            Status = QuizStatus.Created,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow,
            UserId = 10,
            Questions = new List<Question>
            {
                new Question
                {
                    Id = 1,
                    Text = "Spørgsmål 1?",
                    TimeLimitSeconds = 30,
                    Points = 10,
                    OrderIndex = 1,
                    CreatedAt = DateTime.UtcNow,
                    Answers = new List<Answer>
                    {
                        new Answer { Id = 1, Text = "Svar 1", IsCorrect = true, OrderIndex = 1 },
                        new Answer { Id = 2, Text = "Svar 2", IsCorrect = false, OrderIndex = 2 }
                    }
                }
            }
        };

        // ACT: Brug reflection til at kalde private static metode
        var method = typeof(QuizController).GetMethod("MapToQuizDto", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var result = method!.Invoke(null, new object[] { quiz }) as QuizDto;

        // ASSERT: Verificer alle properties
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(quiz.Id));
        Assert.That(result.Title, Is.EqualTo(quiz.Title));
        Assert.That(result.Description, Is.EqualTo(quiz.Description));
        Assert.That(result.Pin, Is.EqualTo(quiz.Pin));
        Assert.That(result.Status, Is.EqualTo(quiz.Status.ToString()));
        Assert.That(result.CreatedAt, Is.EqualTo(quiz.CreatedAt));
        Assert.That(result.UpdatedAt, Is.EqualTo(quiz.UpdatedAt));
        Assert.That(result.UserId, Is.EqualTo(quiz.UserId));
        
        // Verificer spørgsmål
        Assert.That(result.Questions, Is.Not.Null);
        Assert.That(result.Questions.Count, Is.EqualTo(1));
        Assert.That(result.Questions[0].Text, Is.EqualTo("Spørgsmål 1?"));
        Assert.That(result.Questions[0].Answers.Count, Is.EqualTo(2));
    }

    /// <summary>
    /// Test 5: MapToQuizDto skal sortere spørgsmål efter OrderIndex
    /// </summary>
    [Test]
    public void MapToQuizDto_MedUsorteredeSpørgsmål_SortererEfterOrderIndex()
    {
        // ARRANGE: Opret quiz med usorterede spørgsmål
        var quiz = new Quiz
        {
            Id = 1,
            Title = "Test",
            Pin = "123456",
            Questions = new List<Question>
            {
                new Question { Id = 1, Text = "Tredje", OrderIndex = 3, Answers = new List<Answer>() },
                new Question { Id = 2, Text = "Første", OrderIndex = 1, Answers = new List<Answer>() },
                new Question { Id = 3, Text = "Anden", OrderIndex = 2, Answers = new List<Answer>() }
            }
        };

        // ACT
        var method = typeof(QuizController).GetMethod("MapToQuizDto", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method!.Invoke(null, new object[] { quiz }) as QuizDto;

        // ASSERT: Spørgsmål skal være sorteret
        Assert.That(result!.Questions[0].Text, Is.EqualTo("Første"));
        Assert.That(result.Questions[1].Text, Is.EqualTo("Anden"));
        Assert.That(result.Questions[2].Text, Is.EqualTo("Tredje"));
    }

    /// <summary>
    /// Test 6: MapToQuizDto skal sortere svar efter OrderIndex
    /// </summary>
    [Test]
    public void MapToQuizDto_MedUsorteredeSvar_SortererEfterOrderIndex()
    {
        // ARRANGE
        var quiz = new Quiz
        {
            Id = 1,
            Title = "Test",
            Pin = "123456",
            Questions = new List<Question>
            {
                new Question
                {
                    Id = 1,
                    Text = "Spørgsmål?",
                    Answers = new List<Answer>
                    {
                        new Answer { Id = 3, Text = "Tredje svar", OrderIndex = 3 },
                        new Answer { Id = 1, Text = "Første svar", OrderIndex = 1 },
                        new Answer { Id = 2, Text = "Andet svar", OrderIndex = 2 }
                    }
                }
            }
        };

        // ACT
        var method = typeof(QuizController).GetMethod("MapToQuizDto", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method!.Invoke(null, new object[] { quiz }) as QuizDto;

        // ASSERT
        var answers = result!.Questions[0].Answers;
        Assert.That(answers[0].Text, Is.EqualTo("Første svar"));
        Assert.That(answers[1].Text, Is.EqualTo("Andet svar"));
        Assert.That(answers[2].Text, Is.EqualTo("Tredje svar"));
    }

    /// <summary>
    /// Test 7: MapToQuizDto skal håndtere tom quiz (ingen spørgsmål)
    /// </summary>
    [Test]
    public void MapToQuizDto_MedTomQuiz_ReturnererQuizUdenSpørgsmål()
    {
        // ARRANGE
        var quiz = new Quiz
        {
            Id = 1,
            Title = "Tom Quiz",
            Pin = "123456",
            Questions = new List<Question>()
        };

        // ACT
        var method = typeof(QuizController).GetMethod("MapToQuizDto", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method!.Invoke(null, new object[] { quiz }) as QuizDto;

        // ASSERT
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Questions, Is.Not.Null);
        Assert.That(result.Questions.Count, Is.EqualTo(0));
    }

    #endregion
}

