import '../models/auth/auth_response_model.dart';
import '../models/auth/google_login_request.dart';
import '../../core/api/api_client.dart';
import '../../core/api/api_result.dart';

/// Remote data source for authentication
/// 
/// Håndterer alle API calls relateret til authentication.
class AuthRemoteDataSource {
  final ApiClient _apiClient;

  AuthRemoteDataSource({required ApiClient apiClient})
      : _apiClient = apiClient;

  /// Login med Google ID token
  /// 
  /// Sender Google ID token til backend og modtager JWT token.
  Future<ApiResult<AuthResponseModel>> loginWithGoogle(String idToken) async {
    final request = GoogleLoginRequest(idToken: idToken);
    
    return await _apiClient.post<AuthResponseModel>(
      '/auth/google-login', // Fjernet /api da apiBaseUrl allerede indeholder det
      body: request.toJson(),
      fromJson: (json) => AuthResponseModel.fromJson(json as Map<String, dynamic>),
    );
  }

  /// Login med Google access token (workaround for Flutter Web)
  /// 
  /// Bruges når idToken ikke er tilgængelig, men access_token er.
  /// Backend henter brugerinfo direkte fra Google API.
  Future<ApiResult<AuthResponseModel>> loginWithGoogleAccessToken(String accessToken) async {
    return await _apiClient.post<AuthResponseModel>(
      '/auth/google-login-access-token', // Fjernet /api da apiBaseUrl allerede indeholder det
      body: {'accessToken': accessToken},
      fromJson: (json) => AuthResponseModel.fromJson(json as Map<String, dynamic>),
    );
  }

  /// Standard login med username/email og password
  Future<ApiResult<AuthResponseModel>> login({
    required String usernameOrEmail,
    required String password,
  }) async {
    return await _apiClient.post<AuthResponseModel>(
      '/auth/login', // Fjernet /api da apiBaseUrl allerede indeholder det
      body: {
        'usernameOrEmail': usernameOrEmail,
        'password': password,
      },
      fromJson: (json) => AuthResponseModel.fromJson(json as Map<String, dynamic>),
    );
  }

  /// Registrer ny bruger
  Future<ApiResult<AuthResponseModel>> register({
    required String username,
    required String email,
    required String password,
  }) async {
    return await _apiClient.post<AuthResponseModel>(
      '/auth/register', // Fjernet /api da apiBaseUrl allerede indeholder det
      body: {
        'username': username,
        'email': email,
        'password': password,
      },
      fromJson: (json) => AuthResponseModel.fromJson(json as Map<String, dynamic>),
    );
  }

  /// Refresh JWT token
  Future<ApiResult<AuthResponseModel>> refreshToken(String refreshToken) async {
    return await _apiClient.post<AuthResponseModel>(
      '/auth/refresh', // Fjernet /api da apiBaseUrl allerede indeholder det
      body: {
        'refreshToken': refreshToken,
      },
      fromJson: (json) => AuthResponseModel.fromJson(json as Map<String, dynamic>),
    );
  }

  /// Logout - revoke refresh token
  Future<ApiResult<void>> logout(String refreshToken) async {
    return await _apiClient.post<void>(
      '/auth/logout', // Fjernet /api da apiBaseUrl allerede indeholder det
      body: {
        'refreshToken': refreshToken,
      },
      fromJson: (_) => {},
    );
  }

  /// Hent nuværende bruger info
  Future<ApiResult<AuthResponseModel>> getCurrentUser() async {
    return await _apiClient.get<AuthResponseModel>(
      '/auth/me', // Fjernet /api da apiBaseUrl allerede indeholder det
      fromJson: (json) => AuthResponseModel.fromJson(json as Map<String, dynamic>),
    );
  }
}

