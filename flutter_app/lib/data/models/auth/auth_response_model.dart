import 'dart:convert';
import 'user_model.dart';

/// Authentication response model
class AuthResponseModel {
  final String token;
  final String refreshToken;
  final DateTime expires;
  final UserModel user;

  AuthResponseModel({
    required this.token,
    required this.refreshToken,
    required this.expires,
    required this.user,
  });

  factory AuthResponseModel.fromJson(Map<String, dynamic> json) {
    // Hjælpemetode til at konvertere LinkedMap til Map<String, dynamic>
    Map<String, dynamic> convertMap(dynamic data) {
      if (data is Map) {
        // Konverter via JSON for at sikre korrekt type (håndterer LinkedMap)
        final jsonString = jsonEncode(data);
        return Map<String, dynamic>.from(jsonDecode(jsonString));
      }
      throw Exception('Data is not a Map: ${data.runtimeType}');
    }
    
    // Konverter user objekt hvis det er LinkedMap (fra JavaScript interop)
    final userMap = convertMap(json['user']);
    
    return AuthResponseModel(
      token: json['token'] as String,
      refreshToken: json['refreshToken'] as String,
      expires: DateTime.parse(json['expires'] as String),
      user: UserModel.fromJson(userMap),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'token': token,
      'refreshToken': refreshToken,
      'expires': expires.toIso8601String(),
      'user': user.toJson(),
    };
  }
}

