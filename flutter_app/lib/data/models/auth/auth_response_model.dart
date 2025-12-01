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
    return AuthResponseModel(
      token: json['token'] as String,
      refreshToken: json['refreshToken'] as String,
      expires: DateTime.parse(json['expires'] as String),
      user: UserModel.fromJson(json['user'] as Map<String, dynamic>),
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

