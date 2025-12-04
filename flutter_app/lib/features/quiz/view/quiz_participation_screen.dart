import 'dart:async';
import 'package:flutter/material.dart';
import '../../../data/models/quiz/session_model.dart';
import '../../../data/models/quiz/participant_model.dart';
import '../../../data/models/quiz/question_model.dart';
import '../../../core/di/injection.dart';
import '../../../data/repositories/quiz_repository_impl.dart';
import 'quiz_results_screen.dart';

/// Quiz Participation Screen
/// 
/// Skærm for deltagere til at svare på spørgsmål.
class QuizParticipationScreen extends StatefulWidget {
  final SessionModel session;
  final ParticipantModel participant;

  const QuizParticipationScreen({
    super.key,
    required this.session,
    required this.participant,
  });

  @override
  State<QuizParticipationScreen> createState() => _QuizParticipationScreenState();
}

class _QuizParticipationScreenState extends State<QuizParticipationScreen> {
  QuestionModel? _currentQuestion;
  int? _selectedAnswerId;
  Timer? _timer;
  Timer? _pollTimer;
  int _timeRemaining = 0;
  bool _hasAnswered = false;
  DateTime? _questionStartTime;
  int _participantTotalPoints = 0;
  SessionModel? _currentSession;
  int? _lastQuestionOrderIndex; // Track hvilket spørgsmål vi sidst så

  @override
  void initState() {
    super.initState();
    _participantTotalPoints = widget.participant.totalPoints;
    _currentSession = widget.session;
    _lastQuestionOrderIndex = widget.session.currentQuestionOrderIndex;
    
    // Hvis der allerede er et aktivt spørgsmål, load det med det samme
    if (widget.session.currentQuestionOrderIndex != null) {
      _loadCurrentQuestion();
    }
    
    _startPolling(); // Start polling for nuværende spørgsmål
  }

  @override
  void dispose() {
    _timer?.cancel();
    _pollTimer?.cancel();
    super.dispose();
  }

  void _startPolling() {
    // Poll hver sekund for at tjekke om der er et nyt spørgsmål
    _pollTimer = Timer.periodic(const Duration(seconds: 1), (timer) async {
      if (!mounted) return;

      final repository = getIt<QuizRepositoryImpl>();
      
      // Hent opdateret session
      final sessionResult = await repository.getSessionByPin(widget.session.sessionPin);
      if (sessionResult.isSuccess) {
        final updatedSession = sessionResult.dataOrNull!;
        
        setState(() {
          _currentSession = updatedSession;
        });

        // Tjek om session er færdig
        if (updatedSession.status == 'Completed') {
          timer.cancel();
          _showResults();
          return;
        }

        // Tjek om der er et nyt spørgsmål
        if (updatedSession.currentQuestionOrderIndex != null &&
            updatedSession.currentQuestionOrderIndex != _lastQuestionOrderIndex) {
          _lastQuestionOrderIndex = updatedSession.currentQuestionOrderIndex;
          await _loadCurrentQuestion();
        }
      }
    });
  }

  Future<void> _loadCurrentQuestion() async {
    final repository = getIt<QuizRepositoryImpl>();
    final result = await repository.getCurrentQuestion(
      sessionId: widget.session.id,
    );

    if (result.isSuccess) {
      setState(() {
        _currentQuestion = result.dataOrNull!;
        _timeRemaining = _currentQuestion!.timeLimitSeconds;
        _selectedAnswerId = null;
        _hasAnswered = false;
        _questionStartTime = DateTime.now();
      });
      _startTimer();
    } else {
      // Hvis spørgsmål ikke findes, kan det være at quiz er færdig eller venter
      final error = result.exceptionOrNull;
      if (error != null && error.statusCode == 404) {
        // Ingen aktivt spørgsmål - vent på næste
        setState(() {
          _currentQuestion = null;
        });
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Kunne ikke hente spørgsmål: ${error?.userMessage ?? "Ukendt fejl"}'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  void _startTimer() {
    _timer?.cancel();
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (mounted) {
        setState(() {
          if (_timeRemaining > 0) {
            _timeRemaining--;
          } else {
            timer.cancel();
            if (!_hasAnswered) {
              _submitAnswer(null); // Timeout - submit null answer
            }
          }
        });
      }
    });
  }

