import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../core/storage/auth_storage.dart';
import '../../../data/repositories/auth_repository_impl.dart';
import 'auth_event.dart';
import 'auth_state.dart';

/// Authentication BLoC
/// 
/// Håndterer authentication state management og Google Sign-In flow.
/// Gemmer tokens i secure storage for persistence efter reload.
class AuthBloc extends Bloc<AuthEvent, AuthState> {
  final AuthRepositoryImpl _authRepository;
  final AuthStorage _authStorage;

  AuthBloc({
    required AuthRepositoryImpl authRepository,
    required AuthStorage authStorage,
  })  : _authRepository = authRepository,
        _authStorage = authStorage,
        super(const AuthInitial()) {
    on<LoginWithGoogleEvent>(_onLoginWithGoogle);
    on<LogoutEvent>(_onLogout);
    on<CheckAuthStatusEvent>(_onCheckAuthStatus);
  }

  /// Login med Google
  Future<void> _onLoginWithGoogle(
    LoginWithGoogleEvent event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());

    final result = await _authRepository.loginWithGoogle();

    // Håndter resultatet eksplicit i stedet for at bruge when() med async callbacks
    if (result.isSuccess) {
      final authResponse = result.dataOrNull!;
      
      // Gem tokens i secure storage FØR vi emitter state
      try {
        await _authStorage.saveAuthResponse(authResponse);
      } catch (e) {
        // Hvis gemning fejler, log fejl men fortsæt
        print('⚠️ [DEBUG] Kunne ikke gemme tokens: $e');
      }
      
      // Tjek om emitter stadig er aktiv før vi emitter
      if (!emit.isDone) {
        emit(AuthAuthenticated(
          authResponse: authResponse,
          user: authResponse.user,
        ));
      }
    } else {
      final error = result.exceptionOrNull!;
      if (!emit.isDone) {
        emit(AuthError(error.userMessage));
      }
    }
  }

  /// Logout
  Future<void> _onLogout(
    LogoutEvent event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());
    
    // Hvis vi har en refresh token, prøv at revoke den på backend
    if (state is AuthAuthenticated) {
      final currentState = state as AuthAuthenticated;
      try {
        await _authRepository.logout(currentState.authResponse.refreshToken);
      } catch (e) {
        // Ignorer fejl ved logout - vi sletter tokens alligevel
        print('⚠️ [DEBUG] Kunne ikke revoke refresh token: $e');
      }
    }
    
    // Slet alle tokens fra storage
    await _authStorage.clearAuth();
    
    emit(const AuthUnauthenticated());
  }

  /// Check authentication status
  /// 
  /// Tjekker om bruger er logget ind ved at læse token fra secure storage.
  /// Hvis token findes og er gyldig, sættes authenticated state.
  Future<void> _onCheckAuthStatus(
    CheckAuthStatusEvent event,
    Emitter<AuthState> emit,
  ) async {
    emit(const AuthLoading());
    
    try {
      // Tjek om token eksisterer og er gyldig
      final isAuthenticated = await _authStorage.isAuthenticated();
      
      if (isAuthenticated) {
        // Hent auth response fra storage
        final authResponse = await _authStorage.getAuthResponse();
        
        if (authResponse != null) {
          // Tjek om token stadig er gyldig (ikke udløbet)
          if (authResponse.expires.isAfter(DateTime.now())) {
            emit(AuthAuthenticated(
              authResponse: authResponse,
              user: authResponse.user,
            ));
            return;
          } else {
            // Token er udløbet - prøv at refresh
            final refreshToken = await _authStorage.getRefreshToken();
            if (refreshToken != null) {
              final refreshResult = await _authRepository.refreshToken(refreshToken);
              refreshResult.when(
                success: (newAuthResponse) async {
                  // Gem nye tokens
                  await _authStorage.saveAuthResponse(newAuthResponse);
                  emit(AuthAuthenticated(
                    authResponse: newAuthResponse,
                    user: newAuthResponse.user,
                  ));
                },
                failure: (_) {
                  // Refresh fejlede - slet tokens og log ud
                  _authStorage.clearAuth();
                  emit(const AuthUnauthenticated());
                },
              );
              return;
            }
          }
        }
      }
      
      // Ingen gyldig token fundet
      emit(const AuthUnauthenticated());
    } catch (e) {
      print('❌ [DEBUG] Fejl ved tjek af auth status: $e');
      emit(const AuthUnauthenticated());
    }
  }
}

