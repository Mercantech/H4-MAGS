import 'package:dio/dio.dart';
import '../config/app_config.dart';

/// Logging Interceptor til Dio
/// 
/// Logger alle API requests og responses når enableApiLogging er true.
/// Hjælper med debugging under udvikling.
/// 
/// Features:
/// - Logger request method, URL, headers, og body
/// - Logger response status, data, og timing
/// - Logger fejl med detaljer
/// - Pretty printing for bedre læsbarhed
/// - Conditional logging baseret på environment
class LoggingInterceptor extends Interceptor {
  final bool enableLogging;

  LoggingInterceptor({bool? enableLogging})
      : enableLogging = enableLogging ?? AppConfig.instance.enableApiLogging;

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    if (enableLogging) {
      print('╔═══════════════════════════════════════════════════════════════');
      print('║ 🚀 REQUEST');
      print('╠═══════════════════════════════════════════════════════════════');
      print('║ Method: ${options.method}');
      print('║ URL: ${options.uri}');
      
      if (options.headers.isNotEmpty) {
        print('║ Headers:');
        options.headers.forEach((key, value) {
          print('║   $key: $value');
        });
      }
      
      if (options.data != null) {
        print('║ Body: ${options.data}');
      }
      
      if (options.queryParameters.isNotEmpty) {
        print('║ Query Parameters: ${options.queryParameters}');
      }
      
      print('╚═══════════════════════════════════════════════════════════════\n');
    }
    
    super.onRequest(options, handler);
  }

  @override
  void onResponse(Response response, ResponseInterceptorHandler handler) {
    if (enableLogging) {
      print('╔═══════════════════════════════════════════════════════════════');
      print('║ ✅ RESPONSE');
      print('╠═══════════════════════════════════════════════════════════════');
      print('║ Status: ${response.statusCode}');
      print('║ URL: ${response.requestOptions.uri}');
      print('║ Data: ${response.data}');
      print('╚═══════════════════════════════════════════════════════════════\n');
    }
    
    super.onResponse(response, handler);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    if (enableLogging) {
      print('╔═══════════════════════════════════════════════════════════════');
      print('║ ❌ ERROR');
      print('╠═══════════════════════════════════════════════════════════════');
      print('║ Type: ${err.type}');
      print('║ URL: ${err.requestOptions.uri}');
      print('║ Status: ${err.response?.statusCode}');
      print('║ Message: ${err.message}');
      
      if (err.response?.data != null) {
        print('║ Response Data: ${err.response?.data}');
      }
      
      print('╚═══════════════════════════════════════════════════════════════\n');
    }
    
    super.onError(err, handler);
  }
}

/// Auth Interceptor til Dio
/// 
/// Tilføjer automatisk authorization headers til alle requests.
/// Kan nemt udvides til at håndtere token refresh.
/// 
/// Usage:
/// ```dart
/// dio.interceptors.add(AuthInterceptor(tokenProvider: () => getToken()));
/// ```
class AuthInterceptor extends Interceptor {
  /// Callback der returnerer current auth token
  final Future<String?> Function() tokenProvider;

  AuthInterceptor({required this.tokenProvider});

  @override
  void onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    // Hent token fra token provider
    final token = await tokenProvider();
    
    if (token != null && token.isNotEmpty) {
      // Tilføj Bearer token til Authorization header
      options.headers['Authorization'] = 'Bearer $token';
      print('🔑 [DEBUG] Token tilføjet til request: ${options.path}');
    } else {
      print('⚠️ [DEBUG] Ingen token fundet for request: ${options.path}');
    }
    
    super.onRequest(options, handler);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    // TODO: Implementer token refresh logic her hvis nødvendigt
    // Hvis 401 Unauthorized, prøv at refresh token og retry request
    
    if (err.response?.statusCode == 401) {
      // Log user out eller refresh token
      print('⚠️ Unauthorized request - token might be expired');
    }
    
    super.onError(err, handler);
  }
}

/// Retry Interceptor
/// 
/// Prøver automatisk at gentage failed requests et antal gange.
/// Nyttigt ved midlertidige netværksfejl.
class RetryInterceptor extends Interceptor {
  final int maxRetries;
  final Duration retryDelay;

  RetryInterceptor({
    this.maxRetries = 3,
    this.retryDelay = const Duration(seconds: 1),
  });

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    final extra = err.requestOptions.extra;
    final retries = extra['retries'] ?? 0;

    // Check om vi skal retry
    if (retries < maxRetries && _shouldRetry(err)) {
      print('🔄 Retrying request... (attempt ${retries + 1}/$maxRetries)');
      
      // Vent før retry
      await Future.delayed(retryDelay);
      
      // Opdater retry count
      err.requestOptions.extra['retries'] = retries + 1;
      
      // Retry request
      try {
        final response = await Dio().fetch(err.requestOptions);
        return handler.resolve(response);
      } catch (e) {
        return super.onError(err, handler);
      }
    }
    
    super.onError(err, handler);
  }

  /// Bestem om vi skal retry baseret på fejl type
  bool _shouldRetry(DioException err) {
    return err.type == DioExceptionType.connectionTimeout ||
        err.type == DioExceptionType.sendTimeout ||
        err.type == DioExceptionType.receiveTimeout ||
        (err.response?.statusCode ?? 0) >= 500;
  }
}

