/// [Google Auth] Request model til POST /auth/oauth-login – provider (fx "Google") + accessToken.
class OAuthLoginRequest {
  /// OAuth provider navn (f.eks. "Google", "Microsoft", "GitHub")
  final String provider;
  
  /// Access token fra OAuth provider
  final String accessToken;

  OAuthLoginRequest({
    required this.provider,
    required this.accessToken,
  });

  Map<String, dynamic> toJson() {
    return {
      'provider': provider,
      'accessToken': accessToken,
    };
  }
}

