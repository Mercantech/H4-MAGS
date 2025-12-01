import 'package:equatable/equatable.dart';

/// Authentication Events
/// 
/// Events der trigger authentication actions i AuthBloc.
abstract class AuthEvent extends Equatable {
  const AuthEvent();

  @override
  List<Object?> get props => [];
}

/// Login med Google
class LoginWithGoogleEvent extends AuthEvent {
  const LoginWithGoogleEvent();
}

/// Login med GitHub (OAuth popup)
class LoginWithGitHubEvent extends AuthEvent {
  const LoginWithGitHubEvent();
}

/// Logout
class LogoutEvent extends AuthEvent {
  const LogoutEvent();
}

/// Check om bruger er logget ind
class CheckAuthStatusEvent extends AuthEvent {
  const CheckAuthStatusEvent();
}

/// Opdater password for nuværende bruger
class UpdatePasswordEvent extends AuthEvent {
  final String newPassword;

  const UpdatePasswordEvent(this.newPassword);

  @override
  List<Object?> get props => [newPassword];
}

