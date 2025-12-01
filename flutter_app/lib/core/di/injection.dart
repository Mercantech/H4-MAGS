import 'package:get_it/get_it.dart';
import '../../data/datasources/weather_remote_datasource.dart';
import '../../data/datasources/auth_remote_datasource.dart';
import '../../data/repositories/weather_repository_impl.dart';
import '../../data/repositories/auth_repository_impl.dart';
import '../../domain/repositories/weather_repository.dart';
import '../../features/weather/bloc/weather_bloc.dart';
import '../../features/auth/bloc/auth_bloc.dart';
import '../api/api_client.dart';
import '../config/google_config.dart';
import '../storage/auth_storage.dart';

/// Dependency Injection Container
/// 
/// Central sted til at registrere og resolve dependencies.
/// Bruger get_it som service locator.
/// 
/// Benefits:
/// - Single source of truth for dependencies
/// - Easy testing (mock dependencies)
/// - Loose coupling mellem komponenter
/// - Nem at skifte implementations
/// 
/// Usage:
/// ```dart
/// // I main.dart:
/// await setupDependencyInjection();
/// 
/// // I kode:
/// final weatherBloc = getIt<WeatherBloc>();
/// final repository = getIt<WeatherRepository>();
/// ```
final getIt = GetIt.instance;

/// Setup alle dependencies
/// 
/// Registrerer dependencies i den rigtige rækkefølge:
/// 1. Core services (ApiClient)
/// 2. Data sources
/// 3. Repositories
/// 4. BLoCs
/// 
/// Kaldes fra main.dart før app starter.
Future<void> setupDependencyInjection() async {
        // ============================================================
        // Core - Storage
        // ============================================================
        // Singleton fordi vi kun vil have én storage instance
        getIt.registerLazySingleton<AuthStorage>(
          () => AuthStorage(),
        );

        // ============================================================
        // Core - API Client
        // ============================================================
        // Singleton fordi vi kun vil have én API client instance
        // Tilføj AuthInterceptor der henter token fra storage
        getIt.registerLazySingleton<ApiClient>(
          () {
            final apiClient = ApiClient();
            // Tilføj auth interceptor der henter token fra secure storage
            apiClient.addAuthInterceptor(() => getIt<AuthStorage>().getToken());
            return apiClient;
          },
        );

  // ============================================================
  // Data Sources
  // ============================================================
  // Remote data sources
  getIt.registerLazySingleton<WeatherRemoteDataSource>(
    () => WeatherRemoteDataSourceImpl(
      apiClient: getIt<ApiClient>(),
    ),
  );

  getIt.registerLazySingleton<AuthRemoteDataSource>(
    () => AuthRemoteDataSource(
      apiClient: getIt<ApiClient>(),
    ),
  );

  // TODO: Tilføj local data source her når I implementerer caching
  // getIt.registerLazySingleton<WeatherLocalDataSource>(
  //   () => WeatherLocalDataSourceImpl(),
  // );

  // ============================================================
  // Repositories
  // ============================================================
  // Registrer som interface type (WeatherRepository)
  // så BLoC kun afhænger af interface, ikke implementation
  getIt.registerLazySingleton<WeatherRepository>(
    () => WeatherRepositoryImpl(
      remoteDataSource: getIt<WeatherRemoteDataSource>(),
      // localDataSource: getIt<WeatherLocalDataSource>(), // når caching tilføjes
    ),
  );

  getIt.registerLazySingleton<AuthRepositoryImpl>(
    () => AuthRepositoryImpl(
      remoteDataSource: getIt<AuthRemoteDataSource>(),
      webClientId: GoogleConfig.webClientId,
    ),
  );

  // TODO: Tilføj flere repositories her efterhånden:
  // getIt.registerLazySingleton<UserRepository>(
  //   () => UserRepositoryImpl(
  //     remoteDataSource: getIt<UserRemoteDataSource>(),
  //   ),
  // );

  // ============================================================
  // BLoCs
  // ============================================================
  // Factory fordi vi vil have ny instance hver gang
  // (BLoCs skal ikke deles mellem widgets)
  getIt.registerFactory<WeatherBloc>(
    () => WeatherBloc(
      repository: getIt<WeatherRepository>(),
    ),
  );

        getIt.registerFactory<AuthBloc>(
          () => AuthBloc(
            authRepository: getIt<AuthRepositoryImpl>(),
            authStorage: getIt<AuthStorage>(),
          ),
        );
}

/// Reset dependency injection
/// 
/// Nyttigt til testing hvor du vil starte med clean slate.
/// Kan også bruges til at skifte mellem mock og real dependencies.
Future<void> resetDependencyInjection() async {
  await getIt.reset();
}

/// Setup mock dependencies til testing
/// 
/// Eksempel på hvordan I kan lave test setup:
/// ```dart
/// Future<void> setupMockDependencies() async {
///   await resetDependencyInjection();
///   
///   // Register mocks
///   getIt.registerLazySingleton<WeatherRepository>(
///     () => MockWeatherRepository(),
///   );
///   
///   getIt.registerFactory<WeatherBloc>(
///     () => WeatherBloc(repository: getIt<WeatherRepository>()),
///   );
/// }
/// ```

