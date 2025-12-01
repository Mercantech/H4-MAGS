/// GitHub OAuth konfiguration
class GitHubConfig {
  /// GitHub OAuth App Client ID
  /// 
  /// Opret en OAuth App i GitHub Settings → Developer settings → OAuth Apps
  static const String clientId = 'Ov23liuASB3UvtR9nAFw';
  
  /// GitHub OAuth Authorization URL
  static const String authorizationUrl = 'https://github.com/login/oauth/authorize';
  
  /// GitHub OAuth Scopes
  /// 
  /// Scopes der er nødvendige for at hente brugerinfo:
  /// - user:email: Hent bruger email
  static const List<String> scopes = ['user:email'];
  
  /// Backend callback URL
  /// 
  /// Denne URL skal matche GitHub OAuth App callback URL konfiguration PRÆCIST
  /// Bemærk: Casing er vigtigt - skal være /api/Auth/ for at matche controller route
  static String getCallbackUrl(String baseUrl) {
    return '$baseUrl/api/Auth/github/callback';
  }
}

