import 'package:google_sign_in/google_sign_in.dart';
import '../datasources/auth_remote_datasource.dart';
import '../models/auth/auth_response_model.dart';
import '../../core/api/api_result.dart';

/// Implementation of authentication repository
/// 
/// Håndterer Google Sign-In og API calls til backend.
class AuthRepositoryImpl {
  final AuthRemoteDataSource _remoteDataSource;
  final GoogleSignIn _googleSignIn;

  /// Expose GoogleSignIn for renderButton() usage
  GoogleSignIn get googleSignIn => _googleSignIn;

  AuthRepositoryImpl({
    required AuthRemoteDataSource remoteDataSource,
    GoogleSignIn? googleSignIn,
    String? webClientId,
  })  : _remoteDataSource = remoteDataSource,
        _googleSignIn = googleSignIn ?? GoogleSignIn(
          // For Flutter Web, skal clientId specificeres
          clientId: webClientId,
          scopes: ['email', 'profile', 'openid'], // Tilføj 'openid' for idToken
          // Force account selection for at få idToken
          hostedDomain: null, // Tillad alle domæner
        );

  /// Login med Google via generisk OAuth løsning
  /// 
  /// Bruger den nye generiske OAuth endpoint som virker perfekt med access tokens.
  /// Flow:
  /// 1. Prøver signInSilently() først (virker hvis bruger allerede er logget ind)
  /// 2. Hvis det fejler, logger vi ud og bruger signIn()
  /// 3. Henter access token fra Google (det vi får på Flutter Web)
  /// 4. Sender access token til generisk /oauth-login endpoint med provider="Google"
  /// 
  /// Den generiske løsning håndterer automatisk:
  /// - Hentning af brugerinfo fra Google API
  /// - Account linking til eksisterende konti
  /// - Oprettelse af nye brugere
  Future<ApiResult<AuthResponseModel>> loginWithGoogle() async {
    try {
      GoogleSignInAccount? googleUser;
      
      // Prøv først signInSilently() - dette virker hvis brugeren allerede er logget ind
      googleUser = await _googleSignIn.signInSilently();
      
      if (googleUser == null) {
        // Hvis signInSilently fejler, log ud og prøv signIn() med requestScopes
        await _googleSignIn.signOut();
        await Future.delayed(const Duration(milliseconds: 300));
        
        // Brug signIn() - dette åbner en popup
        googleUser = await _googleSignIn.signIn();
        
        if (googleUser == null) {
          return ApiResult.failure(
            ApiException.unknown('Google login blev annulleret'),
          );
        }
        
        // Note: Vi kan ikke tvinge en ny consent prompt direkte
        // Problemet er at signIn() på web ikke altid giver idToken
      }

      // Hent authentication details
      GoogleSignInAuthentication googleAuth = await googleUser.authentication;

      // Brug generisk OAuth endpoint med access token
      // På Flutter Web får vi altid access_token, som er det primære flow
      if (googleAuth.accessToken == null) {
        return ApiResult.failure(
          ApiException.unknown(
            'Kunne ikke hente access token fra Google. '
            'Prøv at logge ud og ind igen.'
          ),
        );
      }

      // Brug generisk OAuth endpoint - virker perfekt med access token
      return await _remoteDataSource.loginWithOAuth(
        provider: 'Google',
        accessToken: googleAuth.accessToken!,
      );
    } catch (e) {
      if (e is ApiException) {
        return ApiResult.failure(e);
      }
      return ApiResult.failure(
        ApiException.unknown('Uventet fejl ved Google login: $e'),
      );
    }
  }

  /// Standard login
  Future<ApiResult<AuthResponseModel>> login({
    required String usernameOrEmail,
    required String password,
  }) async {
    return await _remoteDataSource.login(
      usernameOrEmail: usernameOrEmail,
      password: password,
    );
  }

  /// Registrer ny bruger
  Future<ApiResult<AuthResponseModel>> register({
    required String username,
    required String email,
    required String password,
  }) async {
    return await _remoteDataSource.register(
      username: username,
      email: email,
      password: password,
    );
  }

  /// Refresh JWT token
  Future<ApiResult<AuthResponseModel>> refreshToken(String refreshToken) async {
    return await _remoteDataSource.refreshToken(refreshToken);
  }

  /// Logout
  Future<ApiResult<void>> logout(String refreshToken) async {
    // Sign out fra Google
    await _googleSignIn.signOut();
    
    // Revoke refresh token på backend
    return await _remoteDataSource.logout(refreshToken);
  }

  /// Hent nuværende bruger
  Future<ApiResult<AuthResponseModel>> getCurrentUser() async {
    return await _remoteDataSource.getCurrentUser();
  }
}

