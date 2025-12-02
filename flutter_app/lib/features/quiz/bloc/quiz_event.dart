import 'package:equatable/equatable.dart';
import '../../../data/models/quiz/create_quiz_model.dart';

/// Quiz Events
/// 
/// Events der trigger state changes i QuizBloc.
abstract class QuizEvent extends Equatable {
  const QuizEvent();

  @override
  List<Object?> get props => [];
}

/// Join quiz session med PIN
class JoinSessionEvent extends QuizEvent {
  final String sessionPin;
  final String nickname;

  const JoinSessionEvent({
    required this.sessionPin,
    required this.nickname,
  });

  @override
  List<Object?> get props => [sessionPin, nickname];
}

/// Hent session info via PIN
class GetSessionByPinEvent extends QuizEvent {
  final String pin;

  const GetSessionByPinEvent({required this.pin});

  @override
  List<Object?> get props => [pin];
}

/// Reset quiz state
class ResetQuizEvent extends QuizEvent {
  const ResetQuizEvent();
}

/// Opret quiz
class CreateQuizEvent extends QuizEvent {
  final CreateQuizModel quiz;

  const CreateQuizEvent({required this.quiz});

  @override
  List<Object?> get props => [quiz];
}

/// Opret session
class CreateSessionEvent extends QuizEvent {
  final int quizId;

  const CreateSessionEvent({required this.quizId});

  @override
  List<Object?> get props => [quizId];
}

/// Start session
class StartSessionEvent extends QuizEvent {
  final int sessionId;

  const StartSessionEvent({required this.sessionId});

  @override
  List<Object?> get props => [sessionId];
}

/// Hent alle quizzers
class LoadAllQuizzesEvent extends QuizEvent {
  const LoadAllQuizzesEvent();
}

