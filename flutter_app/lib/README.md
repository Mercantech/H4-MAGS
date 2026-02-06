# Softwarearkitektur – teori og praksis

Denne README forklarer **softwarearkitektur** i bred forstand: hvordan man opdeler og fordeler ansvar i kode, uanset sprog eller platform. Eksemplerne er fra denne Flutter-app, men principperne gælder generelt.

---

## 1. Hvorfor arkitektur?

Ustruktureret kode bliver hurtigt **svær at ændre**, **svær at teste** og **svær at forstå**. Arkitektur handler om at:

- **Opdele ansvar** – hver del har én klar opgave
- **Reducere kobling** – ændringer i én del påvirker ikke hele systemet
- **Gøre koden testbar** – man kan udskifte dele med mocks
- **Skabe fælles forståelse** – nye udviklere kan navigere i koden

---

## 2. Kernebegreber

### 2.1 Separation of Concerns (SoC)

**Idé:** Opdel systemet i dele, hvor hver del har **ét ansvarsområde**.

| Lag | Ansvar | I denne app |
|-----|--------|-------------|
| **Præsentation** | UI og brugerinteraktion | BLoC, sider, widgets |
| **Forretningslogik / domæne** | Regler og begreber | Entities, repository-interfaces |
| **Data** | Hvor data kommer fra og gemmes | Repositories, datasources, modeller |

**Fordele:** UI kan skiftes ud uden at røre API-kald. API kan skiftes ud uden at røre UI.

---

### 2.2 Lagret arkitektur (Layered Architecture)

Data og kontrol flyder i **én retning**: fra UI ned mod data og tilbage.

```
┌─────────────────────────────────────────────────────────┐
│  PRÆSENTATION (UI, state, events)                       │  ← Brugeren ser og interagerer
└──────────────────────────┬──────────────────────────────┘
                           │ afhænger af
┌──────────────────────────▼──────────────────────────────┐
│  DOMÆNE (entities, regler, kontrakter)                  │  ← "Hvad er en bruger? Hvad kan vi gøre?"
└──────────────────────────┬──────────────────────────────┘
                           │ implementeres af
┌──────────────────────────▼──────────────────────────────┐
│  DATA (API, DB, filer, modeller)                         │  ← Hvor data kommer fra og gemmes
└─────────────────────────────────────────────────────────┘
```

**Vigtig regel:** Et lag må kun kende laget **under** sig (eller samme lag). Præsentation må ikke importere fra data direkte; den går via domænet.

---

### 2.3 Dependency Inversion (SOLID)

**Princippet:** Højniveau-moduler (fx UI/BLoC) må ikke afhænge af lavniveau-moduler (fx konkrete API-klasser). Begge skal afhænge af **abstraktioner** (interfaces/kontrakter).

- **Uden inversion:** BLoC → `WeatherRepositoryImpl` → konkret API. BLoC er bundet til én implementation.
- **Med inversion:** BLoC → `WeatherRepository` (interface) ← `WeatherRepositoryImpl`. BLoC kender kun kontrakten.

**I koden:**

```dart
// BLoC afhænger af INTERFACE (domæne)
class WeatherBloc extends Bloc<...> {
  final WeatherRepository _repository;  // ← Interface, ikke impl
  WeatherBloc({required WeatherRepository repository}) ...
}

// Implementation (data-lag) implementerer interfacet
class WeatherRepositoryImpl implements WeatherRepository { ... }
```

**Fordele:** Man kan teste BLoC med en mock-repository. Man kan skifte fra REST til GraphQL ved at lave en ny implementation af samme interface.

---

### 2.4 Repository-mønsteret

**Idé:** UI og forretningslogik skal ikke vide, om data kommer fra REST, GraphQL, lokal DB eller filer. De taler med **én abstraktion**: "giv mig vejrdata", "gem bruger".

- **Interface** (i domæne): `WeatherRepository` med metoder som `getWeatherForecast()`.
- **Implementation** (i data): `WeatherRepositoryImpl` kalder fx `WeatherRemoteDataSource` og mapper modeller → entities.

Så fordeler man ansvar:

- **Repository:** "Jeg henter data og returnerer domæne-objekter (entities)."
- **DataSource:** "Jeg snakker med API/DB og returnerer rå modeller/DTO’er."
- **BLoC:** "Jeg kalder repository og sætter state til UI."

---

### 2.5 State management (BLoC / lignende)

**Problem:** UI skal vise data og reagere på fejl/loading. Hvor holder man den state, og hvem opdaterer den?

**BLoC-ideen:**  
Events (fx "load weather") sendes til en **BLoC**. BLoC kalder repository, får resultat, og **emitter ny state**. UI lytter på state og tegner sig selv. UI sender ikke direkte API-kald.

```
User trykker "Refresh"
    → Event: RefreshWeatherData
    → BLoC modtager event
    → BLoC kalder repository.getWeatherForecast()
    → Repository returnerer ApiResult<List<WeatherEntity>>
    → BLoC emitter WeatherLoaded(state) eller WeatherError(state)
    → UI rebuildes ud fra state
```

**Fordele:** Én tydelig dataflow, nem at teste (mock repository), UI forbliver "dum".

---

## 3. Mappen som arkitektur

I denne app er **mappestrukturen** en direkte afspejling af lagene:

