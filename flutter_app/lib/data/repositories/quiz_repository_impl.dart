import '../datasources/quiz_remote_datasource.dart';
import '../models/quiz/session_model.dart';
import '../models/quiz/participant_model.dart';
import '../models/quiz/question_model.dart';
import '../models/quiz/create_quiz_model.dart';
import '../models/quiz/quiz_model.dart';
import '../models/quiz/quiz_summary_model.dart';
import '../models/quiz/leaderboard_entry_model.dart';
import '../../core/api/api_result.dart';

/// Quiz Repository Implementation
/// 
/// Repository pattern for quiz operations.
/// Abstraherer data source implementation fra business logic.
class QuizRepositoryImpl {
  final QuizRemoteDataSource _remoteDataSource;

  QuizRepositoryImpl({required QuizRemoteDataSource remoteDataSource})
      : _remoteDataSource = remoteDataSource;

  /// Hent session via PIN
  Future<ApiResult<SessionModel>> getSessionByPin(String pin) {
    return _remoteDataSource.getSessionByPin(pin);
  }

  /// Join en session
  Future<ApiResult<ParticipantModel>> joinSession({
    required String sessionPin,
    required String nickname,
  }) {
    return _remoteDataSource.joinSession(
      sessionPin: sessionPin,
      nickname: nickname,
    );
  }

  /// Hent spørgsmål
  Future<ApiResult<QuestionModel>> getQuestion({
    required int sessionId,
    required int questionOrderIndex,
  }) {
    return _remoteDataSource.getQuestion(
      sessionId: sessionId,
      questionOrderIndex: questionOrderIndex,
    );
  }

  /// Indsend svar
  Future<ApiResult<Map<String, dynamic>>> submitAnswer({
    required int participantId,
    required int questionId,
    required int answerId,
    required int responseTimeMs,
  }) {
    return _remoteDataSource.submitAnswer(
      participantId: participantId,
      questionId: questionId,
      answerId: answerId,
      responseTimeMs: responseTimeMs,
    );
  }

  /// Hent leaderboard
  Future<ApiResult<List<LeaderboardEntryModel>>> getLeaderboard(int sessionId) {
    return _remoteDataSource.getLeaderboard(sessionId);
  }

  /// Opret quiz
  Future<ApiResult<QuizModel>> createQuiz(CreateQuizModel quiz) {
    return _remoteDataSource.createQuiz(quiz);
  }

  /// Opret session
  Future<ApiResult<SessionModel>> createSession({required int quizId}) {
    return _remoteDataSource.createSession(quizId: quizId);
  }

  /// Start session
  Future<ApiResult<Map<String, dynamic>>> startSession(int sessionId) {
    return _remoteDataSource.startSession(sessionId);
  }

  /// Hent quiz via PIN
  Future<ApiResult<QuizModel>> getQuizByPin(String pin) {
    return _remoteDataSource.getQuizByPin(pin);
  }

  /// Hent alle quizzers
  Future<ApiResult<List<QuizSummaryModel>>> getAllQuizzes() {
    return _remoteDataSource.getAllQuizzes();
  }

  /// Hent quiz via ID
  Future<ApiResult<QuizModel>> getQuizById(int id) {
    return _remoteDataSource.getQuizById(id);
  }
}

