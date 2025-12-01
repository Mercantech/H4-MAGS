/// Generisk OAuth login request model
/// 
/// Bruges til at sende OAuth access token til backend
/// Virker med alle providers (Google, Microsoft, GitHub, etc.)
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