  Future<void> _submitAnswer(int? answerId) async {
    if (_hasAnswered || _currentQuestion == null) return;

    _timer?.cancel();
    setState(() {
      _hasAnswered = true;
    });

    if (answerId == null) {
      // Timeout - ingen svar
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Tiden er udløbet!'),
          backgroundColor: Colors.orange,
        ),
      );
      return;
    }

    final responseTime = _questionStartTime != null
        ? DateTime.now().difference(_questionStartTime!).inMilliseconds
        : 0;

    final repository = getIt<QuizRepositoryImpl>();
    final result = await repository.submitAnswer(
      participantId: widget.participant.id,
      questionId: _currentQuestion!.id,
      answerId: answerId,
      responseTimeMs: responseTime,
    );

    if (result.isSuccess) {
      final data = result.dataOrNull!;
      final pointsEarned = data['pointsEarned'] as int? ?? 0;
      final isCorrect = data['isCorrect'] as bool? ?? false;

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            isCorrect
                ? 'Korrekt! +$pointsEarned point'
                : 'Forkert! +$pointsEarned point',
          ),
          backgroundColor: isCorrect ? Colors.green : Colors.red,
        ),
      );

      // Opdater total points
      setState(() {
        _participantTotalPoints = data['totalPoints'] as int? ?? _participantTotalPoints;
      });

      // Vent på at serveren går videre til næste spørgsmål (polling håndterer det)
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Fejl: ${result.exceptionOrNull?.userMessage ?? "Ukendt fejl"}'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  Future<void> _showResults() async {
    // Hent leaderboard
    final repository = getIt<QuizRepositoryImpl>();
    final leaderboardResult = await repository.getLeaderboard(widget.session.id);
    
    // Hent opdateret participant info
    final sessionResult = await repository.getSessionByPin(widget.session.sessionPin);
    final updatedSession = sessionResult.isSuccess ? sessionResult.dataOrNull : null;
    
    if (mounted) {
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(
          builder: (context) => QuizResultsScreen(
            session: updatedSession ?? widget.session,
            participantId: widget.participant.id,
            leaderboard: leaderboardResult.isSuccess 
                ? leaderboardResult.dataOrNull ?? []
                : [],
          ),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.session.quizTitle),
        backgroundColor: Theme.of(context).colorScheme.primary,
        foregroundColor: Colors.white,
      ),
      body: Column(
              children: [
                // Timer og Points
                Container(
                  padding: const EdgeInsets.all(16),
                  color: Theme.of(context).colorScheme.primaryContainer,
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Column(
                        children: [
                          Text(
                            'Tid',
                            style: Theme.of(context).textTheme.bodySmall,
                          ),
                          Text(
                            '$_timeRemaining',
                            style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                                  fontWeight: FontWeight.bold,
                                  color: _timeRemaining < 10
                                      ? Colors.red
                                      : Theme.of(context).colorScheme.primary,
                                ),
                          ),
                        ],
                      ),
                      Column(
                        children: [
                          Text(
                            'Point',
                            style: Theme.of(context).textTheme.bodySmall,
                          ),
                          Text(
                            '$_participantTotalPoints',
                            style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                                  fontWeight: FontWeight.bold,
                                ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),

                // Question
                Expanded(
                  child: _currentQuestion == null
                      ? Center(
                          child: Column(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              const CircularProgressIndicator(),
                              const SizedBox(height: 16),
                              Text(
                                _currentSession?.status == 'Completed'
                                    ? 'Quiz færdig!'
                                    : 'Venter på næste spørgsmål...',
                                style: Theme.of(context).textTheme.titleLarge,
                              ),
                            ],
                          ),
                        )
                      : SingleChildScrollView(
                          padding: const EdgeInsets.all(24),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: [
                              Text(
                                'Spørgsmål ${_currentQuestion!.orderIndex}',
                                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                                      color: Colors.grey[600],
                                    ),
                              ),
                              const SizedBox(height: 16),
                              Text(
                                _currentQuestion!.text,
                                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                                      fontWeight: FontWeight.bold,
                                    ),
                              ),
                              const SizedBox(height: 32),

                              // Answers
                              ..._currentQuestion!.answers.asMap().entries.map((entry) {
                          final answer = entry.value;
                          final index = entry.key;
                          final isSelected = _selectedAnswerId == answer.id;

                          return Padding(
                            padding: const EdgeInsets.only(bottom: 12),
                            child: ElevatedButton(
                              onPressed: _hasAnswered
                                  ? null
                                  : () {
                                      setState(() {
                                        _selectedAnswerId = answer.id;
                                      });
                                    },
                              style: ElevatedButton.styleFrom(
                                padding: const EdgeInsets.all(16),
                                backgroundColor: isSelected
                                    ? Theme.of(context).colorScheme.primary
                                    : Colors.grey[200],
                                foregroundColor: isSelected
                                    ? Colors.white
                                    : Colors.black87,
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(12),
                                ),
                              ),
                              child: Row(
                                children: [
                                  Container(
                                    width: 32,
                                    height: 32,
                                    decoration: BoxDecoration(
                                      color: isSelected
                                          ? Colors.white
                                          : Colors.grey[400],
                                      shape: BoxShape.circle,
                                    ),
                                    child: Center(
                                      child: Text(
                                        String.fromCharCode(65 + index), // A, B, C, D
                                        style: TextStyle(
                                          color: isSelected
                                              ? Theme.of(context).colorScheme.primary
                                              : Colors.white,
                                          fontWeight: FontWeight.bold,
                                        ),
                                      ),
                                    ),
                                  ),
                                  const SizedBox(width: 16),
                                  Expanded(
                                    child: Text(
                                      answer.text,
                                      style: const TextStyle(fontSize: 16),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          );
                        }),

                        const SizedBox(height: 24),

                        // Submit Button
                        if (!_hasAnswered && _selectedAnswerId != null)
                          ElevatedButton(
                            onPressed: () => _submitAnswer(_selectedAnswerId),
                            style: ElevatedButton.styleFrom(
                              padding: const EdgeInsets.symmetric(vertical: 16),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12),
                              ),
                            ),
                            child: const Text(
                              'Send Svar',
                              style: TextStyle(fontSize: 16),
                            ),
                          ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
    );
  }
}

