# Quiz BLoC-struktur

Dette dokument beskriver BLoC-strukturen i quiz-featuret og hvordan Events, States og Bloc samarbejder med repository-laget.

---

## Oversigt

Quiz-featuret bruger **én BLoC** (`QuizBloc`) til at håndtere quiz-sessioner, deltagelse, oprettelse af quiz/session og indlæsning af quizzers. BLoC'en afhænger af `QuizRepositoryImpl` og følger mønsteret: **Event → Bloc → State**.

```
┌─────────────┐     add(Event)      ┌─────────────┐     emit(State)      ┌─────────────┐
│   UI/View   │ ──────────────────► │  QuizBloc   │ ──────────────────► │   UI/View   │
└─────────────┘                     └──────┬──────┘                     └─────────────┘
                                          │
                                          │ kalder
                                          ▼
                                   ┌─────────────┐
                                   │ QuizRepo    │
                                   │ (API/Data)  │
                                   └─────────────┘
```

---

## Filstruktur

```
lib/features/quiz/
├── bloc/
│   ├── quiz_bloc.dart   # Bloc-logik
│   ├── quiz_event.dart  # Events
│   └── quiz_state.dart  # States
├── view/
│   ├── quiz_entry_screen.dart
│   ├── quiz_participation_screen.dart
│   ├── quiz_waiting_screen.dart
│   ├── quiz_results_screen.dart
│   ├── quiz_host_screen.dart
│   ├── enhanced_quiz_host_screen.dart
│   ├── create_quiz_screen.dart
│   └── my_quizzes_screen.dart

```

---

## Events (`quiz_event.dart`)

Events er bruger- eller system-handlinger, som BLoC reagerer på. Alle events arver fra `QuizEvent` (Equatable) for nem sammenligning.

| Event | Formål |
|-------|--------|
| `GetSessionByPinEvent(pin)` | Hent session-info via PIN (f.eks. ved indtastning af kode). |
| `JoinSessionEvent(sessionPin, nickname)` | Deltager melder sig til en session med PIN og kaldenavn. |
| `ResetQuizEvent()` | Nulstil BLoC til `QuizInitial` (f.eks. ved "tilbage" eller ny quiz). |
| `CreateQuizEvent(quiz)` | Opret en ny quiz (CreateQuizModel). |
| `CreateSessionEvent(quizId)` | Opret en session for en given quiz. |
| `StartSessionEvent(sessionId)` | Start en eksisterende session. |
| `LoadAllQuizzesEvent()` | Hent listen over alle quizzers (f.eks. "Mine quizzers"). |

---

## States (`quiz_state.dart`)

States beskriver den aktuelle tilstand i BLoC. UI bygger sin visning og navigation ud fra den aktuelle state.

| State | Betydning |
|-------|-----------|
| `QuizInitial` | Starttilstand – klar til at indtaste PIN eller vælge handling. |
| `QuizLoading` | Vent på API-kald (vis fx loading-indikator). |
| `QuizSessionFound(session)` | Session fundet via PIN – vis session-info (fx før join). |
| `QuizJoined(session, participant)` | Deltager er joinet – vis deltager-info og session. |
| `QuizError(message)` | Fejl fra API eller validering – vis fejlbesked. |
| `QuizCreated(quiz)` | Quiz oprettet – fx naviger til redigering eller session. |
| `SessionCreated(session)` | Session oprettet – fx vis PIN og venteskærm. |
| `SessionStarted(session?)` | Session er startet – UI kan opdatere/refreshe. |
| `AllQuizzesLoaded(quizzes)` | Listen over quizzers er hentet – vis "Mine quizzers". |

---

## QuizBloc (`quiz_bloc.dart`)

### Afhængighed

- **`QuizRepositoryImpl`** – injiceret via konstruktør. Bruges til alle API-kald (get session, join, create quiz/session, start session, get all quizzes).

### Event-handlere

| Handler | Event | Hovedflow |
|---------|--------|-----------|
| `_onGetSessionByPin` | `GetSessionByPinEvent` | `QuizLoading` → repo.getSessionByPin → `QuizSessionFound` eller `QuizError`. |
| `_onJoinSession` | `JoinSessionEvent` | `QuizLoading` → repo.joinSession → getSessionByPin → `QuizJoined` eller `QuizError`. |
| `_onResetQuiz` | `ResetQuizEvent` | Emitter `QuizInitial`. |
| `_onCreateQuiz` | `CreateQuizEvent` | `QuizLoading` → repo.createQuiz → `QuizCreated` eller `QuizError`. |
| `_onCreateSession` | `CreateSessionEvent` | `QuizLoading` → repo.createSession → `SessionCreated` eller `QuizError`. |
| `_onStartSession` | `StartSessionEvent` | `QuizLoading` → repo.startSession → `SessionStarted(session: null)` eller `QuizError`. |
| `_onLoadAllQuizzes` | `LoadAllQuizzesEvent` | `QuizLoading` → repo.getAllQuizzes → `AllQuizzesLoaded` eller `QuizError`. |

### Fælles mønster

- Ved async-handlinger: emit `QuizLoading` før kald, derefter enten en success-state med data eller `QuizError(message)` fra `result.exceptionOrNull!.userMessage`.
- Ved fejl bruges altid `QuizError` med en brugerorienteret besked.

---

## Sådan bruger UI'en BLoC'en

1. **BlocProvider** – Sørg for at `QuizBloc` er tilgængelig (fx i quiz-routes eller over quiz-skærme) med en `QuizRepositoryImpl`-instans.
2. **Events** – Kald `context.read<QuizBloc>().add(SomeEvent(...))` ved brugerhandlinger (knap, indtastning af PIN, osv.).
3. **States** – Brug `BlocBuilder<QuizBloc, QuizState>` eller `BlocListener`/`BlocConsumer` og reager på de konkrete states (`QuizLoading`, `QuizSessionFound`, `QuizJoined`, `QuizError`, osv.) med UI og navigation.

Eksempel på flow: Bruger indtaster PIN → `GetSessionByPinEvent(pin)` → Bloc emitter `QuizSessionFound(session)` → UI viser session og "Join"-knap → Bruger trykker Join → `JoinSessionEvent(...)` → Bloc emitter `QuizJoined` → UI navigerer til deltager-/venteskærm.

---

## Kort opsummering

- **Én BLoC** dækker hele quiz-flowet: session-opslag, join, oprettelse af quiz/session, start session og liste over quizzers.
- **Events** = hvad der sker (get by PIN, join, create, start, load list, reset).
- **States** = hvad BLoC rapporterer tilbage (initial, loading, session found, joined, created, started, list loaded, error).
- **Repository** = alle persisterende/API-kald; BLoC koordinerer kun kald og state-ændringer.
