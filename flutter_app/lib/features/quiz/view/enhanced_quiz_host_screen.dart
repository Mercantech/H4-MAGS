import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../bloc/quiz_bloc.dart';
import '../bloc/quiz_event.dart';
import '../bloc/quiz_state.dart';
import '../../../data/models/quiz/quiz_model.dart';
import '../../../data/models/quiz/session_model.dart';
import '../../../data/models/quiz/question_model.dart';
import '../../../data/models/quiz/leaderboard_entry_model.dart';
import '../../../core/di/injection.dart';
import '../../../data/repositories/quiz_repository_impl.dart';

/// Enhanced Quiz Host Screen
/// 
/// Forbedret host screen med:
/// - Status visning
/// - Nuværende spørgsmål
/// - Tid tilbage
/// - Løbende leaderboard
/// - Hyppig opdatering
class EnhancedQuizHostScreen extends StatefulWidget {
  final QuizModel quiz;
  final SessionModel? initialSession;

  const EnhancedQuizHostScreen({
    super.key,
    required this.quiz,
    this.initialSession,
  });

  @override
  State<EnhancedQuizHostScreen> createState() => _EnhancedQuizHostScreenState();
}

class _EnhancedQuizHostScreenState extends State<EnhancedQuizHostScreen> {
  SessionModel? _session;
  List<LeaderboardEntryModel> _leaderboard = [];
  QuestionModel? _currentQuestion;
  int _currentQuestionIndex = 0;
  int _timeRemaining = 0;
  DateTime? _questionStartTime;
  Timer? _pollTimer;
  Timer? _timeTimer;

