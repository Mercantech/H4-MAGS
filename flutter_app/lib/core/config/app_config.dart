/// App Configuration Manager
/// 
/// Håndterer environment-specifik konfiguration og giver nem adgang til
/// API URLs, timeouts, og andre indstillinger baseret på valgt miljø.
/// 
/// Usage:
/// ```dart
/// // Initialisér med environment
/// await AppConfig.initialize(Environment.development);
/// 
/// // Brug i kode
/// final baseUrl = AppConfig.instance.apiBaseUrl;
/// ```
class AppConfig {
  static AppConfig? _instance;
  
  final Environment environment;
  final String apiBaseUrl;
  final int apiTimeout;
  final bool enableApiLogging;
  final bool enableErrorLogging;

  AppConfig._({
    required this.environment,
    required this.apiBaseUrl,
    required this.apiTimeout,
    required this.enableApiLogging,
    required this.enableErrorLogging,
  });

  /// Singleton instance
  static AppConfig get instance {
    if (_instance == null) {
      throw Exception(
        'AppConfig er ikke initialiseret! '
        'Kald AppConfig.initialize() før brug.'
      );
    }
    return _instance!;
  }

  /// Initialisér app konfiguration
  /// 
  /// Kaldes fra main.dart før app starter:
  /// ```dart
  /// await AppConfig.initialize(Environment.development);
  /// ```
  static Future<void> initialize(Environment env) async {
    _instance = AppConfig._(
      environment: env,
      apiBaseUrl: env.apiBaseUrl,
      apiTimeout: env.apiTimeout,
      enableApiLogging: env.enableApiLogging,
      enableErrorLogging: env.enableErrorLogging,
    );
  }

  /// Check om vi kører i development mode
  bool get isDevelopment => environment == Environment.development;
  
  /// Check om vi kører i production mode
  bool get isProduction => environment == Environment.production;
}

/// Environment konfigurationer
/// 
/// Definer forskellige miljøer med deres specifikke indstillinger.
/// Nem at udvide med flere miljøer (staging, test, osv.)
class Environment {
  final String name;
  final String apiBaseUrl;
  final int apiTimeout;
  final bool enableApiLogging;
  final bool enableErrorLogging;

  const Environment._({
    required this.name,
    required this.apiBaseUrl,
    required this.apiTimeout,
    required this.enableApiLogging,
    required this.enableErrorLogging,
  });

  /// Development environment (localhost)
  /// Brug når du udvikler lokalt og API kører på din maskine
  /// 
  static const development = Environment._(
    name: 'development',
    apiBaseUrl: 'https://localhost:7258/api', 
    apiTimeout: 30000, // 30 sekunder
    enableApiLogging: true,
    enableErrorLogging: true,
  );

  /// Production environment (deployed)
  /// Brug til deployed version af API
  static const production = Environment._(
    name: 'production',
    apiBaseUrl: 'https://h4-api.mercantec.tech/api',
    apiTimeout: 30000, // 30 sekunder
    enableApiLogging: false, // Slå logging fra i produktion for performance
    enableErrorLogging: true,
  );

  /// Staging environment (optional)
  /// Kan tilføjes senere hvis I får et staging miljø
  static const staging = Environment._(
    name: 'staging',
    apiBaseUrl: 'https://h4-api-staging.mercantec.tech/api',
    apiTimeout: 30000,
    enableApiLogging: true,
    enableErrorLogging: true,
  );

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is Environment &&
          runtimeType == other.runtimeType &&
          name == other.name;

  @override
  int get hashCode => name.hashCode;
}