```
lib/
├── core/                    # Delt infrastruktur (uafhængig af features)
│   ├── api/                 # HTTP-klient, result-typer, interceptors
│   ├── config/              # Miljø, URLs, feature flags
│   ├── di/                  # Dependency injection (hvem skaber hvad)
│   ├── storage/             # Token, preferences
│   ├── theme/               # Fælles design
│   └── utils/               # Hjælpefunktioner
│
├── domain/                  # DOMÆNE – ingen afhængighed af Flutter/API
│   ├── entities/            # Forretningsobjekter (WeatherEntity, User...)
│   └── repositories/        # Kontrakter (abstract class WeatherRepository)
│
├── data/                    # DATA – hvor data kommer fra
│   ├── models/              # DTOs, JSON-modeller (WeatherModel, UserModel...)
│   ├── datasources/         # API-kald, DB, cache (WeatherRemoteDataSource...)
│   └── repositories/        # Konkrete implementations (WeatherRepositoryImpl...)
│
└── features/                # PRÆSENTATION – pr. feature
    ├── weather/
    │   ├── bloc/            # Events, states, WeatherBloc
    │   ├── view/            # Sider (WeatherPage)
    │   └── widgets/         # Genbrugelige UI-komponenter
    ├── auth/
    └── quiz/
```

**Fordele ved feature-first under `features/`:** Alt der hører til "weather" ligger samlet. Man kan arbejde i én mappe og følge dataflowet: view → bloc → domain → data.

---

## 4. Fordeling af ansvar – oversigt

| Hvem | Ansvar | Afhænger af |
|------|--------|-------------|
| **Entity** | Beskriver et domænebegreb (felter, evt. simple regler) | Intet (ren domæne) |
| **Repository interface** | Kontrakt: "Disse metoder findes" | Entities, ApiResult |
| **Model** | JSON/DTO, serialisering, mapping til entity | Intet (eller kun JSON) |
| **DataSource** | Kalde API/DB, returnere modeller i ApiResult | ApiClient, modeller |
| **Repository impl** | Kalde datasource, mappe model → entity, returnere ApiResult | DataSource, entities |
| **BLoC** | Modtage events, kalde repository, emit states | Repository interface |
| **View/Widget** | Lytte på state, vise UI, sende events til BLoC | BLoC, evt. theme/utils |

---

## 5. Vigtige teknikker i dette projekt

### 5.1 ApiResult – fejlhåndtering uden try/catch

I stedet for at kaste exceptions returnerer API-laget `ApiResult<T>`: enten `Success(data)` eller `Failure(exception)`. BLoC pattern-matcher og sætter state (fx `WeatherError(message)`). Det gør fejl **synlige i typen** og undgår uventede exceptions i UI-laget.

### 5.2 Dependency Injection (get_it)

Alle afhængigheder (ApiClient, repositories, BLoCs) registreres ét sted (`core/di/injection.dart`). UI og BLoC får dem injiceret (fx `WeatherBloc(repository: getIt<WeatherRepository>())`). Det gør det nemt at udskifte med mocks i test og at ændre implementation uden at rode i hele appen.

### 5.3 Entity vs. Model

- **Entity** (domæne): Brugerens/forretningens begreb – "Weather", "User". Ingen JSON, ingen API-detaljer.
- **Model** (data): Det format API’et bruger – felter, `fromJson`/`toJson`, evt. ekstra API-felter.  
Repository impl konverterer: `model.toEntity()` før data sendes op til BLoC. Så ændringer i API’et påvirker kun data-laget.

---

## 6. Sådan bruger du det til undervisning

1. **Start med begreberne:** Separation of concerns, lag, dependency inversion, repository. Brug diagrammer og korte kodeeksempler på sproguafhængig måde.
2. **Vis mappestrukturen:** Kør gennem `lib/` og sæt ord på hvad `core/`, `domain/`, `data/` og `features/` har ansvar for.
3. **Følg én anmodning:** Vælg fx "Hent vejr". Gå fra `WeatherPage` → event → `WeatherBloc` → `WeatherRepository` (interface) → `WeatherRepositoryImpl` → `WeatherRemoteDataSource` → `ApiClient`. Vis at UI aldrig kalder API direkte.
4. **Test og udskiftning:** Vis at `WeatherBloc` testes ved at mocke `WeatherRepository`; vis at man kan lave en `FakeWeatherRepository` der returnerer fast data uden at ændre BLoC.
5. **Generelt vs. Flutter:** Understreg at lagene og repository-mønsteret er de samme i backend (C#, Java, …) og andre frontends; kun BLoC og widgets er Flutter-specifikke.

---

## 7. Læs mere i dette repo

- **`API_ARCHITECTURE.md`** – detaljeret gennemgang af API-lag, ApiClient, ApiResult og hvordan du tilføjer nye endpoints.
- **`QUICK_START.md`** – hvordan du kører og bygger projektet.

---

## 8. Kort opsummering

| Princippet | Hvad det betyder i praksis |
|------------|----------------------------|
| **Separation of concerns** | UI, forretningslogik og data er adskilt; én rolle per lag. |
| **Lagret arkitektur** | Præsentation → domæne → data; afhængigheder peger nedad. |
| **Dependency inversion** | BLoC afhænger af repository-*interface*, ikke konkrete klasser. |
| **Repository** | Ét sted der "henter og returnerer data"; skjuler API/DB. |
| **State management (BLoC)** | Events → BLoC → repository → nye states → UI opdateres. |
| **Entity vs. model** | Domæne bruger entities; data-lag bruger modeller og mapper til entity. |

Når I opdeler og fordeler ansvar på denne måde, bliver koden lettere at forstå, teste og ændre – både i Flutter og i andre stacke.
