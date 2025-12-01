# Generisk OAuth Implementation i Flutter App

## Oversigt

Flutter appen er nu opdateret til at bruge den **generiske OAuth løsning** fra backend i stedet for Google-specifikke endpoints.

## Hvad er Ændret?

### 1. Ny Generisk OAuth Model

**Ny fil:** `lib/data/models/auth/oauth_login_request.dart`

```dart
class OAuthLoginRequest {
  final String provider;  // "Google", "Microsoft", etc.
  final String accessToken;
  
  Map<String, dynamic> toJson() {
    return {
      'provider': provider,
      'accessToken': accessToken,
    };
  }
}
```

### 2. Opdateret AuthRemoteDataSource

**Ny metode:** `loginWithOAuth()`

```dart
Future<ApiResult<AuthResponseModel>> loginWithOAuth({
  required String provider,
  required String accessToken,
}) async {
  final request = OAuthLoginRequest(
    provider: provider,
    accessToken: accessToken,
  );
  
  return await _apiClient.post<AuthResponseModel>(
    '/auth/oauth-login',  // Generisk endpoint!
    body: request.toJson(),
    fromJson: (json) => AuthResponseModel.fromJson(json as Map<String, dynamic>),
  );
}
```

### 3. Opdateret AuthRepositoryImpl

**loginWithGoogle() bruger nu generisk OAuth:**

```dart
// Før: loginWithGoogleAccessToken(accessToken)
// Nu: loginWithOAuth(provider: 'Google', accessToken: accessToken)
```

## Flow

```
1. User klikker "Login med Google"
   ↓
2. GoogleSignIn SDK håndterer Google login
   ↓
3. Får access token fra Google
   ↓
4. AuthRepositoryImpl.loginWithGoogle()
   ↓
5. AuthRemoteDataSource.loginWithOAuth(provider: 'Google', accessToken: ...)
   ↓
6. POST /api/auth/oauth-login { "provider": "Google", "accessToken": "..." }
   ↓
7. Backend OAuthService håndterer alle providers generisk
   ↓
8. Returnerer JWT token
```

## Fordele

### ✅ Generisk Design
- Samme kode virker for alle OAuth providers
- Nem at tilføje Microsoft, GitHub, etc.

### ✅ Bedre for Flutter Web
- Access token er det primære flow (det vi får på Web)
- Ingen kompleks idToken håndtering nødvendig

### ✅ Konsistent med Backend
- Bruger samme generiske endpoint som backend
- Fremtidssikret arkitektur

## Bagudkompatibilitet

De gamle metoder eksisterer stadig:
- `loginWithGoogle(idToken)` - Fallback hvis idToken er tilgængelig
- `loginWithGoogleAccessToken(accessToken)` - Markedet som @deprecated

**Men den nye generiske metode anbefales!**

## Sådan Tilføjer Du En Ny Provider

### Eksempel: Tilføj Microsoft

1. **Tilføj Microsoft Sign-In pakke** (hvis nødvendig):
   ```yaml
   dependencies:
     microsoft_authentication: ^1.0.0  # Eksempel
   ```

2. **Opret login metode i AuthRepositoryImpl:**
   ```dart
   Future<ApiResult<AuthResponseModel>> loginWithMicrosoft() async {
     // Hent access token fra Microsoft SDK
     final accessToken = await microsoftAuth.signIn();
     
     // Brug generisk OAuth metode!
     return await _remoteDataSource.loginWithOAuth(
       provider: 'Microsoft',
       accessToken: accessToken,
     );
   }
   ```

3. **Backend skal have Microsoft konfigureret** (se backend docs)

**Det er det!** Ingen nye endpoints, ingen ny datasource kode - bare kalder den generiske metode! 🎉

## Testing

### Test med Google

1. Kør Flutter appen
2. Klik "Login med Google"
3. Check console logs:
   ```
   ✅ [DEBUG] Bruger generisk OAuth endpoint med access token
   📤 [DEBUG] Sender access_token til backend via /oauth-login...
   📥 [DEBUG] Modtog svar fra backend: SUCCESS
   ```

### Debugging

Hvis der er problemer, tjek:
- ✅ Backend `/api/auth/oauth-login` endpoint virker
- ✅ Provider navn er korrekt: "Google" (case-sensitive)
- ✅ Access token er gyldigt
- ✅ Backend har Google konfigureret i `appsettings.json`

## Migration Status

- ✅ Generisk OAuth model oprettet
- ✅ Datasource opdateret med generisk metode
- ✅ Repository bruger generisk metode som primær løsning
- ✅ Gamle metoder bevares for bagudkompatibilitet
- ✅ Kommentarer opdateret

## Næste Skridt

1. Test Google login med den nye generiske løsning
2. Overvej at fjerne gamle Google-specifikke metoder (efter test)
3. Tilføj Microsoft/GitHub når behov opstår (nemt nu!)

