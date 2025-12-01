import '../models/auth/auth_response_model.dart';
import '../models/auth/oauth_login_request.dart';
import '../../core/api/api_client.dart';
import '../../core/api/api_result.dart';

/// Remote data source for authentication
/// 
/// Håndterer alle API calls relateret til authentication.
class AuthRemoteDataSource {
  final ApiClient _apiClient;

  AuthRemoteDataSource({required ApiClient apiClient})
      : _apiClient = apiClient;

  /// Generisk OAuth login - Virker med alle providers (Google, Microsoft, GitHub, etc.)
  /// 
  /// Bruger det nye generiske /oauth-login endpoint.
  /// Anbefalet metode til OAuth login.
  Future<ApiResult<AuthResponseModel>> loginWithOAuth({
    required String provider,
    required String accessToken,
  }) async {
    final request = OAuthLoginRequest(
      provider: provider,
      accessToken: accessToken,
    );
    
    return await _apiClient.post<AuthResponseModel>(
      '/auth/oauth-login',
      body: request.toJson(),
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

  /// Opdater password for nuværende bruger
  /// Virker både for OAuth brugere (tilføjer password) og normale brugere (opdaterer password)
  Future<ApiResult<void>> updatePassword(String newPassword) async {
    return await _apiClient.post<void>(
      '/auth/update-password',
      body: {
        'newPassword': newPassword,
      },
      fromJson: (_) => {},
    );
  }
}

