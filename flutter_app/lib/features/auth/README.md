# Google Sign-In Integration for Flutter Web

Denne guide forklarer hvordan Google Sign-In er implementeret til Flutter Web app.

## Arkitektur

### Flow
1. **Flutter Web App**: Brugeren klikker på "Login med Google"
2. **Google Sign-In SDK**: Håndterer Google login flow og returnerer ID token
3. **Flutter Repository**: Sender ID token til backend API
4. **Backend API**: Verificerer Google token (med audience validation) og opretter/henter bruger
5. **Backend API**: Genererer JWT token og returnerer det til Flutter app
6. **Flutter App**: Gemmer JWT token og bruger det til alle fremtidige API calls

### Vigtigt
- **Google håndterer kun login/oprettelse** - Google ID token bruges kun til at verificere brugerens identitet
- **JWT systemet styrer adgang og tilladelser** - Alle API calls bruger dit eget JWT token, ikke Google token
- **Brugere oprettes automatisk** - Hvis en Google bruger ikke eksisterer, oprettes de automatisk med Student rolle
- **Kun Web Application client** - Da du kun bruger Flutter Web, skal du kun oprette én OAuth client

## Setup

### 1. Google Cloud Console Setup

1. Gå til [Google Cloud Console](https://console.cloud.google.com/)
2. Opret et nyt projekt eller vælg eksisterende
3. Aktiver **Google Sign-In API**
4. Opret **OAuth 2.0 credentials**:
   - Gå til "Credentials" → "Create Credentials" → "OAuth client ID"
   - **Vælg "Web application"** (dette er den eneste type du skal bruge)
   - Giv det et navn (f.eks. "H4-MAGS Web Client")
   - **Authorized JavaScript origins:**
     - `https://h4-flutter.mercantec.tech` (din production URL)
     - `http://localhost` (for lokal udvikling)
   - **Authorized redirect URIs:**
     - `https://h4-flutter.mercantec.tech` (din production URL)
     - `http://localhost:xxxxx` (for lokal udvikling, hvor xxxxx er din port)
   - Klik "CREATE"
   - **Kopier Client ID** - du skal bruge denne i både Flutter og Backend

### 2. Flutter Setup

1. Opdater `flutter_app/lib/core/config/google_config.dart`:
```dart
static const String webClientId = 'DIN-GOOGLE-CLIENT-ID.apps.googleusercontent.com';
```

2. Client ID bruges automatisk via dependency injection

### 3. Backend Setup

1. Tilføj Google Client ID til `appsettings.json`:
```json
{
  "Google": {
    "ClientId": "SAMME-CLIENT-ID.apps.googleusercontent.com"
  }
}
```

**Vigtigt:** Brug samme Client ID i både Flutter og Backend for at validere audience korrekt.

2. `GoogleAuthService` validerer automatisk audience hvis `Google:ClientId` er sat

## Brug

### I Flutter App

```dart
import 'package:your_app/data/repositories/auth_repository_impl.dart';

final authRepository = AuthRepositoryImpl(
  remoteDataSource: authRemoteDataSource,
);

// Login med Google
final result = await authRepository.loginWithGoogle();

result.when(
  success: (authResponse) {
    // Gem token
    await saveToken(authResponse.token);
    // Naviger til hovedside
  },
  failure: (error) {
    // Vis fejlbesked
    print('Login fejlede: ${error.message}');
  },
);
```

## API Endpoints

### POST /api/auth/google-login
Login med Google ID token.

**Request:**
```json
{
  "idToken": "google-id-token-her"
}
```

**Response:**
```json
{
  "token": "jwt-token",
  "refreshToken": "refresh-token",
  "expires": "2024-01-01T12:00:00Z",
  "user": {
    "id": 1,
    "username": "john.doe",
    "email": "john.doe@gmail.com",
    "role": "Student",
    "createdAt": "2024-01-01T10:00:00Z"
  }
}
```

## Fejlfinding

### "Ugyldig Google token"
- Tjek at Google Client ID er korrekt konfigureret
- Tjek at token ikke er udløbet
- Tjek at token er et ID token, ikke access token

### "Kunne ikke hente ID token fra Google"
- Tjek at Google Sign-In SDK er korrekt konfigureret
- Tjek at SHA-1 fingerprint er tilføjet til Google Cloud Console (Android)
- Tjek at Bundle ID matcher (iOS)

