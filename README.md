# H4-MAGS

Kahoot-lignende quiz-app: Flutter web-frontend, .NET API, PostgreSQL, Seq, MinIO (S3), Bruno E2E.

---

## Quick start

```bash
# .env med bl.a. DefaultConnection (PostgreSQL), Jwt__SecretKey, evt. MINIO_ROOT_USER/MINIO_ROOT_PASSWORD
docker compose up -d
```

**Portoversigt**

| Tjeneste   | Port (host) | Beskrivelse        |
|-----------|-------------|--------------------|
| API       | 9080        | Backend            |
| Flutter   | 9081        | Web-app            |
| Seq       | 9082        | Logs               |
| MinIO API | 9084        | S3                 |
| MinIO UI  | 9085        | Web Console        |
| Bruno     | 9083        | E2E-rapport (HTML) |

---

## Backend – hvad er der under motorhjelmen?

- **Auth & brugere** — Registrering, login, JWT + refresh tokens, og **OAuth** (Google, GitHub): login med ekstern provider, automatisk oprettelse af bruger, mulighed for at tilføje password bagefter. Roller: Student, Teacher, Admin.
- **Quiz & sessioner** — Lærere opretter quizzer og åbner sessioner med PIN; deltagerne joiner via PIN, får spørgsmål løbende, sender svar og kan se leaderboard. Fuldt flow fra oprettelse til resultater.
- **Storage (MinIO)** — S3-kompatibel fil-lagring: `POST /api/storage/upload` (autoriseret, op til 5 MB) returnerer nøgle og URL; `GET /api/storage/file/{key}` server filen (åben for alle). Bucket oprettes automatisk ved første brug.
- **Mail** — Velkomstmail ved registrering (Gmail SMTP), plus test-endpoint til at tjekke opsætning.
- **Observability** — Strukturerede logs sendes til **Seq** (søg og filtrer i brugerfladen på 9082). Swagger/OpenAPI med JWT-support til at prøve API’et.

---

## Repo-struktur

- **Backend/** — .NET 10 API (EF Core, PostgreSQL), Auth/JWT/OAuth, Quiz, QuizSession, Participant, User, Storage (MinIO), Mail; unit tests; Aspire AppHost.
- **flutter_app/** — Flutter web-client mod API’et.
- **Bruno/** — E2E-UserFlows: Auth (register, login, refresh, OAuth), Teacher (quiz + session, start, join), Student (spørgsmål, svar, leaderboard), Public API, Quiz read/update, Cleanup. Kører ved deploy og i CI.
- **docker-compose.yaml** — Prod: backend, flutterweb, seq, minio (S3 + Web Console på fast port 9001), bruno-run-on-deploy, bruno-reports.
- **docker-compose.test.yml** — Kun til CI: PostgreSQL + Seq + backend (migrationer ved opstart), ingen MinIO/Mail; Bruno kører mod denne stack.

---

## Udvikling

- **Backend:** `cd Backend/API && dotnet run` — kræver PostgreSQL (fx lokalt eller i Docker) og JWT/connection string i appsettings eller User Secrets.
- **Flutter:** `cd flutter_app && flutter run -d chrome` — sæt base URL til lokal API (fx `http://localhost:9080`).
- **Bruno E2E mod lokal API:** `docker compose --profile cli run bruno-cli` — sæt `API_BASE_URL` i compose/env til fx `http://host.docker.internal:9080`, så Bruno rammer din lokale backend; rapporten skrives til `bruno-reports`-volumen og kan ses via bruno-reports-tjenesten (9083).

---

## CI

Ved push til `main`/`master`:

1. **Unit tests** — Backend.Tests (Auth, JWT, User, m.fl.) kører først.
2. **API-tests** — Test-stack startes (`docker-compose.test.yml`: PostgreSQL + Seq + backend med migrationer); Bruno kører E2E-UserFlows mod denne API; mail er slået fra. Rapport bruges til at sikre, at auth, quiz, sessioner og public endpoints holder.

E2E-databasen ryddes op via Cleanup-flow (fx delete E2E-bruger), så prod ikke bliver belastet af testdata.
