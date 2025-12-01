import 'package:equatable/equatable.dart';
import '../../../data/models/auth/auth_response_model.dart';
import '../../../data/models/auth/user_model.dart';

/// Authentication States
/// 
/// States der repræsenterer forskellige authentication tilstande.
abstract class AuthState extends Equatable {
  const AuthState();

  @override
  List<Object?> get props => [];
}

/// Initial state - ikke logget ind
class AuthInitial extends AuthState {
  const AuthInitial();
}

/// Loading state - authentication i gang
class AuthLoading extends AuthState {
  const AuthLoading();
}

/// Authenticated state - bruger er logget ind
class AuthAuthenticated extends AuthState {
  final AuthResponseModel authResponse;
  final UserModel user;

  const AuthAuthenticated({
    required this.authResponse,
    required this.user,
  });

  @override
  List<Object?> get props => [authResponse, user];
}

/// Unauthenticated state - ikke logget ind
class AuthUnauthenticated extends AuthState {
  const AuthUnauthenticated();
}

/// Error state - authentication fejlede
class AuthError extends AuthState {
  final String message;

  const AuthError(this.message);

  @override
  List<Object?> get props => [message];
}

