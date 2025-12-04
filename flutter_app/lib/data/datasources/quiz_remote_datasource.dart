import '../models/quiz/session_model.dart';
import '../models/quiz/participant_model.dart';
import '../models/quiz/question_model.dart';
import '../models/quiz/create_quiz_model.dart';
import '../models/quiz/quiz_model.dart';
import '../models/quiz/quiz_summary_model.dart';
import '../models/quiz/leaderboard_entry_model.dart';
import '../../core/api/api_client.dart';
import '../../core/api/api_result.dart';

/// Remote data source for Quiz operations
/// 
/// Håndterer alle API calls relateret til quiz sessions og deltagelse.
class QuizRemoteDataSource {
  final ApiClient _apiClient;

  QuizRemoteDataSource({required ApiClient apiClient})
      : _apiClient = apiClient;

  /// Hent session via PIN
  Future<ApiResult<SessionModel>> getSessionByPin(String pin) async {
    return await _apiClient.get<SessionModel>(
      '/quizsession/pin/$pin',
      fromJson: (json) => SessionModel.fromJson(json as Map<String, dynamic>),
    );
  }

  /// Join en session med PIN og nickname
  Future<ApiResult<ParticipantModel>> joinSession({
    required String sessionPin,
    required String nickname,
  }) async {
    return await _apiClient.post<ParticipantModel>(
      '/quizsession/join',
      body: {
        'sessionPin': sessionPin,
        'nickname': nickname,
      },
      fromJson: (json) => ParticipantModel.fromJson(json as Map<String, dynamic>),
    );
  }

  /// Hent nuværende spørgsmål for en session (centralt styret)
  Future<ApiResult<QuestionModel>> getCurrentQuestion({
    required int sessionId,
  }) async {
    return await _apiClient.get<QuestionModel>(
      '/participant/session/$sessionId/current-question',
      fromJson: (json) => QuestionModel.fromJson(json as Map<String, dynamic>),
    );
  }

  /// Hent et spørgsmål til en session (uden korrekt svar) - deprecated, brug getCurrentQuestion
  Future<ApiResult<QuestionModel>> getQuestion({
    required int sessionId,
    required int questionOrderIndex,
  }) async {
    return await _apiClient.get<QuestionModel>(
      '/participant/session/$sessionId/question/$questionOrderIndex',
      fromJson: (json) => QuestionModel.fromJson(json as Map<String, dynamic>),
    );
  }

  /// Indsend svar på et spørgsmål
  Future<ApiResult<Map<String, dynamic>>> submitAnswer({
    required int participantId,
    required int questionId,
    required int answerId,
    required int responseTimeMs,
  }) async {
    return await _apiClient.post<Map<String, dynamic>>(
      '/participant/submit-answer',
      body: {
        'participantId': participantId,
        'questionId': questionId,
        'answerId': answerId,
        'responseTimeMs': responseTimeMs,
      },
      fromJson: (json) => json as Map<String, dynamic>,
    );
  }

  /// Hent leaderboard for en session
  Future<ApiResult<List<LeaderboardEntryModel>>> getLeaderboard(int sessionId) async {
    return await _apiClient.get<List<LeaderboardEntryModel>>(
      '/participant/leaderboard/$sessionId',
      fromJson: (json) => (json as List<dynamic>)
          .map((e) => LeaderboardEntryModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  /// Opret en ny quiz
  Future<ApiResult<QuizModel>> createQuiz(CreateQuizModel quiz) async {
    return await _apiClient.post<QuizModel>(
      '/quiz',
      body: quiz.toJson(),
      fromJson: (json) => QuizModel.fromJson(json as Map<String, dynamic>),
    );
  }

  /// Opret en ny quiz session
  Future<ApiResult<SessionModel>> createSession({required int quizId}) async {
    return await _apiClient.post<SessionModel>(
      '/quizsession',
      body: {'quizId': quizId},
      fromJson: (json) => SessionModel.fromJson(json as Map<String, dynamic>),
    );
  }

  /// Start en quiz session
  Future<ApiResult<Map<String, dynamic>>> startSession(int sessionId) async {
    return await _apiClient.post<Map<String, dynamic>>(
      '/quizsession/$sessionId/start',
      body: {},
      fromJson: (json) => json as Map<String, dynamic>,
    );
  }

  /// Hent quiz via PIN
  Future<ApiResult<QuizModel>> getQuizByPin(String pin) async {
    return await _apiClient.get<QuizModel>(
      '/quiz/pin/$pin',
      fromJson: (json) => QuizModel.fromJson(json as Map<String, dynamic>),
    );
  }

  /// Hent alle quizzers
  Future<ApiResult<List<QuizSummaryModel>>> getAllQuizzes() async {
    return await _apiClient.get<List<QuizSummaryModel>>(
      '/quiz',
      fromJson: (json) => (json as List<dynamic>)
          .map((e) => QuizSummaryModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  /// Hent quiz via ID
  Future<ApiResult<QuizModel>> getQuizById(int id) async {
    return await _apiClient.get<QuizModel>(
      '/quiz/$id',
      fromJson: (json) => QuizModel.fromJson(json as Map<String, dynamic>),
    );
  }
}

