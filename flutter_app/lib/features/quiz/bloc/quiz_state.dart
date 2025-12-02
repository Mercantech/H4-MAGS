import 'package:equatable/equatable.dart';
import '../../../data/models/quiz/session_model.dart';
import '../../../data/models/quiz/participant_model.dart';
import '../../../data/models/quiz/quiz_model.dart';
import '../../../data/models/quiz/quiz_summary_model.dart';

/// Quiz States
/// 
/// States for quiz deltagelse flow.
abstract class QuizState extends Equatable {
  const QuizState();

  @override
  List<Object?> get props => [];
}

/// Initial state - klar til at indtaste PIN
class QuizInitial extends QuizState {
  const QuizInitial();
}

/// Loading state - venter på API response
class QuizLoading extends QuizState {
  const QuizLoading();
}

/// Session fundet - viser session info
class QuizSessionFound extends QuizState {
  final SessionModel session;

  const QuizSessionFound({required this.session});

  @override
  List<Object?> get props => [session];
}

/// Deltager har joinet session
class QuizJoined extends QuizState {
  final SessionModel session;
  final ParticipantModel participant;

  const QuizJoined({
    required this.session,
    required this.participant,
  });

  @override
  List<Object?> get props => [session, participant];
}

/// Error state
class QuizError extends QuizState {
  final String message;

  const QuizError({required this.message});

  @override
  List<Object?> get props => [message];
}

/// Quiz oprettet
class QuizCreated extends QuizState {
  final QuizModel quiz;

  const QuizCreated({required this.quiz});

  @override
  List<Object?> get props => [quiz];
}

/// Session oprettet
class SessionCreated extends QuizState {
  final SessionModel session;

  const SessionCreated({required this.session});

  @override
  List<Object?> get props => [session];
}

/// Session startet
class SessionStarted extends QuizState {
  final SessionModel? session;

  const SessionStarted({this.session});

  @override
  List<Object?> get props => [session];
}

/// Alle quizzers indlæst
class AllQuizzesLoaded extends QuizState {
  final List<QuizSummaryModel> quizzes;

  const AllQuizzesLoaded({required this.quizzes});

  @override
  List<Object?> get props => [quizzes];
}

