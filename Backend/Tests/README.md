# Unit Tests - H4-MAGS Backend

## Oversigt

Dette projekt indeholder **unit tests** for H4-MAGS API backend. Tests er skrevet med **NUnit** og følger **Arrange-Act-Assert** pattern.

## Struktur

```
Tests/
├── AuthServiceTests.cs          # Tests for password hashing og authentication
├── QuizControllerHelperTests.cs  # Tests for helper metoder i QuizController
├── UserModelTests.cs             # Tests for User model properties
├── UnitTest1.cs                  # Eksempel test
├── TestPlan.md                   # Dokumentation om unit vs integration vs E2E tests
└── README.md                     # Denne fil
```

## Kør Tests

### I Visual Studio / Rider
- Højreklik på test projektet → "Run Tests"
- Eller brug Test Explorer

### Fra Command Line
```bash
cd Backend/Tests
dotnet test
```

### Med Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Kategorier

### ✅ Unit Tests (Det vi laver nu)
- Test individuelle metoder i isolation
- Ingen database, ingen HTTP calls
- Hurtige (< 1 sekund)
- Bruger mocks for dependencies

### ⏭️ Integration Tests (Senere)
- Test flere komponenter sammen
- Bruger rigtig database (InMemory eller test database)
- Teste API endpoints

### ⏭️ E2E Tests (Senere - med Bruno/Postman)
- Test hele systemet
- Test med rigtige HTTP requests
- Test bruger flows

## Læringsmål

### 1. Formål med Unit Tests
- ✅ Verificer at kode virker korrekt
- ✅ Dokumenter hvordan kode skal bruges
- ✅ Forhindre regression (fejl der kommer tilbage)
- ✅ Gør refactoring sikkert

### 2. Forskellen på Test Typer
- **Unit Tests**: Test én metode/klasse i isolation
- **Integration Tests**: Test flere komponenter sammen
- **E2E Tests**: Test hele systemet som en bruger

### 3. Arrange-Act-Assert Pattern
```csharp
[Test]
public void TestEksempel()
{
    // ARRANGE: Forbered test data
    var password = "Test123";
    
    // ACT: Udfør operation
    var hash = service.HashPassword(password);
    
    // ASSERT: Verificer resultat
    Assert.That(hash, Is.Not.Null);
}
```

### 4. Assert Typer
- `Assert.That(result, Is.Not.Null)` - Moderne, anbefalet
- `Assert.AreEqual(expected, actual)` - Klassisk
- `Assert.IsTrue(condition)` - Klassisk

### 5. Mocking
- Bruger **Moq** til at mocke dependencies
- Isolerer testen - tester kun én klasse
- Gør testen hurtig og deterministisk

## Eksempler

### Eksempel 1: Test Pure Function
```csharp
[Test]
public void HashPassword_MedGyldigtPassword_ReturnererHash()
{
    // ARRANGE
    var password = "Test123";
    
    // ACT
    var hash = _authService.HashPassword(password);
    
    // ASSERT
    Assert.That(hash, Is.Not.Null);
}
```

### Eksempel 2: Test med Mocking
```csharp
[SetUp]
public void Setup()
{
    // ARRANGE: Opret mock
    var mockLogger = new Mock<ILogger<AuthService>>();
    var context = new ApplicationDbContext(...);
    
    _authService = new AuthService(context, mockLogger.Object);
}
```

### Eksempel 3: Test Edge Cases
```csharp
[Test]
public void HashPassword_MedTomtPassword_ReturnererHash()
{
    // ARRANGE
    var password = "";
    
    // ACT
    var hash = _authService.HashPassword(password);
    
    // ASSERT
    Assert.That(hash, Is.Not.Null);
}
```

## Best Practices

1. **Én assertion pr. test** (når muligt)
2. **Beskrivende test navne**: `MethodName_Scenario_ExpectedResult`
3. **Arrange-Act-Assert** struktur
4. **Isolation** - hver test kan køre alene
5. **Fast** - tests skal være hurtige
6. **Deterministiske** - samme input = samme output

## Næste Skridt

1. ✅ Unit tests for AuthService
2. ✅ Unit tests for QuizController helpers
3. ✅ Unit tests for User model
4. ⏭️ Integration tests (senere)
5. ⏭️ E2E tests med Bruno/Postman (senere)

## Ressourcer

- [NUnit Documentation](https://docs.nunit.org/)
- [Moq Documentation](https://github.com/moq/moq4)
- [TestPlan.md](./TestPlan.md) - Detaljeret forklaring af test typer

