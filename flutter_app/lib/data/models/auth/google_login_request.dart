/// Request model for Google login
class GoogleLoginRequest {
  final String idToken;

  GoogleLoginRequest({
    required this.idToken,
  });

  Map<String, dynamic> toJson() {
    return {
      'idToken': idToken,
    };
  }
}