  @override
  void initState() {
    super.initState();
    _session = widget.initialSession;
    if (_session == null) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        context.read<QuizBloc>().add(CreateSessionEvent(quizId: widget.quiz.id));
      });
    } else {
      _startPolling();
    }
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    _timeTimer?.cancel();
    super.dispose();
  }

  void _startPolling() {
    if (_session == null) return;
    
    // Poll hver 1 sekund når quiz er i gang, ellers hver 3 sekunder
    _pollTimer = Timer.periodic(
      _session!.status == 'InProgress' 
          ? const Duration(seconds: 1)
          : const Duration(seconds: 2),
      (timer) async {
        if (_session == null) {
          timer.cancel();
          return;
        }
        await _refreshData();
      },
    );
  }

  Future<void> _refreshData() async {
    if (_session == null) return;
    
    final repository = getIt<QuizRepositoryImpl>();
    
    // Hent opdateret session
    final sessionResult = await repository.getSessionByPin(_session!.sessionPin);
    if (sessionResult.isSuccess && mounted) {
      setState(() {
        _session = sessionResult.dataOrNull!;
      });
    }
    
    // Hent leaderboard hvis quiz er i gang
    if (_session?.status == 'InProgress' || _session?.status == 'Completed') {
      final leaderboardResult = await repository.getLeaderboard(_session!.id);
      if (leaderboardResult.isSuccess && mounted) {
        setState(() {
          _leaderboard = leaderboardResult.dataOrNull ?? [];
        });
      }
      
      // Hent nuværende spørgsmål hvis quiz er i gang
      if (_session?.status == 'InProgress') {
        await _updateCurrentQuestion();
      }
    }
  }

  Future<void> _updateCurrentQuestion() async {
    if (_session == null || _session!.startedAt == null) return;
    
    // Find nuværende spørgsmål baseret på hvor lang tid der er gået siden quiz startede
    final elapsed = DateTime.now().difference(_session!.startedAt!);
    int totalSeconds = 0;
    int questionIndex = -1;
    
    // Find hvilket spørgsmål vi er ved baseret på akkumuleret tid
    for (int i = 0; i < widget.quiz.questions.length; i++) {
      final question = widget.quiz.questions[i];
      final questionEndTime = totalSeconds + question.timeLimitSeconds;
      
      if (elapsed.inSeconds >= totalSeconds && elapsed.inSeconds < questionEndTime) {
        questionIndex = i;
        break;
      }
      totalSeconds = questionEndTime;
    }
    
    // Hvis vi har passeret alle spørgsmål
    if (questionIndex < 0 || questionIndex >= widget.quiz.questions.length) {
      if (mounted) {
        setState(() {
          _currentQuestion = null;
          _timeRemaining = 0;
        });
      }
      return;
    }
    
    // Hvis det er et nyt spørgsmål, hent det
    if (_currentQuestionIndex != questionIndex) {
      _currentQuestionIndex = questionIndex;
      final repository = getIt<QuizRepositoryImpl>();
      final questionResult = await repository.getQuestion(
        sessionId: _session!.id,
        questionOrderIndex: questionIndex + 1,
      );
      
      if (questionResult.isSuccess && mounted) {
        final question = questionResult.dataOrNull!;
        // Beregn start tid for dette spørgsmål
        int questionStartSeconds = 0;
        for (int i = 0; i < questionIndex; i++) {
          questionStartSeconds += widget.quiz.questions[i].timeLimitSeconds;
        }
        
        setState(() {
          _currentQuestion = question;
          _questionStartTime = _session!.startedAt!.add(Duration(seconds: questionStartSeconds));
          _timeRemaining = question.timeLimitSeconds;
        });
        _startTimeTimer();
      }
    } else if (_currentQuestion != null && _questionStartTime != null) {
      // Opdater tid tilbage for nuværende spørgsmål
      final elapsed = DateTime.now().difference(_questionStartTime!);
      final remaining = (_currentQuestion!.timeLimitSeconds - elapsed.inSeconds).clamp(0, _currentQuestion!.timeLimitSeconds);
      if (mounted) {
        setState(() {
          _timeRemaining = remaining;
        });
      }
    }
  }

  void _startTimeTimer() {
    _timeTimer?.cancel();
    _timeTimer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (_currentQuestion == null || _questionStartTime == null) {
        timer.cancel();
        return;
      }
      
      final elapsed = DateTime.now().difference(_questionStartTime!);
      final remaining = (_currentQuestion!.timeLimitSeconds - elapsed.inSeconds).clamp(0, _currentQuestion!.timeLimitSeconds);
      
      if (mounted) {
        setState(() {
          _timeRemaining = remaining;
        });
      }
      
      if (remaining <= 0) {
        timer.cancel();
        // Spørgsmål er færdig - vent på næste
        Future.delayed(const Duration(seconds: 2), () {
          _updateCurrentQuestion();
        });
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.quiz.title),
        backgroundColor: Theme.of(context).colorScheme.primary,
        foregroundColor: Colors.white,
      ),
      body: BlocListener<QuizBloc, QuizState>(
        listener: (context, state) {
          if (state is SessionCreated) {
            setState(() {
              _session = state.session;
            });
            _startPolling();
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text('Session oprettet! PIN: ${state.session.sessionPin}'),
                backgroundColor: Colors.green,
                duration: const Duration(seconds: 5),
              ),
            );
          } else if (state is SessionStarted) {
            _refreshData();
            _startPolling(); // Restart polling med højere frekvens
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(
                content: Text('Quiz er startet!'),
                backgroundColor: Colors.green,
              ),
            );
          } else if (state is QuizError) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text(state.message),
                backgroundColor: Colors.red,
              ),
            );
          }
        },
        child: _session == null
            ? const Center(child: CircularProgressIndicator())
            : RefreshIndicator(
                onRefresh: _refreshData,
                child: SingleChildScrollView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.all(16.0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      // Status Card
                      _buildStatusCard(context),
                      const SizedBox(height: 16),

                      // Session PIN Card
                      _buildPinCard(context),
                      const SizedBox(height: 16),

                      // Current Question Card (hvis quiz er i gang)
                      if (_session!.status == 'InProgress' && _currentQuestion != null)
                        _buildCurrentQuestionCard(context),
                      if (_session!.status == 'InProgress' && _currentQuestion != null)
                        const SizedBox(height: 16),

                      // Participants Count
                      _buildParticipantsCard(context),
                      const SizedBox(height: 16),

                      // Leaderboard (hvis quiz er i gang eller færdig)
                      if (_session!.status == 'InProgress' || _session!.status == 'Completed')
                        _buildLeaderboardCard(context),
                      if (_session!.status == 'InProgress' || _session!.status == 'Completed')
                        const SizedBox(height: 16),

                      // Start Quiz Button (hvis waiting)
                      if (_session!.status == 'Waiting') _buildStartButton(context),
                    ],
                  ),
                ),
              ),
      ),
    );
  }

  Widget _buildStatusCard(BuildContext context) {
    Color statusColor;
    IconData statusIcon;
    String statusText;

    switch (_session!.status) {
      case 'Waiting':
        statusColor = Colors.orange;
        statusIcon = Icons.hourglass_empty;
        statusText = 'Venter på deltagere';
        break;
      case 'InProgress':
        statusColor = Colors.green;
        statusIcon = Icons.play_circle;
        statusText = 'Quiz i gang';
        break;
      case 'Completed':
        statusColor = Colors.blue;
        statusIcon = Icons.check_circle;
        statusText = 'Quiz afsluttet';
        break;
      default:
        statusColor = Colors.grey;
        statusIcon = Icons.help_outline;
        statusText = _session!.status;
    }

    return Card(
      elevation: 2,
      color: statusColor.withOpacity(0.1),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: statusColor, width: 2),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            Icon(statusIcon, color: statusColor, size: 32),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Status',
                    style: Theme.of(context).textTheme.bodySmall?.copyWith(
                          color: Colors.grey[600],
                        ),
                  ),
                  Text(
                    statusText,
                    style: Theme.of(context).textTheme.titleLarge?.copyWith(
                          fontWeight: FontWeight.bold,
                          color: statusColor,
                        ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildPinCard(BuildContext context) {
    return Card(
      elevation: 4,
      color: Theme.of(context).colorScheme.primaryContainer,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          children: [
            Text(
              'Session PIN',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
            const SizedBox(height: 16),
            Text(
              _session!.sessionPin,
              style: Theme.of(context).textTheme.displayLarge?.copyWith(
                    fontWeight: FontWeight.bold,
                    letterSpacing: 8,
                    color: Theme.of(context).colorScheme.primary,
                  ),
            ),
            const SizedBox(height: 8),
            Text(
              'Deltagere kan bruge denne PIN til at join',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: Colors.grey[600],
                  ),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildCurrentQuestionCard(BuildContext context) {
    if (_currentQuestion == null) return const SizedBox.shrink();

    return Card(
      elevation: 3,
      color: Theme.of(context).colorScheme.secondaryContainer,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Spørgsmål ${_currentQuestionIndex + 1} / ${widget.quiz.questions.length}',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                  decoration: BoxDecoration(
                    color: _timeRemaining < 10 ? Colors.red : Colors.green,
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(
                    '$_timeRemaining s',
                    style: const TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.bold,
                      fontSize: 16,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Text(
              _currentQuestion!.text,
              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Icon(Icons.timer, size: 16, color: Colors.grey[600]),
                const SizedBox(width: 4),
                Text(
                  '${_currentQuestion!.timeLimitSeconds} sekunder',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: Colors.grey[600],
                      ),
                ),
                const SizedBox(width: 16),
                Icon(Icons.star, size: 16, color: Colors.grey[600]),
                const SizedBox(width: 4),
                Text(
                  '${_currentQuestion!.points} point',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: Colors.grey[600],
                      ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildParticipantsCard(BuildContext context) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.people, size: 32, color: Theme.of(context).colorScheme.primary),
            const SizedBox(width: 16),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  '${_session!.participantCount}',
                  style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                ),
                Text(
                  'Deltagere',
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: Colors.grey[600],
                      ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildLeaderboardCard(BuildContext context) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.emoji_events, color: Colors.amber[700]),
                const SizedBox(width: 8),
                Text(
                  'Leaderboard',
                  style: Theme.of(context).textTheme.titleLarge?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            if (_leaderboard.isEmpty)
              Padding(
                padding: const EdgeInsets.all(16),
                child: Text(
                  'Ingen resultater endnu',
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: Colors.grey[600],
                      ),
                  textAlign: TextAlign.center,
                ),
              )
            else
              ..._leaderboard.take(10).map((entry) {
                final isTopThree = entry.rank <= 3;
                return Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Row(
                    children: [
                      Container(
                        width: 40,
                        height: 40,
                        decoration: BoxDecoration(
                          color: isTopThree
                              ? Colors.amber[100]
                              : Colors.grey[200],
                          shape: BoxShape.circle,
                        ),
                        child: Center(
                          child: Text(
                            '${entry.rank}',
                            style: TextStyle(
                              fontWeight: FontWeight.bold,
                              color: isTopThree ? Colors.amber[900] : Colors.black87,
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          entry.nickname,
                          style: TextStyle(
                            fontWeight: isTopThree ? FontWeight.bold : FontWeight.normal,
                            fontSize: 16,
                          ),
                        ),
                      ),
                      Text(
                        '${entry.totalPoints}',
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(
                              fontWeight: FontWeight.bold,
                              color: isTopThree ? Colors.amber[900] : null,
                            ),
                      ),
                    ],
                  ),
                );
              }),
          ],
        ),
      ),
    );
  }

  Widget _buildStartButton(BuildContext context) {
    return BlocBuilder<QuizBloc, QuizState>(
      builder: (context, state) {
        final isLoading = state is QuizLoading;
        return ElevatedButton(
          onPressed: isLoading || _session!.participantCount == 0
              ? null
              : () {
                  context.read<QuizBloc>().add(
                        StartSessionEvent(sessionId: _session!.id),
                      );
                },
          style: ElevatedButton.styleFrom(
            padding: const EdgeInsets.symmetric(vertical: 16),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
          ),
          child: isLoading
              ? const SizedBox(
                  height: 20,
                  width: 20,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Text(
                  'Start Quiz',
                  style: TextStyle(fontSize: 16),
                ),
        );
      },
    );
  }
}

