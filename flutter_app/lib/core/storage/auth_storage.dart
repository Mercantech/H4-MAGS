import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'dart:convert';
import '../../data/models/auth/auth_response_model.dart';

/// Secure Storage for Authentication
/// 
/// Håndterer gemning og hentning af JWT tokens i secure storage.
/// Bruger flutter_secure_storage som krypterer data på enhedens secure keychain/keystore.
/// 
/// Features:
/// - Gemmer JWT token og refresh token
/// - Gemmer fuld auth response (inkl. user info)
/// - Automatisk kryptering via platform secure storage
/// - Thread-safe operations
class AuthStorage {
  static const _storage = FlutterSecureStorage();

  // Storage keys
  static const String _keyToken = 'auth_token';
  static const String _keyRefreshToken = 'auth_refresh_token';
  static const String _keyAuthResponse = 'auth_response';
  static const String _keyExpires = 'auth_expires';

  /// Gem authentication response
  Future<void> saveAuthResponse(AuthResponseModel authResponse) async {
    try {
      await Future.wait([
        _storage.write(key: _keyToken, value: authResponse.token),
        _storage.write(key: _keyRefreshToken, value: authResponse.refreshToken),
        _storage.write(
          key: _keyExpires,
          value: authResponse.expires.toIso8601String(),
        ),
        _storage.write(
          key: _keyAuthResponse,
          value: json.encode(authResponse.toJson()),
        ),
      ]);
    } catch (e) {
      throw Exception('Fejl ved gemning af auth data: $e');
    }
  }

  /// Hent JWT token
  Future<String?> getToken() async {
    try {
      return await _storage.read(key: _keyToken);
    } catch (e) {
      return null;
    }
  }

  /// Hent refresh token
  Future<String?> getRefreshToken() async {
    try {
      return await _storage.read(key: _keyRefreshToken);
    } catch (e) {
      return null;
    }
  }

  /// Hent fuld authentication response
  Future<AuthResponseModel?> getAuthResponse() async {
    try {
      final jsonString = await _storage.read(key: _keyAuthResponse);
      if (jsonString == null) return null;

      final jsonData = json.decode(jsonString) as Map<String, dynamic>;
      return AuthResponseModel.fromJson(jsonData);
    } catch (e) {
      return null;
    }
  }

  /// Tjek om token er gyldig (ikke udløbet)
  Future<bool> isTokenValid() async {
    try {
      final expiresString = await _storage.read(key: _keyExpires);
      if (expiresString == null) return false;

      final expires = DateTime.parse(expiresString);
      return expires.isAfter(DateTime.now());
    } catch (e) {
      return false;
    }
  }

  /// Slet alle authentication data
  Future<void> clearAuth() async {
    try {
      await Future.wait([
        _storage.delete(key: _keyToken),
        _storage.delete(key: _keyRefreshToken),
        _storage.delete(key: _keyAuthResponse),
        _storage.delete(key: _keyExpires),
      ]);
    } catch (e) {
      // Ignorer fejl ved sletning
    }
  }

  /// Tjek om bruger er logget ind
  Future<bool> isAuthenticated() async {
    final token = await getToken();
    if (token == null) return false;
    
    return await isTokenValid();
  }
}

