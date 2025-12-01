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

  /// Login med Google
  /// 
  /// Workaround for Flutter Web idToken problem:
  /// 1. Prøver signInSilently() først (virker hvis bruger allerede er logget ind)
  /// 2. Hvis det fejler, logger vi ud og bruger signIn() med requestScopes()
  /// 3. requestScopes() tvinger en ny consent prompt, hvilket kan give idToken
  /// 4. Henter ID token fra Google
  /// 5. Sender ID token til backend
  /// 
  /// Note: Dette er en workaround. Den bedste løsning er at migrere til renderButton()
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

      // DEBUG: Log hvad vi får fra Google
      print('🔍 [DEBUG] Google Auth Details:');
      print('   - Has idToken: ${googleAuth.idToken != null}');
      print('   - Has accessToken: ${googleAuth.accessToken != null}');
      if (googleAuth.idToken != null) {
        print('   - idToken length: ${googleAuth.idToken!.length}');
        print('   - idToken preview: ${googleAuth.idToken!.substring(0, 50)}...');
      }
      if (googleAuth.accessToken != null) {
        print('   - accessToken length: ${googleAuth.accessToken!.length}');
        print('   - accessToken preview: ${googleAuth.accessToken!.substring(0, 50)}...');
      }

      // WORKAROUND: På Flutter Web får vi ofte kun access_token, ikke idToken
      // Vi bruger access_token til at hente brugerinfo fra Google API på backend
      if (googleAuth.idToken != null) {
        // Hvis vi har idToken, brug det (bedste løsning)
        print('✅ [DEBUG] Bruger idToken til login');
        return await _remoteDataSource.loginWithGoogle(googleAuth.idToken!);
      } else if (googleAuth.accessToken != null) {
        // Hvis vi kun har access_token, brug det til alternativt endpoint
        print('⚠️ [DEBUG] idToken mangler, bruger access_token i stedet');
        print('📤 [DEBUG] Sender access_token til backend...');
        final result = await _remoteDataSource.loginWithGoogleAccessToken(googleAuth.accessToken!);
        print('📥 [DEBUG] Modtog svar fra backend: ${result.isSuccess ? "SUCCESS" : "FAILURE"}');
        if (result.isFailure) {
          print('❌ [DEBUG] Fejl: ${result.exceptionOrNull?.message}');
        }
        return result;
      } else {
        // Hvis vi ikke har nogen token, prøv at hente det igen
        print('⏳ [DEBUG] Ingen token fundet, venter og prøver igen...');
        await Future.delayed(const Duration(milliseconds: 1000));
        googleAuth = await googleUser.authentication;
        
        print('🔍 [DEBUG] Efter retry:');
        print('   - Has idToken: ${googleAuth.idToken != null}');
        print('   - Has accessToken: ${googleAuth.accessToken != null}');
        
        if (googleAuth.idToken != null) {
          print('✅ [DEBUG] Fandt idToken efter retry');
          return await _remoteDataSource.loginWithGoogle(googleAuth.idToken!);
        } else if (googleAuth.accessToken != null) {
          print('⚠️ [DEBUG] Fandt access_token efter retry');
          return await _remoteDataSource.loginWithGoogleAccessToken(googleAuth.accessToken!);
        }
        
        print('❌ [DEBUG] Ingen token fundet efter retry');
        return ApiResult.failure(
          ApiException.unknown(
            'Kunne ikke hente token fra Google. '
            'Prøv at logge ud og ind igen.'
          ),
        );
      }
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

