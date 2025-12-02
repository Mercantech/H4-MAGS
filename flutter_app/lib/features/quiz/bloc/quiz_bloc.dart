import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../data/repositories/quiz_repository_impl.dart';
import 'quiz_event.dart';
import 'quiz_state.dart';

/// Quiz BLoC
/// 
/// Håndterer quiz session deltagelse state management.
class QuizBloc extends Bloc<QuizEvent, QuizState> {
  final QuizRepositoryImpl _quizRepository;

  QuizBloc({required QuizRepositoryImpl quizRepository})
      : _quizRepository = quizRepository,
        super(const QuizInitial()) {
    on<GetSessionByPinEvent>(_onGetSessionByPin);
    on<JoinSessionEvent>(_onJoinSession);
    on<ResetQuizEvent>(_onResetQuiz);
    on<CreateQuizEvent>(_onCreateQuiz);
    on<CreateSessionEvent>(_onCreateSession);
    on<StartSessionEvent>(_onStartSession);
    on<LoadAllQuizzesEvent>(_onLoadAllQuizzes);
  }

  /// Hent session via PIN
  Future<void> _onGetSessionByPin(
    GetSessionByPinEvent event,
    Emitter<QuizState> emit,
  ) async {
    emit(const QuizLoading());

    final result = await _quizRepository.getSessionByPin(event.pin);

    if (result.isSuccess) {
      final session = result.dataOrNull!;
      emit(QuizSessionFound(session: session));
    } else {
      final error = result.exceptionOrNull!;
      emit(QuizError(message: error.userMessage));
    }
  }

  /// Join session
  Future<void> _onJoinSession(
    JoinSessionEvent event,
    Emitter<QuizState> emit,
  ) async {
    emit(const QuizLoading());

    final result = await _quizRepository.joinSession(
      sessionPin: event.sessionPin,
      nickname: event.nickname,
    );

    if (result.isSuccess) {
      final participant = result.dataOrNull!;
      
      // Hent session info igen for at få opdateret participant count
      final sessionResult = await _quizRepository.getSessionByPin(event.sessionPin);
      
      if (sessionResult.isSuccess) {
        final session = sessionResult.dataOrNull!;
        emit(QuizJoined(session: session, participant: participant));
      } else {
        // Hvis vi ikke kan hente session, brug deltager info
        emit(QuizError(message: 'Kunne ikke hente session info'));
      }
    } else {
      final error = result.exceptionOrNull!;
      emit(QuizError(message: error.userMessage));
    }
  }

  /// Reset quiz state
  void _onResetQuiz(
    ResetQuizEvent event,
    Emitter<QuizState> emit,
  ) {
    emit(const QuizInitial());
  }

  /// Opret quiz
  Future<void> _onCreateQuiz(
    CreateQuizEvent event,
    Emitter<QuizState> emit,
  ) async {
    emit(const QuizLoading());

    final result = await _quizRepository.createQuiz(event.quiz);

    if (result.isSuccess) {
      final quiz = result.dataOrNull!;
      emit(QuizCreated(quiz: quiz));
    } else {
      final error = result.exceptionOrNull!;
      emit(QuizError(message: error.userMessage));
    }
  }

  /// Opret session
  Future<void> _onCreateSession(
    CreateSessionEvent event,
    Emitter<QuizState> emit,
  ) async {
    emit(const QuizLoading());

    final result = await _quizRepository.createSession(quizId: event.quizId);

    if (result.isSuccess) {
      final session = result.dataOrNull!;
      emit(SessionCreated(session: session));
    } else {
      final error = result.exceptionOrNull!;
      emit(QuizError(message: error.userMessage));
    }
  }

  /// Start session
  Future<void> _onStartSession(
    StartSessionEvent event,
    Emitter<QuizState> emit,
  ) async {
    emit(const QuizLoading());

    final result = await _quizRepository.startSession(event.sessionId);

    if (result.isSuccess) {
      // Hent opdateret session info
      // Vi skal have session PIN - men vi har ikke den i event
      // Så vi returnerer success og lader UI håndtere refresh
      emit(const SessionStarted(session: null));
    } else {
      final error = result.exceptionOrNull!;
      emit(QuizError(message: error.userMessage));
    }
  }

  /// Hent alle quizzers
  Future<void> _onLoadAllQuizzes(
    LoadAllQuizzesEvent event,
    Emitter<QuizState> emit,
  ) async {
    emit(const QuizLoading());

    final result = await _quizRepository.getAllQuizzes();

    if (result.isSuccess) {
      final quizzes = result.dataOrNull!;
      emit(AllQuizzesLoaded(quizzes: quizzes));
    } else {
      final error = result.exceptionOrNull!;
      emit(QuizError(message: error.userMessage));
    }
  }
}

