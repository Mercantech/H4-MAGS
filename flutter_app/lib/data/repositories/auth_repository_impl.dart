import 'dart:async';
import 'dart:convert';
import 'dart:html' as html;
import 'package:google_sign_in/google_sign_in.dart';
import '../datasources/auth_remote_datasource.dart';
import '../models/auth/auth_response_model.dart';
import '../../core/api/api_result.dart';
import '../../core/config/github_config.dart';
import '../../core/config/app_config.dart';

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

  /// Login med GitHub via OAuth flow (popup)
  /// 
  /// Åbner GitHub OAuth popup og håndterer hele flowet automatisk.
  /// Flow:
  /// 1. Åbner GitHub OAuth popup
  /// 2. Brugeren godkender app på GitHub
  /// 3. GitHub redirecter til backend callback
  /// 4. Backend exchange code for access token
  /// 5. Backend logger brugeren ind og returnerer JWT token
  Future<ApiResult<AuthResponseModel>> loginWithGitHub() async {
    try {
      // Generer random state for security
      final state = DateTime.now().millisecondsSinceEpoch.toString();
      
      // Byg GitHub OAuth URL
      // Fjern /api fra apiBaseUrl for at få base URL (f.eks. https://kahoot-api.mercantec.tech)
      final baseUrl = AppConfig.instance.apiBaseUrl.replaceAll('/api', '');
      final callbackUrl = GitHubConfig.getCallbackUrl(baseUrl);
      final scopes = GitHubConfig.scopes.join(' ');
      final authUrl = '${GitHubConfig.authorizationUrl}'
          '?client_id=${GitHubConfig.clientId}'
          '&redirect_uri=${Uri.encodeComponent(callbackUrl)}'
          '&scope=${Uri.encodeComponent(scopes)}'
          '&state=$state';

      print('🔍 [DEBUG] Åbner GitHub OAuth popup...');
      print('📍 [DEBUG] API Base URL: ${AppConfig.instance.apiBaseUrl}');
      print('📍 [DEBUG] Base URL (efter /api fjernet): $baseUrl');
      print('📍 [DEBUG] Callback URL: $callbackUrl');
      print('📍 [DEBUG] Full Auth URL: $authUrl');
      
      // Åbn popup window (Flutter Web)
      final popup = html.window.open(
        authUrl.toString(),
        'GitHub OAuth',
        'width=600,height=700,scrollbars=yes,resizable=yes',
      );

      if (popup == null) {
        return ApiResult.failure(
          ApiException.unknown('Kunne ikke åbne popup. Tjek popup-blocker indstillinger.'),
        );
      }

      // Vent på callback via postMessage
      final completer = Completer<ApiResult<AuthResponseModel>>();
      
      // Lyt til postMessage fra callback page
      html.window.addEventListener('message', (event) {
        final message = event as html.MessageEvent;
        
        print('📥 [DEBUG] PostMessage modtaget: ${message.data}');
        print('📥 [DEBUG] Message type: ${message.data.runtimeType}');
        
        // Tjek om det er GitHub OAuth callback
        if (message.data is Map) {
          final messageData = message.data as Map;
          print('📥 [DEBUG] Message data type: ${messageData['type']}');
          
          if (messageData['type'] == 'github_oauth_success') {
            final data = messageData['data'];
            print('📥 [DEBUG] Auth data type: ${data.runtimeType}');
            
            try {
              // Håndter både Map og String (hvis JSON er serialiseret som string)
              Map<String, dynamic> dataMap;
              if (data is String) {
                print('📥 [DEBUG] Data er string, parser JSON...');
                dataMap = Map<String, dynamic>.from(jsonDecode(data));
              } else if (data is Map) {
                print('📥 [DEBUG] Data er Map (type: ${data.runtimeType}), konverterer...');
                // Konverter LinkedMap eller Map<dynamic, dynamic> til Map<String, dynamic>
                // Brug JSON serialization for at sikre korrekt konvertering
                final jsonString = jsonEncode(data);
                dataMap = Map<String, dynamic>.from(jsonDecode(jsonString));
                print('📥 [DEBUG] Data konverteret til Map<String, dynamic>');
              } else {
                throw Exception('Uventet data type: ${data.runtimeType}');
              }
              
              final authResponse = AuthResponseModel.fromJson(dataMap);
              print('✅ [DEBUG] Auth response parsed successfully');
              
              if (!completer.isCompleted) {
                completer.complete(ApiResult.success(authResponse));
              }
              
              // Vent lidt før popup lukker så brugeren kan se success
              Future.delayed(const Duration(milliseconds: 500), () {
                popup.close();
              });
            } catch (e, stackTrace) {
              print('❌ [DEBUG] Fejl ved parsing: $e');
              print('❌ [DEBUG] Stack trace: $stackTrace');
              if (!completer.isCompleted) {
                completer.complete(ApiResult.failure(
                  ApiException.unknown('Fejl ved parsing af auth response: $e'),
                ));
              }
              popup.close();
            }
          }
        }
      });

      // Tjek om popup blev lukket (bruger cancelled)
      Timer.periodic(const Duration(milliseconds: 500), (timer) {
        if (popup.closed ?? false) {
          timer.cancel();
          if (!completer.isCompleted) {
            completer.complete(ApiResult.failure(
              ApiException.unknown('GitHub login blev annulleret'),
            ));
          }
        }
      });

      // Vent på resultat med timeout
      return await completer.future.timeout(
        const Duration(minutes: 5),
        onTimeout: () {
          popup.close();
          return ApiResult.failure(
            ApiException.unknown('GitHub login timeout. Prøv igen.'),
          );
        },
      );
    } catch (e) {
      if (e is ApiException) {
        return ApiResult.failure(e);
      }
      return ApiResult.failure(
        ApiException.unknown('Uventet fejl ved GitHub login: $e'),
      );
    }
  }

  /// Login med GitHub via access token (manual/fallback)
  /// 
  /// Bruges hvis du har en GitHub Personal Access Token direkte.
  Future<ApiResult<AuthResponseModel>> loginWithGitHubToken(String accessToken) async {
    try {
      if (accessToken.isEmpty) {
        return ApiResult.failure(
          ApiException.unknown('GitHub access token er påkrævet'),
        );
      }

      // Brug generisk OAuth endpoint - virker perfekt med GitHub!
      return await _remoteDataSource.loginWithOAuth(
        provider: 'GitHub',
        accessToken: accessToken,
      );
    } catch (e) {
      if (e is ApiException) {
        return ApiResult.failure(e);
      }
      return ApiResult.failure(
        ApiException.unknown('Uventet fejl ved GitHub login: $e'),
      );
    }
  }

  /// Hent nuværende bruger
  Future<ApiResult<AuthResponseModel>> getCurrentUser() async {
    return await _remoteDataSource.getCurrentUser();
  }
}

